using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using StoryTime.Api.Contracts;
using StoryTime.Api.Services;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace StoryTime.Api.Tests.Unit;

public sealed class StoryGenerationServiceTests
{
    [Fact]
    public async Task GenerateAsync_PreservesSeriesContinuityAcrossContinuation()
    {
        var service = CreateService();

        var first = await service.GenerateAsync(
            new GenerateStoryRequest("user-series", "Mila", "series", 6, null, null, false),
            CancellationToken.None);

        var second = await service.GenerateAsync(
            new GenerateStoryRequest("user-series", "Mila", "series", 6, first.SeriesId, null, false, StoryBible: first.StoryBible),
            CancellationToken.None);

        Assert.NotNull(first.SeriesId);
        Assert.Equal(first.SeriesId, second.SeriesId);
        Assert.StartsWith("Previously:", second.Recap);
        Assert.NotNull(second.StoryBible);
        Assert.Equal(2, second.StoryBible!.ArcEpisodeNumber);
        Assert.All(second.Scenes, scene => Assert.Contains("Mila", scene));
    }

    [Fact]
    public async Task GenerateAsync_UsesClientStoryBibleSnapshot_WithoutServerPersistenceSurface()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "storytime-bible-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var storyBiblePath = Path.Combine(tempRoot, "story-bibles.json");

        try
        {
            var options = StoryTimeOptionsFactory.Create();
            options.Generation.Fallbacks.PersistentRecurringCharacterAlias = "Stella";

            var firstService = CreateService(options);
            var first = await firstService.GenerateAsync(
                new GenerateStoryRequest("user-series-persist", "Mila", "series", 6, null, null, false),
                CancellationToken.None);

            var secondService = CreateService(options);
            var continuation = await secondService.GenerateAsync(
                new GenerateStoryRequest("user-series-persist", "Mila", "series", 6, first.SeriesId, null, false, StoryBible: first.StoryBible),
                CancellationToken.None);

            Assert.StartsWith("Previously:", continuation.Recap, StringComparison.Ordinal);
            Assert.NotNull(continuation.StoryBible);
            Assert.Equal(2, continuation.StoryBible!.ArcEpisodeNumber);
            Assert.NotNull(first.StoryBible);
            Assert.Equal(first.StoryBible!.VisualIdentity, continuation.StoryBible.VisualIdentity);
            Assert.Equal(first.StoryBible.AudioAnchorMetadata, continuation.StoryBible.AudioAnchorMetadata);
            Assert.False(File.Exists(storyBiblePath));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_UsesConfiguredNarrativeTemplates()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.NarrativeTemplates.SeriesRecapFirstEpisode = "BEGIN::{Protagonist}:{ArcName}";
        options.Generation.NarrativeTemplates.SeriesRecapContinuation = "RECAP::{PreviousSummary}";
        options.Generation.NarrativeTemplates.EpisodeSummary = "SUMMARY::{SceneCount}";
        options.Generation.NarrativeTemplates.Scene = "SCENE::{SceneNumber}:{Protagonist}:{Objective}:{Transition}{OneShotDetail}{ArcNote}";
        options.Generation.NarrativeTemplates.SceneArcNote = "|ARC::{ArcObjective}";
        var service = CreateService(options);

        var first = await service.GenerateAsync(
            new GenerateStoryRequest("user-template-series", "Mila", "series", 6, null, null, false),
            CancellationToken.None);
        var second = await service.GenerateAsync(
            new GenerateStoryRequest("user-template-series", "Mila", "series", 6, first.SeriesId, null, false, StoryBible: first.StoryBible),
            CancellationToken.None);

        Assert.StartsWith("BEGIN::Mila:", first.Recap, StringComparison.Ordinal);
        Assert.StartsWith("RECAP::SUMMARY::", second.Recap, StringComparison.Ordinal);
        Assert.Contains(first.Scenes, scene => scene.Contains("SCENE::1:Mila:", StringComparison.Ordinal));
        Assert.Contains(second.Scenes, scene => scene.Contains("SCENE::", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GenerateAsync_ProvidesArcStateAndAnchoredAudioMetadata()
    {
        var service = CreateService();

        var generated = await service.GenerateAsync(
            new GenerateStoryRequest("user-arc", "Ari", "series", 8, null, null, false),
            CancellationToken.None);

        Assert.NotNull(generated.StoryBible);
        Assert.False(string.IsNullOrWhiteSpace(generated.StoryBible!.ArcName));
        Assert.False(string.IsNullOrWhiteSpace(generated.StoryBible.AudioAnchorMetadata.ThemeTrackId));
        Assert.False(string.IsNullOrWhiteSpace(generated.StoryBible.AudioAnchorMetadata.NarrationStyle));
        Assert.NotEmpty(generated.Scenes);
    }

    [Fact]
    public async Task GenerateAsync_UsesProceduralPosterFallbackWithinBudget_AndSupportsReducedMotion()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.ForceProceduralPosterFallback = true;
        var service = CreateService(options);

        var generated = await service.GenerateAsync(
            new GenerateStoryRequest("user-fallback", "Noa", "one-shot", 5, null, null, false, ReducedMotion: true),
            CancellationToken.None);

        Assert.InRange(generated.PosterLayers.Count, 3, 5);
        Assert.Equal(["BACKGROUND", "MIDGROUND_1", "FOREGROUND", "PARTICLES"], generated.PosterLayers.Select(layer => layer.Role).ToArray());
        Assert.All(generated.PosterLayers, layer => Assert.StartsWith("data:image/svg+xml;base64,", layer.DataUri));
        Assert.All(generated.PosterLayers, layer => Assert.Equal(0, layer.SpeedMultiplier));
        Assert.True(generated.ReducedMotion);
    }

    [Fact]
    public async Task GenerateAsync_UsesConfiguredPosterRoleSpeedMultipliers()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.ForceProceduralPosterFallback = true;
        options.Generation.PosterRoleSpeedMultipliers["BACKGROUND"] = 0.15;
        options.Generation.PosterRoleSpeedMultipliers["MIDGROUND_1"] = 0.45;
        options.Generation.PosterRoleSpeedMultipliers["FOREGROUND"] = 0.95;
        options.Generation.PosterRoleSpeedMultipliers["PARTICLES"] = 1.25;
        var service = CreateService(options);

        var generated = await service.GenerateAsync(
            new GenerateStoryRequest("user-speed-map", "Noa", "one-shot", 5, null, null, false),
            CancellationToken.None);

        var speedsByRole = generated.PosterLayers.ToDictionary(layer => layer.Role, layer => layer.SpeedMultiplier, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(0.15, speedsByRole["BACKGROUND"]);
        Assert.Equal(0.45, speedsByRole["MIDGROUND_1"]);
        Assert.Equal(0.95, speedsByRole["FOREGROUND"]);
        Assert.Equal(1.25, speedsByRole["PARTICLES"]);
    }

    [Fact]
    public async Task GenerateAsync_UsesConfiguredProceduralPosterTuning()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.ForceProceduralPosterFallback = true;
        options.Generation.ProceduralPoster.FallbackStarCount = 3;
        options.Generation.ProceduralPoster.FallbackOpacity = 0.11;
        var service = CreateService(options);

        var generated = await service.GenerateAsync(
            new GenerateStoryRequest("user-poster-tuning", "Noa", "one-shot", 5, null, null, false),
            CancellationToken.None);

        var firstLayer = generated.PosterLayers[0];
        var payload = firstLayer.DataUri["data:image/svg+xml;base64,".Length..];
        var svg = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        Assert.Equal(3, svg.Split("<circle", StringSplitOptions.None).Length - 1);
        Assert.Contains("fill-opacity='0.11'", svg, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsFullNarrationWhenApprovalNotRequired()
    {
        var service = CreateService();

        var generated = await service.GenerateAsync(
            new GenerateStoryRequest("user-audio", "Noa", "one-shot", 5, null, false, false),
            CancellationToken.None);

        Assert.NotNull(generated.FullAudio);
        Assert.StartsWith("data:audio/wav;base64,", generated.FullAudio);
        Assert.StartsWith("data:audio/wav;base64,", generated.TeaserAudio);
    }

    [Fact]
    public async Task GenerateAsync_UsesExternalPosterAndNarrationProviders_WhenAvailable()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AiOrchestration.Enabled = false;
        var service = CreateService(options, new TestHttpClientFactory(new ProviderSuccessHandler()));

        var generated = await service.GenerateAsync(
            new GenerateStoryRequest("user-external-media", "Noa", "one-shot", 5, null, false, false),
            CancellationToken.None);

        Assert.All(generated.PosterLayers, layer => Assert.StartsWith("data:image/png;base64,", layer.DataUri));
        Assert.Equal("data:audio/wav;base64,VEVTVA==", generated.TeaserAudio);
        Assert.Equal("data:audio/wav;base64,VEVTVA==", generated.FullAudio);
    }

    [Fact]
    public async Task GenerateAsync_UsesOpenRouterContract_WhenEndpointTargetsOpenRouter()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AiOrchestration.EnforceOpenRouterEndpoint = true;
        options.Generation.AiOrchestration.LocalFallbackEnabled = false;
        options.Generation.AiOrchestration.Endpoint = "https://openrouter.ai/api/v1/chat/completions";
        options.Generation.AiOrchestration.ApiKey = "openrouter-test-key";
        var handler = new OpenRouterContractHandler();
        var service = CreateService(options, new TestHttpClientFactory(handler));

        var generated = await service.GenerateAsync(
            new GenerateStoryRequest("user-openrouter", "Noa", "one-shot", 5, null, false, false),
            CancellationToken.None);

        Assert.NotEmpty(handler.OpenRouterRequests);
        var firstRequest = handler.OpenRouterRequests[0];
        Assert.Equal("Bearer", firstRequest.AuthorizationScheme);
        Assert.Equal("openrouter-test-key", firstRequest.AuthorizationParameter);
        Assert.Equal(options.Generation.AiOrchestration.OpenRouterReferer, firstRequest.HttpReferer);
        Assert.Equal(options.Generation.AiOrchestration.OpenRouterTitle, firstRequest.XTitle);

        using var payload = JsonDocument.Parse(firstRequest.Body);
        Assert.Equal("storytime-orchestrator", payload.RootElement.GetProperty("model").GetString());
        var messages = payload.RootElement.GetProperty("messages");
        Assert.True(messages.GetArrayLength() >= 2);
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal(
            options.Generation.AiOrchestration.StageResponseFormatInstruction,
            messages[0].GetProperty("content").GetString());
        Assert.Contains("\"stage\":\"outline\"", messages[1].GetProperty("content").GetString(), StringComparison.Ordinal);
        Assert.Contains(generated.Scenes, scene => scene.Contains("AI polish", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GenerateAsync_ThrowsConfiguredError_WhenAiEndpointIsNotOpenRouter()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AiOrchestration.Enabled = true;
        options.Generation.AiOrchestration.Endpoint = "https://provider.storytime.test/storytime/ai";
        var service = CreateService(options, new TestHttpClientFactory(new OpenRouterContractHandler()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAsync(
                new GenerateStoryRequest("user-openrouter-invalid-endpoint", "Noa", "one-shot", 5, null, false, false),
                CancellationToken.None));

        Assert.Equal(
            options.Messages.Internal("AiOrchestrationEndpointMustTargetOpenRouter"),
            exception.Message);
    }

    [Fact]
    public async Task GenerateAsync_ThrowsExplicitAiStageError_WhenOpenRouterFails()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AiOrchestration.Enabled = true;
        options.Generation.AiOrchestration.LocalFallbackEnabled = false;
        options.Generation.AiOrchestration.Endpoint = "https://openrouter.ai/api/v1/chat/completions";
        options.Generation.AiOrchestration.ApiKey = "openrouter-test-key";
        var service = CreateService(options, new TestHttpClientFactory(new OpenRouterFailureHandler()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAsync(
                new GenerateStoryRequest("user-ai-failure", "Noa", "one-shot", 5, null, false, false),
                CancellationToken.None));

        Assert.Contains("outline", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("503", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_ThrowsExplicitPosterProviderError_WhenFallbackIsDisabled()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AiOrchestration.Enabled = false;
        options.Generation.PosterModelProvider.LocalFallbackEnabled = false;
        var service = CreateService(options, new TestHttpClientFactory(new PosterProviderFailureHandler()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAsync(
                new GenerateStoryRequest("user-poster-failure", "Noa", "one-shot", 5, null, false, false),
                CancellationToken.None));

        Assert.Contains("PosterModelProvider failed with status 502", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_UsesProceduralFallbackWhenPosterModelFails()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.PosterModelFailureSeedPrefixes = ["user-model-fail"];
        options.Generation.ForceProceduralPosterFallback = false;
        var service = CreateService(options);

        var generated = await service.GenerateAsync(
            new GenerateStoryRequest("user-model-fail-001", "Noa", "one-shot", 5, null, null, false),
            CancellationToken.None);

        Assert.All(generated.PosterLayers, layer => Assert.StartsWith("data:image/svg+xml;base64,", layer.DataUri));
    }

    [Fact]
    public async Task GenerateAsync_AppliesOneShotCustomizationToNarrative()
    {
        var service = CreateService();

        var generated = await service.GenerateAsync(
            new GenerateStoryRequest(
                "user-custom",
                "Noa",
                "one-shot",
                6,
                null,
                null,
                false,
                Customization: new OneShotCustomizationRequest(
                    ArcName: "Coral Bay",
                    CompanionName: "Pip",
                    Setting: "the floating lantern docks",
                    Mood: "curious and gentle",
                    ThemeTrackId: "night-chimes",
                    NarrationStyle: "calm-storyteller")),
            CancellationToken.None);

        Assert.Contains("Coral Bay", generated.Title);
        Assert.Contains(generated.Scenes, scene => scene.Contains("Pip", StringComparison.Ordinal));
        Assert.Contains(generated.Scenes, scene => scene.Contains("floating lantern docks", StringComparison.Ordinal));
        Assert.Contains(generated.Scenes, scene => scene.Contains("curious and gentle", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GenerateAsync_UsesConfiguredOneShotFallbackValuesWhenCustomizationIsMissing()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.Fallbacks.OneShotCompanionName = "Nori";
        options.Generation.Fallbacks.OneShotSetting = "lantern vale";
        options.Generation.Fallbacks.OneShotMood = "dreamy and calm";
        options.Generation.Fallbacks.ThemeTrackId = "twilight-hum";
        options.Generation.Fallbacks.NarrationStyle = "midnight-whisper";
        var service = CreateService(options);

        var generated = await service.GenerateAsync(
            new GenerateStoryRequest("user-fallbacks", "Noa", "one-shot", 6, null, null, false, Customization: null),
            CancellationToken.None);

        Assert.Contains(generated.Scenes, scene => scene.Contains("Nori", StringComparison.Ordinal));
        Assert.Contains(generated.Scenes, scene => scene.Contains("lantern vale", StringComparison.Ordinal));
        Assert.Contains(generated.Scenes, scene => scene.Contains("dreamy and calm", StringComparison.Ordinal));
        Assert.Contains(generated.Scenes, scene => scene.Contains("twilight-hum", StringComparison.Ordinal));
        Assert.Contains(generated.Scenes, scene => scene.Contains("midnight-whisper", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GenerateAsync_UsesConfiguredModeLabelsInTitle()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.ModeLabels.Series = "Saga";
        options.Generation.ModeLabels.OneShot = "Single";
        var service = CreateService(options);

        var seriesStory = await service.GenerateAsync(
            new GenerateStoryRequest("user-mode-series", "Noa", "series", 6, null, null, false),
            CancellationToken.None);
        var oneShotStory = await service.GenerateAsync(
            new GenerateStoryRequest("user-mode-one-shot", "Noa", "one-shot", 6, null, null, false),
            CancellationToken.None);

        Assert.Contains("(Saga ", seriesStory.Title, StringComparison.Ordinal);
        Assert.Contains("(Single ", oneShotStory.Title, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_ThrowsWhenFallbackBudgetIsInvalid()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.ForceProceduralPosterFallback = true;
        options.Generation.ProceduralPosterFallbackBudgetMilliseconds = 0;
        var service = CreateService(options);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAsync(
                new GenerateStoryRequest("user-invalid-budget", "Noa", "one-shot", 5, null, null, false),
                CancellationToken.None));

        Assert.Contains("ProceduralPosterFallbackBudgetMilliseconds", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_ThrowsWhenProceduralPosterFallbackExceedsConfiguredBudget()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.ForceProceduralPosterFallback = true;
        options.Generation.ProceduralPosterFallbackBudgetMilliseconds = 1;
        options.Generation.ProceduralPoster.FallbackStarCount = 5000;
        var service = CreateService(options);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAsync(
                new GenerateStoryRequest("user-exceeded-budget", "Noa", "one-shot", 5, null, null, false),
                CancellationToken.None));

        Assert.Contains("exceeded budget", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static StoryGenerationService CreateService(
        StoryTimeOptions? options = null,
        IHttpClientFactory? httpClientFactory = null)
    {
        var resolvedOptions = options ?? StoryTimeOptionsFactory.Create();
        if (httpClientFactory is null)
        {
            resolvedOptions.Generation.AiOrchestration.Enabled = false;
        }
        var optionsWrapper = Options.Create(resolvedOptions);
        var resolvedHttpClientFactory = httpClientFactory ?? new TestHttpClientFactory();
        var mediaAssetService = new ProceduralMediaAssetService(
            optionsWrapper,
            NullLogger<ProceduralMediaAssetService>.Instance,
            resolvedHttpClientFactory);
        return new StoryGenerationService(optionsWrapper, mediaAssetService, resolvedHttpClientFactory);
    }

    private sealed class TestHttpClientFactory(HttpMessageHandler? handler = null) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler ?? new ThrowingHandler());
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("External providers are unavailable in unit tests.");
    }

    private sealed class ProviderSuccessHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.EndsWith("/storytime/poster", StringComparison.Ordinal))
            {
                const string posterPayload = """
{"layers":[{"role":"BACKGROUND","dataUri":"data:image/png;base64,AAA="},{"role":"MIDGROUND_1","dataUri":"data:image/png;base64,BBB="},{"role":"FOREGROUND","dataUri":"data:image/png;base64,CCC="},{"role":"PARTICLES","dataUri":"data:image/png;base64,DDD="}]}
""";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(posterPayload, Encoding.UTF8, "application/json")
                });
            }

            if (path.EndsWith("/storytime/narration", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"dataUri":"data:audio/wav;base64,VEVTVA=="}""",
                        Encoding.UTF8,
                        "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }

    private sealed class OpenRouterContractHandler : HttpMessageHandler
    {
        public List<OpenRouterRequestLog> OpenRouterRequests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var host = request.RequestUri?.Host ?? string.Empty;

            if (string.Equals(host, "openrouter.ai", StringComparison.OrdinalIgnoreCase))
            {
                var body = request.Content is null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken);

                OpenRouterRequests.Add(
                    new OpenRouterRequestLog(
                        Body: body,
                        AuthorizationScheme: request.Headers.Authorization?.Scheme,
                        AuthorizationParameter: request.Headers.Authorization?.Parameter,
                        HttpReferer: request.Headers.TryGetValues("HTTP-Referer", out var referers)
                            ? referers.FirstOrDefault()
                            : null,
                        XTitle: request.Headers.TryGetValues("X-Title", out var titles)
                            ? titles.FirstOrDefault()
                            : null));

                object stageResponse = body.Contains("\"stage\":\"outline\"", StringComparison.Ordinal)
                    ? new { text = "AI outline" }
                    : new { items = new[] { "AI polish scene one", "AI polish scene two" } };
                var completionPayload = JsonSerializer.Serialize(new
                {
                    choices = new[]
                    {
                        new
                        {
                            message = new
                            {
                                content = JsonSerializer.Serialize(stageResponse)
                            }
                        }
                    }
                });

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(completionPayload, Encoding.UTF8, "application/json")
                };
            }

            if (path.EndsWith("/storytime/poster", StringComparison.Ordinal))
            {
                const string posterPayload = """
{"layers":[{"role":"BACKGROUND","dataUri":"data:image/png;base64,AAA="},{"role":"MIDGROUND_1","dataUri":"data:image/png;base64,BBB="},{"role":"FOREGROUND","dataUri":"data:image/png;base64,CCC="},{"role":"PARTICLES","dataUri":"data:image/png;base64,DDD="}]}
""";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(posterPayload, Encoding.UTF8, "application/json")
                };
            }

            if (path.EndsWith("/storytime/narration", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"dataUri":"data:audio/wav;base64,VEVTVA=="}""",
                        Encoding.UTF8,
                        "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }
    }

    private sealed class OpenRouterFailureHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var host = request.RequestUri?.Host ?? string.Empty;
            if (string.Equals(host, "openrouter.ai", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }

    private sealed class PosterProviderFailureHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.EndsWith("/storytime/poster", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }

    private sealed record OpenRouterRequestLog(
        string Body,
        string? AuthorizationScheme,
        string? AuthorizationParameter,
        string? HttpReferer,
        string? XTitle);
}
