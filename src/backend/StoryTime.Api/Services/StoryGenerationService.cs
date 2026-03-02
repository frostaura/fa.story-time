using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using StoryTime.Api.Contracts;
using StoryTime.Api.Domain;

namespace StoryTime.Api.Services;

public sealed class StoryGenerationService(
    IOptions<StoryTimeOptions> options,
    IMediaAssetService mediaAssetService,
    IHttpClientFactory httpClientFactory) : IStoryGenerationService
{
    private static readonly object StoryBiblePersistenceLock = new();
    private readonly StoryTimeOptions _options = options.Value;
    private readonly MessageTemplateOptions _messages = options.Value.Messages;
    private readonly IMediaAssetService _mediaAssetService = mediaAssetService;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly bool _persistSeriesStoryBible = options.Value.Generation.PersistSeriesStoryBible;
    private readonly bool _persistContinuityFacts = options.Value.Generation.PersistContinuityFacts;
    private readonly AiStageNameOptions _aiStageNames = options.Value.Generation.AiOrchestration.StageNames;
    private readonly string _persistedRecurringCharacterAlias = NormalizePersistentRecurringCharacter(
        options.Value.Generation.Fallbacks.PersistentRecurringCharacterAlias,
        options.Value.Ui.DefaultChildName,
        options.Value.Messages);
    private readonly string _storyBibleFilePath = ResolveStoryBibleFilePath(options.Value.Generation.StoryBibleFilePath);
    private readonly ConcurrentDictionary<string, StoryBible> _seriesBibles =
        LoadStoryBibles(
            options.Value.Generation.PersistSeriesStoryBible,
            options.Value.Generation.StoryBibleFilePath,
            options.Value.Generation.PersistContinuityFacts,
            NormalizePersistentRecurringCharacter(
                options.Value.Generation.Fallbacks.PersistentRecurringCharacterAlias,
                options.Value.Ui.DefaultChildName,
                options.Value.Messages));

    public async Task<GeneratedStory> GenerateAsync(GenerateStoryRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var mode = NormalizeMode(request.Mode);
        var now = DateTimeOffset.UtcNow;
        var protagonist = ResolveProtagonist(request.ChildName);
        var sceneCount = ComputeSceneCount(request.DurationMinutes);
        var customization = ResolveOneShotCustomization(mode, request.Customization);

        StoryBible? bible = null;
        var seriesId = request.SeriesId;
        var recap = "";

        if (string.Equals(mode, StoryModes.Series, StringComparison.OrdinalIgnoreCase))
        {
            seriesId = string.IsNullOrWhiteSpace(seriesId) ? Guid.NewGuid().ToString("N") : seriesId.Trim();
            bible = _seriesBibles.GetOrAdd(seriesId, id => CreateBible(id, protagonist));
            recap = string.IsNullOrWhiteSpace(bible.LastEpisodeSummary)
                ? ApplyTemplate(
                    _options.Generation.NarrativeTemplates.SeriesRecapFirstEpisode,
                    new Dictionary<string, string>
                    {
                        ["Protagonist"] = protagonist,
                        ["ArcName"] = bible.ArcName
                    })
                : ApplyTemplate(
                    _options.Generation.NarrativeTemplates.SeriesRecapContinuation,
                    new Dictionary<string, string>
                    {
                        ["PreviousSummary"] = bible.LastEpisodeSummary
                    });
        }

        var arcName = bible?.ArcName ?? customization.ArcName ?? Pick(_options.Generation.ArcNames, _options.Generation.Fallbacks.ArcName);
        var outline = await ComposeOutlineAsync(mode, protagonist, sceneCount, arcName, bible, customization, cancellationToken);
        var scenePlan = await ComposeScenePlanAsync(outline, sceneCount, arcName, bible, cancellationToken);
        var sceneBatch = await ComposeSceneBatchAsync(scenePlan, protagonist, bible, customization, cancellationToken);
        var stitchedScenes = await StitchScenesAsync(sceneBatch, recap, bible, cancellationToken);
        var polishedScenes = await PolishScenesAsync(stitchedScenes, cancellationToken);
        var title = BuildTitle(mode, protagonist, arcName, bible, now);
        var posterLayers = _mediaAssetService.BuildPosterLayers(seriesId ?? request.SoftUserId, request.ReducedMotion);

        if (bible is not null)
        {
            bible.ArcEpisodeNumber += 1;
            bible.ArcObjective = scenePlan.LastOrDefault() ?? bible.ArcObjective;
            if (_persistContinuityFacts)
            {
                bible.ContinuityFacts.Add(ApplyTemplate(
                    _options.Generation.NarrativeTemplates.ContinuityFact,
                    new Dictionary<string, string>
                    {
                        ["EpisodeNumber"] = bible.ArcEpisodeNumber.ToString(),
                        ["Timestamp"] = now.ToString("O"),
                        ["SceneCount"] = sceneCount.ToString()
                    }));
                if (bible.ContinuityFacts.Count > _options.Generation.ContinuityFactRetentionLimit)
                {
                    bible.ContinuityFacts.RemoveAt(0);
                }
            }
            else
            {
                bible.ContinuityFacts.Clear();
            }

            bible.LastEpisodeSummary = BuildEpisodeSummary(polishedScenes.Length);
            PersistStoryBibles();
        }

        var approvalRequired = request.ApprovalRequired ?? _options.DefaultApprovalRequired;
        var storyId = Guid.NewGuid().ToString("N");
        var fullAudio = approvalRequired ? null : BuildFullAudio(storyId);

        return new GeneratedStory(
            StoryId: storyId,
            Title: title,
            Mode: mode,
            SeriesId: seriesId,
            Recap: recap,
            Scenes: polishedScenes,
            PosterLayers: posterLayers,
            ApprovalRequired: approvalRequired,
            TeaserAudio: BuildTeaserAudio(storyId),
            FullAudio: fullAudio,
            StoryBible: bible is null ? null : ToSnapshot(bible),
            ReducedMotion: request.ReducedMotion,
            GeneratedAt: now);
    }

    private string ResolveProtagonist(string? childName)
    {
        if (!string.IsNullOrWhiteSpace(childName))
        {
            return childName.Trim();
        }

        return _options.Ui.DefaultChildName.Trim();
    }

    private StoryBible CreateBible(string seriesId, string protagonist)
    {
        var arcName = Pick(_options.Generation.ArcNames, _options.Generation.Fallbacks.ArcName);
        return new StoryBible
        {
            SeriesId = seriesId,
            VisualIdentity = $"palette-{seriesId[..6]}",
            RecurringCharacter = protagonist,
            ArcName = arcName,
            ArcEpisodeNumber = 0,
            ArcObjective = ApplyTemplate(
                _options.Generation.NarrativeTemplates.ArcObjective,
                new Dictionary<string, string> { ["ArcName"] = arcName }),
            AudioAnchorMetadata = new AudioAnchorMetadata(
                ThemeTrackId: Pick(_options.Generation.ThemeTrackIds, _options.Generation.Fallbacks.ThemeTrackId),
                NarrationStyle: Pick(_options.Generation.NarrationStyles, _options.Generation.Fallbacks.NarrationStyle))
        };
    }

    private async Task<string> ComposeOutlineAsync(
        string mode,
        string protagonist,
        int sceneCount,
        string arcName,
        StoryBible? bible,
        OneShotCustomization customization,
        CancellationToken cancellationToken)
    {
        var deterministic = BuildOutline(mode, protagonist, sceneCount, arcName, bible, customization);
        var ai = await TryRunAiStageAsync(
            _aiStageNames.Outline,
            new Dictionary<string, object?>
            {
                ["mode"] = mode,
                ["protagonist"] = protagonist,
                ["sceneCount"] = sceneCount,
                ["arcName"] = arcName,
                ["customization"] = customization
            },
            cancellationToken);

        return string.IsNullOrWhiteSpace(ai?.Text) ? deterministic : ai.Text.Trim();
    }

    private async Task<string[]> ComposeScenePlanAsync(
        string outline,
        int sceneCount,
        string arcName,
        StoryBible? bible,
        CancellationToken cancellationToken)
    {
        var deterministic = BuildScenePlan(outline, sceneCount, arcName, bible);
        var ai = await TryRunAiStageAsync(
            _aiStageNames.ScenePlan,
            new Dictionary<string, object?>
            {
                ["outline"] = outline,
                ["sceneCount"] = sceneCount,
                ["arcName"] = arcName,
                ["arcEpisodeNumber"] = bible?.ArcEpisodeNumber
            },
            cancellationToken);

        var aiItems = NormalizeAiItems(ai);
        return aiItems.Length == 0 ? deterministic : aiItems;
    }

    private async Task<string[]> ComposeSceneBatchAsync(
        IReadOnlyList<string> scenePlan,
        string protagonist,
        StoryBible? bible,
        OneShotCustomization customization,
        CancellationToken cancellationToken)
    {
        var deterministic = BuildSceneBatch(scenePlan, protagonist, bible, customization);
        var ai = await TryRunAiStageAsync(
            _aiStageNames.SceneBatch,
            new Dictionary<string, object?>
            {
                ["scenePlan"] = scenePlan,
                ["protagonist"] = protagonist,
                ["arcObjective"] = bible?.ArcObjective,
                ["companionName"] = customization.CompanionName,
                ["setting"] = customization.Setting,
                ["mood"] = customization.Mood
            },
            cancellationToken);

        var aiItems = NormalizeAiItems(ai);
        return aiItems.Length == 0 ? deterministic : aiItems;
    }

    private async Task<string[]> StitchScenesAsync(
        IReadOnlyList<string> sceneBatch,
        string recap,
        StoryBible? bible,
        CancellationToken cancellationToken)
    {
        var deterministic = StitchScenes(sceneBatch, recap, bible);
        var ai = await TryRunAiStageAsync(
            _aiStageNames.Stitch,
            new Dictionary<string, object?>
            {
                ["sceneBatch"] = sceneBatch,
                ["recap"] = recap,
                ["arcEpisodeNumber"] = bible?.ArcEpisodeNumber
            },
            cancellationToken);

        var aiItems = NormalizeAiItems(ai);
        return aiItems.Length == 0 ? deterministic : aiItems;
    }

    private async Task<string[]> PolishScenesAsync(IReadOnlyList<string> stitchedScenes, CancellationToken cancellationToken)
    {
        var deterministic = PolishScenes(stitchedScenes);
        var ai = await TryRunAiStageAsync(
            _aiStageNames.Polish,
            new Dictionary<string, object?>
            {
                ["stitchedScenes"] = stitchedScenes,
                ["tone"] = _options.Generation.PolishToneTag
            },
            cancellationToken);

        var aiItems = NormalizeAiItems(ai);
        return aiItems.Length == 0 ? deterministic : aiItems;
    }

    private string BuildOutline(
        string mode,
        string protagonist,
        int sceneCount,
        string arcName,
        StoryBible? bible,
        OneShotCustomization customization)
    {
        if (string.Equals(mode, StoryModes.OneShot, StringComparison.OrdinalIgnoreCase))
        {
            var companion = string.IsNullOrWhiteSpace(customization.CompanionName)
                ? _options.Generation.Fallbacks.OneShotCompanionName
                : customization.CompanionName;
            var setting = string.IsNullOrWhiteSpace(customization.Setting)
                ? _options.Generation.Fallbacks.OneShotSetting
                : customization.Setting;
            var mood = string.IsNullOrWhiteSpace(customization.Mood)
                ? _options.Generation.Fallbacks.OneShotMood
                : customization.Mood;
            var themeTrack = string.IsNullOrWhiteSpace(customization.ThemeTrackId)
                ? _options.Generation.Fallbacks.ThemeTrackId
                : customization.ThemeTrackId;
            var narrationStyle = string.IsNullOrWhiteSpace(customization.NarrationStyle)
                ? _options.Generation.Fallbacks.NarrationStyle
                : customization.NarrationStyle;
            return ApplyTemplate(
                _options.Generation.NarrativeTemplates.OneShotOutline,
                new Dictionary<string, string>
                {
                    ["Protagonist"] = protagonist,
                    ["CompanionName"] = companion,
                    ["Mood"] = mood,
                    ["SceneCount"] = sceneCount.ToString(),
                    ["Setting"] = setting,
                    ["ThemeTrackId"] = themeTrack,
                    ["NarrationStyle"] = narrationStyle
                });
        }

        var arcContext = bible is null
            ? _options.Generation.NarrativeTemplates.SeriesOutlineStandaloneArcContext
            : ApplyTemplate(
                _options.Generation.NarrativeTemplates.SeriesOutlineArcContext,
                new Dictionary<string, string> { ["ArcName"] = bible.ArcName });
        return ApplyTemplate(
            _options.Generation.NarrativeTemplates.SeriesOutline,
            new Dictionary<string, string>
            {
                ["Protagonist"] = protagonist,
                ["ArcContext"] = arcContext,
                ["SceneCount"] = sceneCount.ToString()
            });
    }

    private string[] BuildScenePlan(string outline, int sceneCount, string arcName, StoryBible? bible)
    {
        var objectives = new List<string>(sceneCount);
        for (var sceneNumber = 1; sceneNumber <= sceneCount; sceneNumber++)
        {
            var objective = bible is null
                ? ApplyTemplate(
                    _options.Generation.NarrativeTemplates.ScenePlanStandaloneObjective,
                    new Dictionary<string, string>
                    {
                        ["SceneNumber"] = sceneNumber.ToString(),
                        ["ArcName"] = arcName
                    })
                : ApplyTemplate(
                    _options.Generation.NarrativeTemplates.ScenePlanSeriesObjective,
                    new Dictionary<string, string>
                    {
                        ["ArcName"] = bible.ArcName,
                        ["MilestoneNumber"] = (bible.ArcEpisodeNumber + sceneNumber).ToString()
                    });
            objectives.Add(objective);
        }

        if (objectives.Count > 0)
        {
            objectives[0] = ApplyTemplate(
                _options.Generation.NarrativeTemplates.ScenePlanOpening,
                new Dictionary<string, string> { ["Outline"] = outline });
        }

        return [.. objectives];
    }

    private string[] BuildSceneBatch(
        IEnumerable<string> scenePlan,
        string protagonist,
        StoryBible? bible,
        OneShotCustomization customization)
    {
        return scenePlan
            .Select((objective, index) =>
            {
                var opener = Pick(_options.Generation.CalmOpeners, _options.Generation.Fallbacks.CalmOpener);
                var transition = Pick(_options.Generation.CalmTransitions, _options.Generation.Fallbacks.CalmTransition);
                var arcNote = bible is null
                    ? ""
                    : ApplyTemplate(
                        _options.Generation.NarrativeTemplates.SceneArcNote,
                        new Dictionary<string, string> { ["ArcObjective"] = bible.ArcObjective });
                var oneShotDetail = bible is null
                    ? BuildOneShotDetail(customization)
                    : "";
                return ApplyTemplate(
                    _options.Generation.NarrativeTemplates.Scene,
                    new Dictionary<string, string>
                    {
                        ["SceneNumber"] = (index + 1).ToString(),
                        ["Opener"] = opener,
                        ["Protagonist"] = protagonist,
                        ["Objective"] = objective,
                        ["Transition"] = transition,
                        ["OneShotDetail"] = oneShotDetail,
                        ["ArcNote"] = arcNote
                    });
            })
            .ToArray();
    }

    private string BuildOneShotDetail(OneShotCustomization customization)
    {
        var detailParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(customization.CompanionName))
        {
            detailParts.Add(ApplyTemplate(
                _options.Generation.NarrativeTemplates.OneShotDetailCompanion,
                new Dictionary<string, string> { ["Value"] = customization.CompanionName }));
        }

        if (!string.IsNullOrWhiteSpace(customization.Setting))
        {
            detailParts.Add(ApplyTemplate(
                _options.Generation.NarrativeTemplates.OneShotDetailSetting,
                new Dictionary<string, string> { ["Value"] = customization.Setting }));
        }

        if (!string.IsNullOrWhiteSpace(customization.Mood))
        {
            detailParts.Add(ApplyTemplate(
                _options.Generation.NarrativeTemplates.OneShotDetailMood,
                new Dictionary<string, string> { ["Value"] = customization.Mood }));
        }

        return detailParts.Count == 0 ? "" : $" ({string.Join(", ", detailParts)})";
    }

    private string[] StitchScenes(IReadOnlyList<string> sceneBatch, string recap, StoryBible? bible)
    {
        var stitched = new string[sceneBatch.Count];
        for (var index = 0; index < sceneBatch.Count; index++)
        {
            var continuityLead = index == 0 && !string.IsNullOrWhiteSpace(recap)
                ? $"{recap} "
                : string.Empty;

            var arcLead = bible is null
                ? string.Empty
                : ApplyTemplate(
                    _options.Generation.NarrativeTemplates.StitchedArcLead,
                    new Dictionary<string, string> { ["EpisodeNumber"] = (bible.ArcEpisodeNumber + 1).ToString() });

            stitched[index] = $"{arcLead}{continuityLead}{sceneBatch[index]}".Trim();
        }

        return stitched;
    }

    private string[] PolishScenes(IReadOnlyList<string> stitchedScenes)
    {
        var closer = Pick(_options.Generation.CalmClosers, _options.Generation.Fallbacks.CalmCloser);
        return stitchedScenes.Select((scene, index) =>
        {
            var ending = index == stitchedScenes.Count - 1
                ? $" {closer}"
                : "";
            return $"{scene}{ending}";
        }).ToArray();
    }

    private string BuildTitle(string mode, string protagonist, string arcName, StoryBible? bible, DateTimeOffset now)
    {
        var template = Pick(_options.Generation.TitleTemplates, _options.Generation.Fallbacks.TitleTemplate);
        var modeLabel = ResolveModeLabel(mode);
        var episodeNumber = bible is null ? 1 : bible.ArcEpisodeNumber + 1;

        return template
            .Replace("{ChildName}", protagonist, StringComparison.Ordinal)
            .Replace("{ArcName}", arcName, StringComparison.Ordinal)
            .Replace("{ModeLabel}", modeLabel, StringComparison.Ordinal)
            .Replace("{EpisodeNumber}", episodeNumber.ToString(), StringComparison.Ordinal)
            .Replace("{DateStamp}", now.ToString("MMdd-HHmm"), StringComparison.Ordinal);
    }

    private string ResolveModeLabel(string mode)
    {
        return string.Equals(mode, StoryModes.Series, StringComparison.OrdinalIgnoreCase)
            ? _options.Generation.ModeLabels.Series
            : _options.Generation.ModeLabels.OneShot;
    }

    private static StoryBibleSnapshot ToSnapshot(StoryBible bible) => new(
        SeriesId: bible.SeriesId,
        VisualIdentity: bible.VisualIdentity,
        RecurringCharacter: bible.RecurringCharacter,
        ArcName: bible.ArcName,
        ArcEpisodeNumber: bible.ArcEpisodeNumber,
        ArcObjective: bible.ArcObjective,
        PreviousEpisodeSummary: bible.LastEpisodeSummary,
        AudioAnchorMetadata: bible.AudioAnchorMetadata);

    private string BuildEpisodeSummary(int sceneCount) =>
        ApplyTemplate(
            _options.Generation.NarrativeTemplates.EpisodeSummary,
            new Dictionary<string, string> { ["SceneCount"] = Math.Max(1, sceneCount).ToString() });

    private string BuildTeaserAudio(string storyId)
    {
        return _mediaAssetService.BuildAudioDataUri(
            storyId,
            _options.Generation.TeaserDurationSeconds,
            _options.Generation.TeaserAudioAmplitudeScale);
    }

    private string BuildFullAudio(string storyId)
    {
        return _mediaAssetService.BuildAudioDataUri(
            storyId,
            _options.Generation.FullDurationSeconds,
            _options.Generation.FullAudioAmplitudeScale);
    }

    private static string Pick(IReadOnlyList<string> values, string fallback)
    {
        if (values.Count == 0)
        {
            return fallback;
        }

        var index = Random.Shared.Next(values.Count);
        return values[index];
    }

    private string NormalizeMode(string mode)
    {
        if (_options.Generation.OneShotModeAliases.Any(alias =>
            string.Equals(alias, mode, StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(mode, StoryModes.OneShot, StringComparison.OrdinalIgnoreCase))
        {
            return StoryModes.OneShot;
        }

        return StoryModes.Series;
    }

    private int ComputeSceneCount(int durationMinutes)
    {
        var minutesPerScene = Math.Max(1, _options.Generation.MinutesPerScene);
        var boundedDuration = Math.Max(durationMinutes, minutesPerScene);
        var estimate = (int)Math.Ceiling(boundedDuration / (double)minutesPerScene);
        var minScenes = Math.Max(1, _options.Generation.MinSceneCount);
        var maxScenes = Math.Max(minScenes, _options.Generation.MaxSceneCount);
        return Math.Clamp(estimate, minScenes, maxScenes);
    }

    private OneShotCustomization ResolveOneShotCustomization(string mode, OneShotCustomizationRequest? customization)
    {
        if (!string.Equals(mode, StoryModes.OneShot, StringComparison.OrdinalIgnoreCase) || customization is null)
        {
            return OneShotCustomization.Empty;
        }

        return new OneShotCustomization(
            ArcName: NormalizeOptionalValue(customization.ArcName),
            CompanionName: NormalizeOptionalValue(customization.CompanionName),
            Setting: NormalizeOptionalValue(customization.Setting),
            Mood: NormalizeOptionalValue(customization.Mood),
            ThemeTrackId: NormalizeOptionalValue(customization.ThemeTrackId),
            NarrationStyle: NormalizeOptionalValue(customization.NarrationStyle));
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private async Task<AiStageResponse?> TryRunAiStageAsync(string stage, IReadOnlyDictionary<string, object?> payload, CancellationToken cancellationToken)
    {
        if (!_options.Generation.AiOrchestration.Enabled)
        {
            return null;
        }

        var aiOptions = _options.Generation.AiOrchestration;
        var endpoint = aiOptions.Endpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return aiOptions.LocalFallbackEnabled
                ? null
                : throw new InvalidOperationException(_messages.Internal("AiOrchestrationEndpointRequiredWhenEnabled"));
        }

        var client = _httpClientFactory.CreateClient(nameof(StoryGenerationService));
        client.Timeout = TimeSpan.FromSeconds(Math.Max(1, aiOptions.TimeoutSeconds));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new AiStageRequest(stage, aiOptions.Model, payload))
            };

            var apiKey = aiOptions.ApiKey;
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                if (aiOptions.LocalFallbackEnabled)
                {
                    return null;
                }

                throw new InvalidOperationException(
                    _messages.Internal(
                        "AiOrchestrationStageFailedWithStatus",
                        ("Stage", stage),
                        ("StatusCode", ((int)response.StatusCode).ToString())));
            }

            var aiResponse = await response.Content.ReadFromJsonAsync<AiStageResponse>(cancellationToken: cancellationToken);
            if (aiResponse is null)
            {
                return aiOptions.LocalFallbackEnabled
                    ? null
                    : throw new InvalidOperationException(
                        _messages.Internal(
                            "AiOrchestrationStageReturnedEmptyResponse",
                            ("Stage", stage)));
            }

            return aiResponse;
        }
        catch (HttpRequestException) when (aiOptions.LocalFallbackEnabled)
        {
            return null;
        }
        catch (TaskCanceledException) when (aiOptions.LocalFallbackEnabled)
        {
            return null;
        }
        catch (JsonException) when (aiOptions.LocalFallbackEnabled)
        {
            return null;
        }
    }

    private static string[] NormalizeAiItems(AiStageResponse? response)
    {
        if (response is null)
        {
            return [];
        }

        if (response.Items is { Count: > 0 })
        {
            return response.Items.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToArray();
        }

        if (!string.IsNullOrWhiteSpace(response.Text))
        {
            return response.Text
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
        }

        return [];
    }

    private sealed record OneShotCustomization(
        string? ArcName,
        string? CompanionName,
        string? Setting,
        string? Mood,
        string? ThemeTrackId,
        string? NarrationStyle)
    {
        public static OneShotCustomization Empty { get; } = new(null, null, null, null, null, null);
    }

    private sealed record AiStageRequest(string Stage, string Model, IReadOnlyDictionary<string, object?> Payload);

    private sealed record AiStageResponse(string? Text, IReadOnlyList<string>? Items);

    private static string ApplyTemplate(string template, IReadOnlyDictionary<string, string> values)
    {
        var rendered = template;
        foreach (var (key, value) in values)
        {
            rendered = rendered.Replace($"{{{key}}}", value, StringComparison.Ordinal);
        }

        return rendered;
    }

    private static string NormalizePersistentRecurringCharacter(
        string configuredAlias,
        string defaultAlias,
        MessageTemplateOptions messages)
    {
        var normalized = configuredAlias.Trim();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        var normalizedDefault = defaultAlias.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedDefault))
        {
            return normalizedDefault;
        }

        throw new InvalidOperationException(messages.Internal("PersistentRecurringCharacterAliasMustBeConfigured"));
    }

    private void PersistStoryBibles()
    {
        if (!_persistSeriesStoryBible)
        {
            return;
        }

        lock (StoryBiblePersistenceLock)
        {
            var directory = Path.GetDirectoryName(_storyBibleFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var persisted = _seriesBibles.Values
                .OrderBy(bible => bible.SeriesId, StringComparer.Ordinal)
                .Select(bible => new PersistedStoryBible(
                    bible.SeriesId,
                    bible.VisualIdentity,
                    _persistedRecurringCharacterAlias,
                    bible.ArcName,
                    bible.ArcEpisodeNumber,
                    bible.ArcObjective,
                    bible.LastEpisodeSummary,
                    _persistContinuityFacts ? [.. bible.ContinuityFacts] : null,
                    bible.AudioAnchorMetadata))
                .ToArray();

            var tempFilePath = $"{_storyBibleFilePath}.{Guid.NewGuid():N}.tmp";
            File.WriteAllText(
                tempFilePath,
                JsonSerializer.Serialize(
                    persisted,
                    new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    }));
            File.Move(tempFilePath, _storyBibleFilePath, overwrite: true);
        }
    }

    private static ConcurrentDictionary<string, StoryBible> LoadStoryBibles(
        bool persistEnabled,
        string configuredPath,
        bool persistContinuityFacts,
        string recurringCharacterFallback)
    {
        var bibles = new ConcurrentDictionary<string, StoryBible>(StringComparer.Ordinal);
        if (!persistEnabled)
        {
            return bibles;
        }

        var filePath = ResolveStoryBibleFilePath(configuredPath);
        List<PersistedStoryBible> restored;
        lock (StoryBiblePersistenceLock)
        {
            if (!File.Exists(filePath))
            {
                return bibles;
            }

            var raw = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return bibles;
            }

            restored = JsonSerializer.Deserialize<List<PersistedStoryBible>>(raw) ?? [];
        }

        foreach (var item in restored)
        {
            var bible = new StoryBible
            {
                SeriesId = item.SeriesId,
                VisualIdentity = item.VisualIdentity,
                RecurringCharacter = string.IsNullOrWhiteSpace(item.RecurringCharacter)
                    ? recurringCharacterFallback
                    : item.RecurringCharacter,
                ArcName = item.ArcName,
                ArcEpisodeNumber = item.ArcEpisodeNumber,
                ArcObjective = string.IsNullOrWhiteSpace(item.ArcObjective) ? item.ArcName : item.ArcObjective,
                LastEpisodeSummary = item.LastEpisodeSummary ?? "",
                AudioAnchorMetadata = item.AudioAnchorMetadata
            };
            if (persistContinuityFacts && item.ContinuityFacts is { Count: > 0 })
            {
                bible.ContinuityFacts.AddRange(item.ContinuityFacts.Where(fact => !string.IsNullOrWhiteSpace(fact)));
            }

            bibles[item.SeriesId] = bible;
        }

        return bibles;
    }

    private static string ResolveStoryBibleFilePath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.Combine(AppContext.BaseDirectory, configuredPath);
    }

    private sealed record PersistedStoryBible(
        string SeriesId,
        string VisualIdentity,
        string RecurringCharacter,
        string ArcName,
        int ArcEpisodeNumber,
        string ArcObjective,
        string? LastEpisodeSummary,
        IReadOnlyList<string>? ContinuityFacts,
        AudioAnchorMetadata AudioAnchorMetadata);
}
