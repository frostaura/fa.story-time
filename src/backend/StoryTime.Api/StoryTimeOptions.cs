using StoryTime.Api.Domain;

namespace StoryTime.Api;

public sealed class StoryTimeOptions
{
    public const string SectionName = "StoryTime";

    public bool DefaultApprovalRequired { get; set; }

    public UiDefaults Ui { get; set; } = new();

    public ApiRoutes ApiRoutes { get; set; } = new();

    public Dictionary<string, TierLimits> TierLimits { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public GenerationOptions Generation { get; set; } = new();

    public ParentGateOptions ParentGate { get; set; } = new();

    public ParentSettingsDefaults ParentDefaults { get; set; } = new();

    public CatalogOptions Catalog { get; set; } = new();

    public CorsOptions Cors { get; set; } = new();

    public CheckoutOptions Checkout { get; set; } = new();

    public MessageTemplateOptions Messages { get; set; } = new();

    public TierLimits GetLimits(string tier)
    {
        if (TierLimits.TryGetValue(tier, out var limits))
        {
            return limits;
        }

        var defaultTier = Checkout.DefaultTier?.Trim();
        if (!string.IsNullOrWhiteSpace(defaultTier) && TierLimits.TryGetValue(defaultTier, out var configuredDefault))
        {
            return configuredDefault;
        }

        var firstConfiguredTier = TierLimits.Values.FirstOrDefault();
        if (firstConfiguredTier is not null)
        {
            return firstConfiguredTier;
        }

        throw new InvalidOperationException(Messages.Internal("TierLimitsMustDefineTrial"));
    }

    public IReadOnlyList<string> GetTierOrder()
    {
        var configured = Checkout.TierOrder
            .Where(tier => !string.IsNullOrWhiteSpace(tier))
            .Select(tier => tier.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (configured.Length > 0)
        {
            return configured;
        }

        var defaultTier = Checkout.DefaultTier?.Trim();
        if (string.IsNullOrWhiteSpace(defaultTier))
        {
            throw new InvalidOperationException(Messages.Internal("CheckoutDefaultTierMustBeConfigured"));
        }

        return [defaultTier];
    }

    public string GetNextTier(string currentTier)
    {
        var order = GetTierOrder();
        var currentIndex = order
            .Select((tier, index) => new { tier, index })
            .FirstOrDefault(entry => string.Equals(entry.tier, currentTier, StringComparison.OrdinalIgnoreCase))
            ?.index ?? 0;

        return currentIndex >= order.Count - 1
            ? order[^1]
            : order[currentIndex + 1];
    }

    public bool IsHigherTier(string currentTier, string targetTier)
    {
        var order = GetTierOrder();
        var currentIndex = order
            .Select((tier, index) => new { tier, index })
            .FirstOrDefault(entry => string.Equals(entry.tier, currentTier, StringComparison.OrdinalIgnoreCase))
            ?.index ?? -1;
        var targetIndex = order
            .Select((tier, index) => new { tier, index })
            .FirstOrDefault(entry => string.Equals(entry.tier, targetTier, StringComparison.OrdinalIgnoreCase))
            ?.index ?? -1;

        return currentIndex >= 0 &&
            targetIndex >= 0 &&
            targetIndex > currentIndex;
    }
}

public sealed class UiDefaults
{
    public bool QuickGenerateVisible { get; set; }

    public bool DurationSliderVisible { get; set; }

    public bool ParentControlsEnabled { get; set; }

    public int DurationMinMinutes { get; set; }

    public int DurationMaxMinutes { get; set; }

    public int DurationDefaultMinutes { get; set; }

    public string DefaultChildName { get; set; } = "";
}

public sealed class GenerationOptions
{
    public bool ForceProceduralPosterFallback { get; set; }

    public int ProceduralPosterFallbackBudgetMilliseconds { get; set; }

    public int ContinuityFactRetentionLimit { get; set; } = 30;

    public int MinutesPerScene { get; set; }

    public int MinSceneCount { get; set; }

    public int MaxSceneCount { get; set; }

    public int PosterModelRetryCount { get; set; }

    public double PosterModelFailureRate { get; set; }

    public List<string> PosterModelFailureSeedPrefixes { get; set; } = [];

    public List<string> OneShotModeAliases { get; set; } = [];

    public int TeaserDurationSeconds { get; set; }

    public int FullDurationSeconds { get; set; }

    public double TeaserAudioAmplitudeScale { get; set; } = 0.06;

    public double FullAudioAmplitudeScale { get; set; } = 0.07;

    public int AudioSampleRate { get; set; }

    public double AudioBaseFrequencyHz { get; set; }

    public ProceduralAudioOptions ProceduralAudio { get; set; } = new();

    public DataUriOptions DataUris { get; set; } = new();

    public ProceduralPosterOptions ProceduralPoster { get; set; } = new();

    public ProceduralPosterGeometryOptions ProceduralPosterGeometry { get; set; } = new();

    public ExternalMediaProviderOptions PosterModelProvider { get; set; } = new();

    public ExternalMediaProviderOptions NarrationProvider { get; set; } = new();

    public string PolishToneTag { get; set; } = "";

    public ModeLabelOptions ModeLabels { get; set; } = new();

    public List<string> TitleTemplates { get; set; } = [];

    public List<string> CalmOpeners { get; set; } = [];

    public List<string> CalmTransitions { get; set; } = [];

    public List<string> CalmClosers { get; set; } = [];

    public List<string> ArcNames { get; set; } = [];

    public List<string> ThemeTrackIds { get; set; } = [];

    public List<string> NarrationStyles { get; set; } = [];

    public List<PosterLayerRule> PosterLayers { get; set; } = [];

    public Dictionary<string, double> PosterRoleSpeedMultipliers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string PalettePrefix { get; set; } = "palette-";

    public GenerationFallbackOptions Fallbacks { get; set; } = new();

    public AiOrchestrationOptions AiOrchestration { get; set; } = new();

    public NarrativeTemplateOptions NarrativeTemplates { get; set; } = new();
}

public sealed class GenerationFallbackOptions
{
    public string TitleTemplate { get; set; } = "";

    public string ArcName { get; set; } = "";

    public string OneShotCompanionName { get; set; } = "";

    public string OneShotSetting { get; set; } = "";

    public string OneShotMood { get; set; } = "";

    public string ThemeTrackId { get; set; } = "";

    public string NarrationStyle { get; set; } = "";

    public string CalmOpener { get; set; } = "";

    public string CalmTransition { get; set; } = "";

    public string CalmCloser { get; set; } = "";

    public string PersistentRecurringCharacterAlias { get; set; } = "";

    public List<PosterLayerRule> PosterLayers { get; set; } = [];
}

public sealed class NarrativeTemplateOptions
{
    public string SeriesRecapFirstEpisode { get; set; } = "";

    public string SeriesRecapContinuation { get; set; } = "";

    public string ArcObjective { get; set; } = "";

    public string ContinuityFact { get; set; } = "";

    public string EpisodeSummary { get; set; } = "";

    public string PersistedArcObjective { get; set; } = "";

    public string PersistedEpisodeSummary { get; set; } = "";

    public string OneShotOutline { get; set; } = "";

    public string SeriesOutline { get; set; } = "";

    public string SeriesOutlineStandaloneArcContext { get; set; } = "";

    public string SeriesOutlineArcContext { get; set; } = "";

    public string ScenePlanStandaloneObjective { get; set; } = "";

    public string ScenePlanSeriesObjective { get; set; } = "";

    public string ScenePlanOpening { get; set; } = "";

    public string Scene { get; set; } = "";

    public string SceneArcNote { get; set; } = "";

    public string StitchedArcLead { get; set; } = "";

    public string OneShotDetailCompanion { get; set; } = "";

    public string OneShotDetailSetting { get; set; } = "";

    public string OneShotDetailMood { get; set; } = "";
}

public sealed class PosterLayerRule
{
    public string Role { get; set; } = PosterRoles.Background;

    public double SpeedMultiplier { get; set; }
}

public sealed class ProceduralAudioOptions
{
    public int IdentifierOffsetRange { get; set; } = 64;

    public int SegmentDivisor { get; set; } = 5;

    public double PhraseEnvelopeExponent { get; set; } = 0.7;

    public double MelodicBendAmplitude { get; set; } = 0.06;

    public double BreathNoiseAmplitude { get; set; } = 0.05;

    public double CarrierWeight { get; set; } = 0.62;

    public double Harmonic2Weight { get; set; } = 0.25;

    public double Harmonic3Weight { get; set; } = 0.13;
}

public sealed class DataUriOptions
{
    public string AudioWavBase64Prefix { get; set; } = "";

    public string AudioPayloadPrefix { get; set; } = "";

    public string PosterSvgBase64Prefix { get; set; } = "";
}

public sealed class ProceduralPosterOptions
{
    public double RichDetailOpacity { get; set; } = 0.35;

    public double FallbackOpacity { get; set; } = 0.24;

    public int RichDetailStarCount { get; set; } = 18;

    public int FallbackStarCount { get; set; } = 9;
}

public sealed class ProceduralPosterGeometryOptions
{
    public int CanvasWidth { get; set; } = 1024;

    public int CanvasHeight { get; set; } = 1024;

    public int ViewBoxWidth { get; set; } = 1024;

    public int ViewBoxHeight { get; set; } = 1024;

    public int HorizonBaseY { get; set; } = 540;

    public int HorizonVariance { get; set; } = 140;

    public int DriftVariance { get; set; } = 160;

    /// <summary>
    /// Centers the signed drift range around 0 by subtracting half of <see cref="DriftVariance"/>.
    /// With the default variance (160), 80 yields a balanced [-80, 79] drift span.
    /// </summary>
    public int DriftCenterOffset { get; set; } = 80;

    public int MoonCenterX { get; set; } = 512;

    public int MoonCenterY { get; set; } = 300;

    public int StarBaseX { get; set; } = 60;

    public int StarRangeX { get; set; } = 900;

    public int StarBaseY { get; set; } = 50;

    public int StarRangeY { get; set; } = 420;

    public int StarBaseRadius { get; set; } = 1;

    public int StarRadiusRange { get; set; } = 3;

    public double StarOpacity { get; set; } = 0.55;
}

public sealed class ModeLabelOptions
{
    public string Series { get; set; } = "";

    public string OneShot { get; set; } = "";
}

public sealed class ParentGateOptions
{
    public int ChallengeTtlMinutes { get; set; }

    public int ChallengeByteLength { get; set; } = 32;

    public int SessionTtlMinutes { get; set; }

    public bool RequireAssertion { get; set; }

    public bool RequireChallengeBoundAssertion { get; set; }

    public bool RequireRegisteredCredential { get; set; }

    public bool RequireUserVerification { get; set; }

    public string StateFilePath { get; set; } = "";

    public string RelyingPartyId { get; set; } = "";

    public string AssertionType { get; set; } = "";

    public List<string> AllowedOrigins { get; set; } = [];
}

public sealed class ParentSettingsDefaults
{
    public bool NotificationsEnabled { get; set; }

    public bool AnalyticsEnabled { get; set; }

    public bool KidShelfEnabled { get; set; }
}

public sealed class CatalogOptions
{
    public string Provider { get; set; } = "";

    public string FilePath { get; set; } = "";

    public string LibraryTitleWithArcTemplate { get; set; } = "";

    public string LibraryTitleWithoutArcTemplate { get; set; } = "";

    public int RecentItemsLimit { get; set; }

    public int KidShelfRecentLimit { get; set; }

    public int KidShelfFavoritesLimit { get; set; }

    public int HashedIdentifierByteLength { get; set; } = 6;

    public string AnonymousIdentifierFallback { get; set; } = "";

    public List<string> NarrativeLeakageMarkers { get; set; } = [];

    public int NarrativeTextMinWords { get; set; }

    public int SemanticNarrativeTextMinWords { get; set; }
}

public sealed class CorsOptions
{
    public List<string> AllowedOrigins { get; set; } = [];

    public List<string> AllowedMethods { get; set; } = [];

    public List<string> AllowedHeaders { get; set; } = [];
}

public sealed class CheckoutOptions
{
    public string DefaultTier { get; set; } = "";

    public string UpgradeUrl { get; set; } = "";

    public string DefaultReturnUrl { get; set; } = "";

    public List<string> TierOrder { get; set; } = [];

    public int SessionTtlMinutes { get; set; }

    public string StateFilePath { get; set; } = "";

    public string WebhookSharedSecret { get; set; } = "";

    public CheckoutProviderOptions Provider { get; set; } = new();
}

public sealed class MessageTemplateOptions
{
    public string UpgradeForLongerStories { get; set; } = "";

    public string InvalidSubscriptionPayload { get; set; } = "";

    public string UnableToCreateCheckoutSession { get; set; } = "";

    public string InvalidOrExpiredCheckoutSession { get; set; } = "";

    public string SoftUserIdRequired { get; set; } = "";

    public string InvalidParentCredential { get; set; } = "";

    public string DurationMinutesMustBeGreaterThanZero { get; set; } = "";

    public string SubscriptionDurationExceedsTier { get; set; } = "";

    public string SubscriptionCooldownActive { get; set; } = "";

    public string SubscriptionConcurrencyLimitReached { get; set; } = "";

    public string SubscriptionAllowed { get; set; } = "";

    public string UnsupportedCatalogProvider { get; set; } = "";

    public Dictionary<string, string> ValidationErrors { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> InternalErrors { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string Validation(string key, params (string Token, string Value)[] replacements) =>
        ApplyTemplate(ResolveTemplate(ValidationErrors, "ValidationErrors", key), replacements);

    public string Internal(string key, params (string Token, string Value)[] replacements) =>
        ApplyTemplate(ResolveTemplate(InternalErrors, "InternalErrors", key), replacements);

    private static string ResolveTemplate(
        IReadOnlyDictionary<string, string> templates,
        string section,
        string key)
    {
        if (templates.TryGetValue(key, out var template) && !string.IsNullOrWhiteSpace(template))
        {
            return template;
        }

        throw new InvalidOperationException($"StoryTime:Messages:{section}:{key} is required.");
    }

    private static string ApplyTemplate(string template, IReadOnlyList<(string Token, string Value)> replacements)
    {
        if (replacements.Count == 0)
        {
            return template;
        }

        var rendered = template;
        foreach (var (token, value) in replacements)
        {
            rendered = rendered.Replace($"{{{token}}}", value, StringComparison.Ordinal);
        }

        return rendered;
    }
}

public sealed class AiOrchestrationOptions
{
    public bool Enabled { get; set; }

    public bool LocalFallbackEnabled { get; set; }

    public bool EnforceOpenRouterEndpoint { get; set; } = true;

    public string Endpoint { get; set; } = "";

    public string ApiKey { get; set; } = "";

    public string OpenRouterReferer { get; set; } = "";

    public string OpenRouterTitle { get; set; } = "";

    public string Model { get; set; } = "";

    public int TimeoutSeconds { get; set; }

    public string StageResponseFormatInstruction { get; set; } = "";

    public AiStageNameOptions StageNames { get; set; } = new();
}

public sealed class ExternalMediaProviderOptions
{
    public bool Enabled { get; set; }

    public bool LocalFallbackEnabled { get; set; }

    public string Endpoint { get; set; } = "";

    public string ApiKey { get; set; } = "";

    public int TimeoutSeconds { get; set; }
}

public sealed class AiStageNameOptions
{
    public string Outline { get; set; } = "";

    public string ScenePlan { get; set; } = "";

    public string SceneBatch { get; set; } = "";

    public string Stitch { get; set; } = "";

    public string Polish { get; set; } = "";
}

public sealed class CheckoutProviderOptions
{
    public string Mode { get; set; } = "";

    public bool LocalFallbackEnabled { get; set; }

    public string Endpoint { get; set; } = "";

    public string ApiKey { get; set; } = "";

    public int TimeoutSeconds { get; set; }
}

public sealed class TierLimits
{
    public int Concurrency { get; set; }

    public int CooldownMinutes { get; set; }

    public int MaxDurationMinutes { get; set; }
}
