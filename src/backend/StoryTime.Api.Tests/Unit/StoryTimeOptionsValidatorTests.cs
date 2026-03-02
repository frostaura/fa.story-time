using StoryTime.Api;

namespace StoryTime.Api.Tests.Unit;

public sealed class StoryTimeOptionsValidatorTests
{
    [Fact]
    public void Create_UsesDeterministicProviderEndpointDefaults()
    {
        Environment.SetEnvironmentVariable("STORYTIME_TEST_PROVIDER_BASE_URL", null);
        var options = StoryTimeOptionsFactory.Create();

        Assert.Equal("http://localhost:19081/storytime/poster", options.Generation.PosterModelProvider.Endpoint);
        Assert.Equal("http://localhost:19081/storytime/narration", options.Generation.NarrationProvider.Endpoint);
        Assert.Equal("https://openrouter.ai/api/v1/chat/completions", options.Generation.AiOrchestration.Endpoint);
        Assert.Equal("http://localhost:19081/storytime/checkout", options.Checkout.Provider.Endpoint);
    }

    [Fact]
    public void Create_UsesConfiguredProviderEndpointBaseUrl()
    {
        Environment.SetEnvironmentVariable("STORYTIME_TEST_PROVIDER_BASE_URL", "https://provider.storytime.test/base");
        try
        {
            var options = StoryTimeOptionsFactory.Create();

            Assert.Equal("https://provider.storytime.test/storytime/poster", options.Generation.PosterModelProvider.Endpoint);
            Assert.Equal("https://provider.storytime.test/storytime/narration", options.Generation.NarrationProvider.Endpoint);
            Assert.Equal("https://openrouter.ai/api/v1/chat/completions", options.Generation.AiOrchestration.Endpoint);
            Assert.Equal("https://provider.storytime.test/storytime/checkout", options.Checkout.Provider.Endpoint);
        }
        finally
        {
            Environment.SetEnvironmentVariable("STORYTIME_TEST_PROVIDER_BASE_URL", null);
        }
    }

    [Fact]
    public void Validate_FailsWhenOpenRouterHeadersAreMissing()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AiOrchestration.EnforceOpenRouterEndpoint = true;
        options.Generation.AiOrchestration.Endpoint = "https://openrouter.ai/api/v1/chat/completions";
        options.Generation.AiOrchestration.OpenRouterReferer = "";
        options.Generation.AiOrchestration.OpenRouterTitle = "";

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("OpenRouterReferer", StringComparison.Ordinal));
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("OpenRouterTitle", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_AcceptsConfiguredPosterRoleSpeedMap()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.PosterRoleSpeedMultipliers["BACKGROUND"] = 0.15;
        options.Generation.PosterRoleSpeedMultipliers["MIDGROUND_1"] = 0.4;
        options.Generation.PosterRoleSpeedMultipliers["MIDGROUND_2"] = 0.7;
        options.Generation.PosterRoleSpeedMultipliers["FOREGROUND"] = 0.95;
        options.Generation.PosterRoleSpeedMultipliers["PARTICLES"] = 1.2;
        SyncLayerSpeedsWithMap(options);

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_FailsWhenNarrativeLeakageMarkersAreMissing()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Catalog.NarrativeLeakageMarkers.Clear();

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("StoryTime:Catalog:NarrativeLeakageMarkers", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenApiRouteIsMissing()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.ApiRoutes.HomeStatus = "";

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("StoryTime:ApiRoutes:HomeStatus", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenParentAssertionTypeIsMissing()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.ParentGate.AssertionType = "";

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("StoryTime:ParentGate:AssertionType", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenPosterRoleSpeedMapMissesRequiredRoles()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.PosterRoleSpeedMultipliers.Remove("BACKGROUND");

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("PosterRoleSpeedMultipliers must include 'BACKGROUND'", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenNarrativeTemplatesAreMissing()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.NarrativeTemplates.Scene = "";

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("StoryTime:Generation:NarrativeTemplates", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenPersistedNarrativeTemplatesAreMissing()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.NarrativeTemplates.PersistedEpisodeSummary = "";

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("StoryTime:Generation:NarrativeTemplates", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenAnonymousIdentifierFallbackIsMissing()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Catalog.AnonymousIdentifierFallback = "";

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("StoryTime:Catalog:AnonymousIdentifierFallback", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_AllowsSeriesBiblePersistenceWithoutContinuityFacts()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.PersistSeriesStoryBible = true;
        options.Generation.PersistContinuityFacts = false;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_FailsWhenCorsOriginsUseWildcard()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Cors.AllowedOrigins = ["*"];

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("StoryTime:Cors:AllowedOrigins cannot include wildcard", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenProceduralAudioWeightsAreInvalid()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.ProceduralAudio.CarrierWeight = 0;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("StoryTime:Generation:ProceduralAudio harmonic weights", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenAudioAmplitudeScalesAreOutOfRange()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.TeaserAudioAmplitudeScale = 0;
        options.Generation.FullAudioAmplitudeScale = 1.2;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("audio amplitude scales", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenContinuityFactRetentionLimitIsNotPositive()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.ContinuityFactRetentionLimit = 0;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("ContinuityFactRetentionLimit", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenProceduralPosterStarsAreInvalid()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.ProceduralPoster.FallbackStarCount = 0;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("StoryTime:Generation:ProceduralPoster star counts", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenLibraryTitleTemplatesAreMissing()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Catalog.LibraryTitleWithArcTemplate = "";

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("StoryTime:Catalog library title templates", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenHashedIdentifierByteLengthIsOutOfRange()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Catalog.HashedIdentifierByteLength = 0;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("HashedIdentifierByteLength", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenUpgradeMessageTemplateOmitsUpgradeTierToken()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Messages.UpgradeForLongerStories = "Upgrade today for longer bedtime stories.";

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("StoryTime:Messages:UpgradeForLongerStories", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenSubscriptionDurationTemplateOmitsRequiredTokens()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Messages.SubscriptionDurationExceedsTier = "Upgrade now.";

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("StoryTime:Messages:SubscriptionDurationExceedsTier", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenAiStageNamesAreMissing()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AiOrchestration.StageNames.ScenePlan = "";

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("StoryTime:Generation:AiOrchestration:StageNames", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenAiEndpointIsNotOpenRouterAndEnforcementIsEnabled()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AiOrchestration.EnforceOpenRouterEndpoint = true;
        options.Generation.AiOrchestration.Endpoint = "https://provider.storytime.test/storytime/ai";

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("Endpoint must target openrouter.ai", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenAiEndpointIsNotOpenRouterEvenWhenAiIsDisabled()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AiOrchestration.Enabled = false;
        options.Generation.AiOrchestration.Endpoint = "https://provider.storytime.test/storytime/ai";

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("Endpoint must target openrouter.ai", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenAiLocalFallbackIsEnabled()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AiOrchestration.LocalFallbackEnabled = true;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("LocalFallbackEnabled must be false", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenOpenRouterEnforcementFlagIsDisabled()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AiOrchestration.EnforceOpenRouterEndpoint = false;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("EnforceOpenRouterEndpoint must be true", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenAiStageResponseFormatInstructionIsMissing()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AiOrchestration.StageResponseFormatInstruction = "";

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("StageResponseFormatInstruction", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_AcceptsOpenRouterAiEndpointWhenEnforced()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AiOrchestration.EnforceOpenRouterEndpoint = true;
        options.Generation.AiOrchestration.Endpoint = "https://openrouter.ai/api/v1/chat/completions";

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_FailsWhenCheckoutDefaultTierIsUnknown()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Checkout.DefaultTier = "Gold";

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("StoryTime:Checkout:DefaultTier", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenParentChallengeByteLengthIsNotPositive()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.ParentGate.ChallengeByteLength = 0;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("ChallengeByteLength", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenSemanticNarrativeThresholdExceedsNarrativeThreshold()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Catalog.NarrativeTextMinWords = 8;
        options.Catalog.SemanticNarrativeTextMinWords = 12;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("SemanticNarrativeTextMinWords", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenCheckoutUpgradeTierIsUnknown()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Checkout.UpgradeTier = "Diamond";

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("StoryTime:Checkout:UpgradeTier", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenPosterModelFailureRateIsNegative()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.PosterModelFailureRate = -0.1;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("PosterModelFailureRate", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenPosterModelFailureRateExceedsOne()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.PosterModelFailureRate = 1.1;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("PosterModelFailureRate", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Validate_AcceptsValidPosterModelFailureRate(double rate)
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.PosterModelFailureRate = rate;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(7999)]
    [InlineData(48001)]
    public void Validate_FailsWhenAudioSampleRateIsOutOfRange(int sampleRate)
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AudioSampleRate = sampleRate;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("AudioSampleRate", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(8000)]
    [InlineData(48000)]
    public void Validate_AcceptsValidAudioSampleRate(int sampleRate)
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AudioSampleRate = sampleRate;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(119)]
    [InlineData(1201)]
    public void Validate_FailsWhenAudioBaseFrequencyIsOutOfRange(double freq)
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AudioBaseFrequencyHz = freq;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("AudioBaseFrequencyHz", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(120)]
    [InlineData(1200)]
    public void Validate_AcceptsValidAudioBaseFrequency(double freq)
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.AudioBaseFrequencyHz = freq;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33)]
    public void Validate_FailsWhenHashedIdentifierByteLengthIsOutOfBounds(int length)
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Catalog.HashedIdentifierByteLength = length;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("HashedIdentifierByteLength", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(32)]
    public void Validate_AcceptsValidHashedIdentifierByteLength(int length)
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Catalog.HashedIdentifierByteLength = length;

        var result = new StoryTimeOptionsValidator().Validate(name: null, options);

        Assert.True(result.Succeeded);
    }

    private static void SyncLayerSpeedsWithMap(StoryTimeOptions options)
    {
        foreach (var layer in options.Generation.PosterLayers.Concat(options.Generation.Fallbacks.PosterLayers))
        {
            if (options.Generation.PosterRoleSpeedMultipliers.TryGetValue(layer.Role, out var speed))
            {
                layer.SpeedMultiplier = speed;
            }
        }
    }
}
