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
        Assert.Equal("http://localhost:19081/storytime/ai", options.Generation.AiOrchestration.Endpoint);
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
            Assert.Equal("https://provider.storytime.test/storytime/ai", options.Generation.AiOrchestration.Endpoint);
            Assert.Equal("https://provider.storytime.test/storytime/checkout", options.Checkout.Provider.Endpoint);
        }
        finally
        {
            Environment.SetEnvironmentVariable("STORYTIME_TEST_PROVIDER_BASE_URL", null);
        }
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
