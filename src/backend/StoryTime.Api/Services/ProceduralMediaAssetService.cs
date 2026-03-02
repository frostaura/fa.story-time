using System.Text;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StoryTime.Api.Domain;

namespace StoryTime.Api.Services;

public sealed class ProceduralMediaAssetService(
    IOptions<StoryTimeOptions> options,
    ILogger<ProceduralMediaAssetService> logger,
    IHttpClientFactory httpClientFactory) : IMediaAssetService
{
    private readonly StoryTimeOptions _options = options.Value;
    private readonly MessageTemplateOptions _messages = options.Value.Messages;
    private readonly ILogger<ProceduralMediaAssetService> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public IReadOnlyList<PosterLayer> BuildPosterLayers(string seed, bool reducedMotion)
    {
        var configuredLayers = _options.Generation.PosterLayers
            .Where(layer => !string.IsNullOrWhiteSpace(layer.Role))
            .ToArray();
        var fallbackLayers = _options.Generation.Fallbacks.PosterLayers
            .Where(layer => !string.IsNullOrWhiteSpace(layer.Role))
            .ToArray();

        var layerRules = configuredLayers.Length >= 3 && configuredLayers.Length <= 5
            ? configuredLayers
            : fallbackLayers;
        if (layerRules.Length < 3 || layerRules.Length > 5)
        {
            throw new InvalidOperationException(_messages.Internal("PosterLayerConfigMustDefine3To5Layers"));
        }

        var posterRoleSpeedByRole = GetPosterRoleSpeedByRole();
        var normalizedLayerRules = NormalizeLayerRules(layerRules, posterRoleSpeedByRole);
        var livePosterLayers = TryBuildLivePosterLayers(seed, normalizedLayerRules, reducedMotion);
        if (livePosterLayers is not null)
        {
            return livePosterLayers;
        }

        if (_options.Generation.ForceProceduralPosterFallback)
        {
            return BuildProceduralLayersWithinBudget(seed, normalizedLayerRules, reducedMotion, "forced");
        }

        var retries = Math.Max(0, _options.Generation.PosterModelRetryCount);
        for (var attempt = 0; attempt <= retries; attempt++)
        {
            if (ShouldUseProceduralFallback(seed))
            {
                continue;
            }

            return BuildModelLayers(seed, normalizedLayerRules, reducedMotion);
        }

        return BuildProceduralLayersWithinBudget(seed, normalizedLayerRules, reducedMotion, "model-failed");
    }

    public string BuildAudioDataUri(string storyId, int durationSeconds, double amplitudeScale)
    {
        var liveNarration = TryBuildLiveNarrationAudioDataUri(storyId, durationSeconds, amplitudeScale);
        if (!string.IsNullOrWhiteSpace(liveNarration))
        {
            return liveNarration;
        }

        return BuildProceduralAudioDataUri(storyId, durationSeconds, amplitudeScale);
    }

    private string BuildProceduralAudioDataUri(string storyId, int durationSeconds, double amplitudeScale)
    {
        var clampedDuration = Math.Max(1, durationSeconds);
        var sampleRate = Math.Clamp(_options.Generation.AudioSampleRate, 8000, 48000);
        var frequency = Math.Clamp(_options.Generation.AudioBaseFrequencyHz, 120, 1200);
        var audioTuning = _options.Generation.ProceduralAudio;
        var weightTotal = audioTuning.CarrierWeight + audioTuning.Harmonic2Weight + audioTuning.Harmonic3Weight;
        if (weightTotal <= 0)
        {
            throw new InvalidOperationException(_messages.Internal("ProceduralAudioWeightsMustSumPositive"));
        }

        var normalizedCarrierWeight = audioTuning.CarrierWeight / weightTotal;
        var normalizedHarmonic2Weight = audioTuning.Harmonic2Weight / weightTotal;
        var normalizedHarmonic3Weight = audioTuning.Harmonic3Weight / weightTotal;
        var sampleCount = sampleRate * clampedDuration;
        var pcmBytes = new byte[sampleCount * sizeof(short)];
        var storySignature = Math.Abs(storyId.GetHashCode(StringComparison.Ordinal));
        var identifierOffset = storySignature % Math.Max(1, audioTuning.IdentifierOffsetRange);
        var segmentSamples = Math.Max(1, sampleRate / Math.Max(1, audioTuning.SegmentDivisor));

        for (var index = 0; index < sampleCount; index++)
        {
            var t = index / (double)sampleRate;
            var segmentPhase = (index % segmentSamples) / (double)segmentSamples;
            var phraseEnvelope = Math.Pow(
                Math.Sin(Math.PI * segmentPhase),
                Math.Max(0.0001, audioTuning.PhraseEnvelopeExponent));
            var baseFrequency = frequency + identifierOffset;
            var melodicBend = 1 + audioTuning.MelodicBendAmplitude * Math.Sin(2 * Math.PI * segmentPhase);
            var carrier = Math.Sin(2 * Math.PI * baseFrequency * melodicBend * t);
            var harmonic2 = Math.Sin(2 * Math.PI * (baseFrequency * 2) * t);
            var harmonic3 = Math.Sin(2 * Math.PI * (baseFrequency * 3) * t);
            var breath = (PseudoNoise(index + storySignature) - 0.5) * audioTuning.BreathNoiseAmplitude;
            var wave = ((carrier * normalizedCarrierWeight) +
                        (harmonic2 * normalizedHarmonic2Weight) +
                        (harmonic3 * normalizedHarmonic3Weight) +
                        breath) * phraseEnvelope;
            var sampleValue = Math.Clamp(wave * short.MaxValue * amplitudeScale, short.MinValue, short.MaxValue);
            var sample = (short)sampleValue;
            BitConverter.GetBytes(sample).CopyTo(pcmBytes, index * sizeof(short));
        }

        var waveBytes = BuildWavBytes(pcmBytes, sampleRate);
        return $"{_options.Generation.DataUris.AudioWavBase64Prefix}{Convert.ToBase64String(waveBytes)}";
    }

    private bool ShouldUseProceduralFallback(string seed)
    {
        if (_options.Generation.PosterModelFailureSeedPrefixes.Any(prefix =>
                !string.IsNullOrWhiteSpace(prefix) && seed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var failureRate = _options.Generation.PosterModelFailureRate;
        if (failureRate <= 0)
        {
            return false;
        }

        return Random.Shared.NextDouble() < failureRate;
    }

    private IReadOnlyList<PosterLayer>? TryBuildLivePosterLayers(
        string seed,
        IReadOnlyList<PosterLayerRule> normalizedLayerRules,
        bool reducedMotion)
    {
        var provider = _options.Generation.PosterModelProvider;
        if (!provider.Enabled)
        {
            return null;
        }

        var endpoint = provider.Endpoint?.Trim();
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return provider.LocalFallbackEnabled
                ? null
                : throw new InvalidOperationException(_messages.Internal("PosterModelProviderEndpointRequiredWhenEnabled"));
        }

        var client = _httpClientFactory.CreateClient(nameof(ProceduralMediaAssetService));
        client.Timeout = TimeSpan.FromSeconds(Math.Max(1, provider.TimeoutSeconds));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new PosterModelRequest(seed, reducedMotion, normalizedLayerRules))
            };

            if (!string.IsNullOrWhiteSpace(provider.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
            }

            using var response = client.SendAsync(request).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                return provider.LocalFallbackEnabled
                    ? null
                    : throw new InvalidOperationException(
                        _messages.Internal(
                            "PosterModelProviderFailedWithStatus",
                            ("StatusCode", ((int)response.StatusCode).ToString())));
            }

            var payload = response.Content.ReadFromJsonAsync<PosterModelResponse>().GetAwaiter().GetResult();
            var roleToLayer = (payload?.Layers ?? [])
                .Where(layer => !string.IsNullOrWhiteSpace(layer.Role) && !string.IsNullOrWhiteSpace(layer.DataUri))
                .ToDictionary(
                    layer => layer.Role!.Trim().ToUpperInvariant(),
                    layer => layer.DataUri!.Trim(),
                    StringComparer.OrdinalIgnoreCase);
            if (roleToLayer.Count == 0)
            {
                return provider.LocalFallbackEnabled
                    ? null
                    : throw new InvalidOperationException(_messages.Internal("PosterModelProviderReturnedNoLayers"));
            }

            foreach (var rule in normalizedLayerRules)
            {
                if (!roleToLayer.ContainsKey(rule.Role))
                {
                    return provider.LocalFallbackEnabled
                        ? null
                        : throw new InvalidOperationException(
                            _messages.Internal(
                                "PosterModelProviderResponseMissingLayer",
                                ("Role", rule.Role)));
                }
            }

            return normalizedLayerRules.Select(rule => new PosterLayer(
                rule.Role,
                reducedMotion ? 0 : rule.SpeedMultiplier,
                roleToLayer[rule.Role])).ToArray();
        }
        catch (HttpRequestException) when (provider.LocalFallbackEnabled)
        {
            return null;
        }
        catch (TaskCanceledException) when (provider.LocalFallbackEnabled)
        {
            return null;
        }
        catch (JsonException) when (provider.LocalFallbackEnabled)
        {
            return null;
        }
    }

    private string? TryBuildLiveNarrationAudioDataUri(string storyId, int durationSeconds, double amplitudeScale)
    {
        var provider = _options.Generation.NarrationProvider;
        if (!provider.Enabled)
        {
            return null;
        }

        var endpoint = provider.Endpoint?.Trim();
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return provider.LocalFallbackEnabled
                ? null
                : throw new InvalidOperationException(_messages.Internal("NarrationProviderEndpointRequiredWhenEnabled"));
        }

        var client = _httpClientFactory.CreateClient(nameof(ProceduralMediaAssetService));
        client.Timeout = TimeSpan.FromSeconds(Math.Max(1, provider.TimeoutSeconds));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new NarrationRequest(storyId, Math.Max(1, durationSeconds), amplitudeScale))
            };

            if (!string.IsNullOrWhiteSpace(provider.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
            }

            using var response = client.SendAsync(request).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                return provider.LocalFallbackEnabled
                    ? null
                    : throw new InvalidOperationException(
                        _messages.Internal(
                            "NarrationProviderFailedWithStatus",
                            ("StatusCode", ((int)response.StatusCode).ToString())));
            }

            var payload = response.Content.ReadFromJsonAsync<NarrationResponse>().GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(payload?.DataUri))
            {
                return provider.LocalFallbackEnabled
                    ? null
                    : throw new InvalidOperationException(_messages.Internal("NarrationProviderReturnedEmptyAudio"));
            }

            var dataUri = payload.DataUri.Trim();
            if (!dataUri.StartsWith(_options.Generation.DataUris.AudioPayloadPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return provider.LocalFallbackEnabled
                    ? null
                    : throw new InvalidOperationException(_messages.Internal("NarrationProviderReturnedNonAudioPayload"));
            }

            return dataUri;
        }
        catch (HttpRequestException) when (provider.LocalFallbackEnabled)
        {
            return null;
        }
        catch (TaskCanceledException) when (provider.LocalFallbackEnabled)
        {
            return null;
        }
        catch (JsonException) when (provider.LocalFallbackEnabled)
        {
            return null;
        }
    }

    private IReadOnlyList<PosterLayer> BuildModelLayers(string seed, IEnumerable<PosterLayerRule> layerRules, bool reducedMotion)
    {
        var posterTuning = _options.Generation.ProceduralPoster;
        var geometry = _options.Generation.ProceduralPosterGeometry;
        return layerRules.Select((rule, index) =>
        {
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                BuildLayerSvg(seed, rule.Role, index, richDetail: true, posterTuning, geometry)));
            return new PosterLayer(
                Role: rule.Role,
                SpeedMultiplier: reducedMotion ? 0 : rule.SpeedMultiplier,
                DataUri: $"{_options.Generation.DataUris.PosterSvgBase64Prefix}{payload}");
        }).ToArray();
    }

    private IReadOnlyList<PosterLayer> BuildProceduralLayers(string seed, IEnumerable<PosterLayerRule> layerRules, bool reducedMotion)
    {
        var posterTuning = _options.Generation.ProceduralPoster;
        var geometry = _options.Generation.ProceduralPosterGeometry;
        return layerRules.Select((rule, index) =>
        {
            var data = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                BuildLayerSvg(seed, rule.Role, index, richDetail: false, posterTuning, geometry)));
            return new PosterLayer(
                Role: rule.Role,
                SpeedMultiplier: reducedMotion ? 0 : rule.SpeedMultiplier,
                DataUri: $"{_options.Generation.DataUris.PosterSvgBase64Prefix}{data}");
        }).ToArray();
    }

    private IReadOnlyList<PosterLayer> BuildProceduralLayersWithinBudget(
        string seed,
        IReadOnlyList<PosterLayerRule> layerRules,
        bool reducedMotion,
        string reason)
    {
        var budgetMs = _options.Generation.ProceduralPosterFallbackBudgetMilliseconds;
        if (budgetMs <= 0)
        {
            throw new InvalidOperationException(_messages.Internal("ProceduralPosterFallbackBudgetMustBePositive"));
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var layers = BuildProceduralLayers(seed, layerRules, reducedMotion);
        stopwatch.Stop();

        _logger.LogInformation(
            "Procedural poster fallback completed in {ElapsedMs}ms (budget {BudgetMs}ms, reason={Reason})",
            stopwatch.ElapsedMilliseconds,
            budgetMs,
            reason);

        if (stopwatch.ElapsedMilliseconds > budgetMs)
        {
            throw new InvalidOperationException(
                _messages.Internal(
                    "ProceduralPosterFallbackExceededBudget",
                    ("ElapsedMs", stopwatch.ElapsedMilliseconds.ToString()),
                    ("BudgetMs", budgetMs.ToString())));
        }

        return layers;
    }

    private Dictionary<string, double> GetPosterRoleSpeedByRole()
    {
        var map = _options.Generation.PosterRoleSpeedMultipliers
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
            .ToDictionary(
                entry => entry.Key.Trim().ToUpperInvariant(),
                entry => entry.Value,
                StringComparer.OrdinalIgnoreCase);

        if (map.Count == 0)
        {
            throw new InvalidOperationException(_messages.Internal("PosterRoleSpeedMultipliersMustDefineAtLeastOneRole"));
        }

        return map;
    }

    private PosterLayerRule[] NormalizeLayerRules(
        IReadOnlyList<PosterLayerRule> layerRules,
        IReadOnlyDictionary<string, double> posterRoleSpeedByRole)
    {
        if (layerRules.Count is < 3 or > 5)
        {
            throw new InvalidOperationException(_messages.Internal("PosterLayerConfigMustDefine3To5Layers"));
        }

        var unknownRoles = layerRules
            .Select(rule => rule.Role?.Trim() ?? string.Empty)
            .Where(role => !string.IsNullOrWhiteSpace(role) && !posterRoleSpeedByRole.ContainsKey(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unknownRoles.Length > 0)
        {
            throw new InvalidOperationException(
                _messages.Internal(
                    "PosterLayerConfigIncludesUnsupportedRoles",
                    ("Roles", string.Join(", ", unknownRoles))));
        }

        var normalized = PosterRoles.Ordered
            .Select(role => layerRules.FirstOrDefault(rule => string.Equals(rule.Role?.Trim(), role, StringComparison.OrdinalIgnoreCase)))
            .Where(rule => rule is not null)
            .Select(rule =>
            {
                var normalizedRole = rule!.Role.Trim().ToUpperInvariant();
                return new PosterLayerRule
                {
                    Role = normalizedRole,
                    SpeedMultiplier = posterRoleSpeedByRole[normalizedRole]
                };
            })
            .ToArray();

        if (normalized.Length is < 3 or > 5)
        {
            throw new InvalidOperationException(_messages.Internal("PosterLayerConfigMustNormalize3To5Layers"));
        }

        if (!normalized.Any(rule => string.Equals(rule.Role, PosterRoles.Background, StringComparison.Ordinal)) ||
            !normalized.Any(rule => string.Equals(rule.Role, PosterRoles.Foreground, StringComparison.Ordinal)) ||
            !normalized.Any(rule => string.Equals(rule.Role, PosterRoles.Particles, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                _messages.Internal(
                    "PosterLayerConfigMustIncludeRequiredRoles",
                    ("BackgroundRole", PosterRoles.Background),
                    ("ForegroundRole", PosterRoles.Foreground),
                    ("ParticlesRole", PosterRoles.Particles)));
        }

        if (normalized.Any(rule => string.Equals(rule.Role, PosterRoles.Midground2, StringComparison.Ordinal)) &&
            !normalized.Any(rule => string.Equals(rule.Role, PosterRoles.Midground1, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                _messages.Internal(
                    "PosterLayerConfigMidgroundRoleDependency",
                    ("Midground2Role", PosterRoles.Midground2),
                    ("Midground1Role", PosterRoles.Midground1)));
        }

        return normalized;
    }

    private static string BuildLayerSvg(
        string seed,
        string role,
        int depth,
        bool richDetail,
        ProceduralPosterOptions tuning,
        ProceduralPosterGeometryOptions geometry)
    {
        var canvasWidth = Math.Max(1, geometry.CanvasWidth);
        var canvasHeight = Math.Max(1, geometry.CanvasHeight);
        var viewBoxWidth = Math.Max(1, geometry.ViewBoxWidth);
        var viewBoxHeight = Math.Max(1, geometry.ViewBoxHeight);
        var horizonVariance = Math.Max(1, geometry.HorizonVariance);
        var driftVariance = Math.Max(1, geometry.DriftVariance);
        var starRangeX = Math.Max(1, geometry.StarRangeX);
        var starRangeY = Math.Max(1, geometry.StarRangeY);
        var starBaseRadius = Math.Max(1, geometry.StarBaseRadius);
        var starRadiusRange = Math.Max(1, geometry.StarRadiusRange);
        var starOpacity = Math.Clamp(geometry.StarOpacity, 0, 1).ToString("0.###", CultureInfo.InvariantCulture);
        var hash = Math.Abs($"{seed}:{role}:{depth}".GetHashCode(StringComparison.Ordinal));
        var primary = BuildColor(hash, 0);
        var secondary = BuildColor(hash, 1);
        var accent = BuildColor(hash, 2);
        var horizon = geometry.HorizonBaseY + (hash % horizonVariance);
        var drift = (hash % driftVariance) - geometry.DriftCenterOffset;
        var opacity = (richDetail ? tuning.RichDetailOpacity : tuning.FallbackOpacity).ToString("0.###", CultureInfo.InvariantCulture);
        var stars = richDetail ? tuning.RichDetailStarCount : tuning.FallbackStarCount;
        var starMarkup = string.Concat(Enumerable.Range(0, stars).Select(index =>
        {
            var sx = geometry.StarBaseX + ((hash + (index * 173)) % starRangeX);
            var sy = geometry.StarBaseY + ((hash + (index * 97)) % starRangeY);
            var r = starBaseRadius + ((hash + index) % starRadiusRange);
            return $"<circle cx='{sx}' cy='{sy}' r='{r}' fill='white' fill-opacity='{starOpacity}' />";
        }));

        return
            $"<svg xmlns='http://www.w3.org/2000/svg' width='{canvasWidth}' height='{canvasHeight}' viewBox='0 0 {viewBoxWidth} {viewBoxHeight}'>" +
            $"<defs><linearGradient id='g{depth}' x1='0%' y1='0%' x2='0%' y2='100%'>" +
            $"<stop offset='0%' stop-color='{primary}' /><stop offset='100%' stop-color='{secondary}' /></linearGradient>" +
            $"<radialGradient id='m{depth}' cx='50%' cy='35%' r='70%'><stop offset='0%' stop-color='{accent}' stop-opacity='0.55' /><stop offset='100%' stop-color='{accent}' stop-opacity='0.02' /></radialGradient></defs>" +
            $"<rect width='{canvasWidth}' height='{canvasHeight}' fill='url(#g{depth})' />" +
            $"<ellipse cx='{geometry.MoonCenterX + drift}' cy='{geometry.MoonCenterY}' rx='{260 + (hash % 120)}' ry='{170 + (hash % 80)}' fill='url(#m{depth})' />" +
            $"{starMarkup}" +
            $"<path d='M0 {horizon} C240 {horizon - 70}, 520 {horizon + 90}, 1024 {horizon - 50} L1024 1024 L0 1024 Z' fill='{accent}' fill-opacity='{opacity}' />" +
            $"<path d='M0 {horizon + 120} C280 {horizon + 20}, 620 {horizon + 220}, 1024 {horizon + 80} L1024 1024 L0 1024 Z' fill='{secondary}' fill-opacity='{opacity}' />" +
            "</svg>";
    }

    private static string BuildColor(int hash, int offset)
    {
        var r = 64 + ((hash >> (offset * 3)) & 0x7F);
        var g = 48 + ((hash >> ((offset * 3) + 7)) & 0x7F);
        var b = 96 + ((hash >> ((offset * 3) + 13)) & 0x7F);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static double PseudoNoise(int seed)
    {
        unchecked
        {
            var value = seed;
            value = (value ^ 61) ^ (value >> 16);
            value += value << 3;
            value ^= value >> 4;
            value *= 0x27d4eb2d;
            value ^= value >> 15;
            return (value & 0x7FFFFFFF) / (double)int.MaxValue;
        }
    }

    private static byte[] BuildWavBytes(byte[] pcm, int sampleRate)
    {
        const short channels = 1;
        const short bitsPerSample = 16;
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var blockAlign = (short)(channels * bitsPerSample / 8);
        var dataSize = pcm.Length;
        using var stream = new MemoryStream(44 + dataSize);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);
        writer.Write(pcm);
        writer.Flush();
        return stream.ToArray();
    }

    private sealed record PosterModelRequest(
        string Seed,
        bool ReducedMotion,
        IReadOnlyList<PosterLayerRule> Layers);

    private sealed record PosterModelResponse(IReadOnlyList<PosterModelLayer>? Layers);

    private sealed record PosterModelLayer(string? Role, string? DataUri);

    private sealed record NarrationRequest(
        string StoryId,
        int DurationSeconds,
        double AmplitudeScale);

    private sealed record NarrationResponse(string? DataUri);
}
