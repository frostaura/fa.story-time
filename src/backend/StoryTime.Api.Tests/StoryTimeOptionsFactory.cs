using StoryTime.Api;
using StoryTime.Api.Domain;

namespace StoryTime.Api.Tests;

internal static class StoryTimeOptionsFactory
{
    private const string ExternalProviderBaseUrlEnvironmentVariable = "STORYTIME_TEST_PROVIDER_BASE_URL";
    private static readonly Uri DefaultExternalProviderBaseUri = new("http://localhost:19081");

    private static string BuildProviderEndpoint(string relativePath)
    {
        var configuredBaseUrl = Environment.GetEnvironmentVariable(ExternalProviderBaseUrlEnvironmentVariable);
        var baseUri = Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var parsedBaseUri)
            ? parsedBaseUri
            : DefaultExternalProviderBaseUri;

        return new Uri(baseUri, relativePath).ToString();
    }

    public static StoryTimeOptions Create() => new()
    {
        DefaultApprovalRequired = true,
        Ui = new UiDefaults
        {
            QuickGenerateVisible = true,
            DurationSliderVisible = true,
            ParentControlsEnabled = true,
            DurationMinMinutes = 5,
            DurationMaxMinutes = 15,
            DurationDefaultMinutes = 6,
            DefaultChildName = "Dreamer"
        },
        ApiRoutes = new ApiRoutes
        {
            HomeStatus = "/api/home/status",
            SubscriptionWebhook = "/api/subscription/webhook",
            SubscriptionPaywall = "/api/subscription/{softUserId}/paywall",
            SubscriptionCheckoutSession = "/api/subscription/{softUserId}/checkout/session",
            SubscriptionCheckoutComplete = "/api/subscription/{softUserId}/checkout/complete",
            ParentGateRegister = "/api/parent/{softUserId}/gate/register",
            ParentGateChallenge = "/api/parent/{softUserId}/gate/challenge",
            ParentGateVerify = "/api/parent/{softUserId}/gate/verify",
            ParentSettings = "/api/parent/{softUserId}/settings",
            StoriesGenerate = "/api/stories/generate",
            StoryApprove = "/api/stories/{storyId}/approve",
            StoryFavorite = "/api/stories/{storyId}/favorite",
            Library = "/api/library/{softUserId}",
            LibraryStorageAudit = "/api/library/{softUserId}/storage-audit"
        },
        TierLimits = new Dictionary<string, TierLimits>(StringComparer.OrdinalIgnoreCase)
        {
            ["Trial"] = new TierLimits { Concurrency = 1, CooldownMinutes = 30, MaxDurationMinutes = 10 },
            ["Plus"] = new TierLimits { Concurrency = 1, CooldownMinutes = 30, MaxDurationMinutes = 10 },
            ["Premium"] = new TierLimits { Concurrency = 3, CooldownMinutes = 15, MaxDurationMinutes = 15 }
        },
        Generation = new GenerationOptions
        {
            ForceProceduralPosterFallback = false,
            PersistSeriesStoryBible = false,
            PersistContinuityFacts = true,
            ContinuityFactRetentionLimit = 30,
            StoryBibleFilePath = "data/story-bibles.tests.json",
            MinutesPerScene = 2,
            MinSceneCount = 3,
            MaxSceneCount = 8,
            PosterModelRetryCount = 2,
            PosterModelFailureRate = 0,
            OneShotModeAliases = ["one-shot", "oneshot"],
            TeaserDurationSeconds = 8,
            FullDurationSeconds = 24,
            TeaserAudioAmplitudeScale = 0.06,
            FullAudioAmplitudeScale = 0.07,
            AudioSampleRate = 16000,
            AudioBaseFrequencyHz = 261.63,
            DataUris = new DataUriOptions
            {
                AudioWavBase64Prefix = "data:audio/wav;base64,",
                AudioPayloadPrefix = "data:audio/",
                PosterSvgBase64Prefix = "data:image/svg+xml;base64,"
            },
            ProceduralAudio = new ProceduralAudioOptions
            {
                IdentifierOffsetRange = 64,
                SegmentDivisor = 5,
                PhraseEnvelopeExponent = 0.7,
                MelodicBendAmplitude = 0.06,
                BreathNoiseAmplitude = 0.05,
                CarrierWeight = 0.62,
                Harmonic2Weight = 0.25,
                Harmonic3Weight = 0.13
            },
            ProceduralPoster = new ProceduralPosterOptions
            {
                RichDetailOpacity = 0.35,
                FallbackOpacity = 0.24,
                RichDetailStarCount = 18,
                FallbackStarCount = 9
            },
            ProceduralPosterGeometry = new ProceduralPosterGeometryOptions
            {
                CanvasWidth = 1024,
                CanvasHeight = 1024,
                ViewBoxWidth = 1024,
                ViewBoxHeight = 1024,
                HorizonBaseY = 540,
                HorizonVariance = 140,
                DriftVariance = 160,
                DriftCenterOffset = 80,
                MoonCenterX = 512,
                MoonCenterY = 300,
                StarBaseX = 60,
                StarRangeX = 900,
                StarBaseY = 50,
                StarRangeY = 420,
                StarBaseRadius = 1,
                StarRadiusRange = 3,
                StarOpacity = 0.55
            },
            PosterModelProvider = new ExternalMediaProviderOptions
            {
                Enabled = true,
                LocalFallbackEnabled = true,
                Endpoint = BuildProviderEndpoint("/storytime/poster"),
                ApiKey = "",
                TimeoutSeconds = 1
            },
            NarrationProvider = new ExternalMediaProviderOptions
            {
                Enabled = true,
                LocalFallbackEnabled = true,
                Endpoint = BuildProviderEndpoint("/storytime/narration"),
                ApiKey = "",
                TimeoutSeconds = 1
            },
            ProceduralPosterFallbackBudgetMilliseconds = 200,
            PolishToneTag = "calm-bedtime",
            ModeLabels = new ModeLabelOptions
            {
                Series = "Series",
                OneShot = "One-shot"
            },
            TitleTemplates = ["{ChildName} and the {ArcName} ({ModeLabel} {EpisodeNumber})"],
            CalmOpeners = ["A soft lantern glows"],
            CalmTransitions = ["the next moment stays steady and kind"],
            CalmClosers = ["The night wraps everyone in peaceful rest."],
            ArcNames = ["Moonlit Meadow"],
            ThemeTrackIds = ["soft-piano"],
            NarrationStyles = ["warm-whisper"],
            PosterLayers =
            [
                new PosterLayerRule { Role = "BACKGROUND", SpeedMultiplier = 0.2 },
                new PosterLayerRule { Role = "MIDGROUND_1", SpeedMultiplier = 0.5 },
                new PosterLayerRule { Role = "FOREGROUND", SpeedMultiplier = 1.0 },
                new PosterLayerRule { Role = "PARTICLES", SpeedMultiplier = 1.3 }
            ],
            PosterRoleSpeedMultipliers = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["BACKGROUND"] = 0.2,
                ["MIDGROUND_1"] = 0.5,
                ["MIDGROUND_2"] = 0.8,
                ["FOREGROUND"] = 1.0,
                ["PARTICLES"] = 1.3
            },
            Fallbacks = new GenerationFallbackOptions
            {
                TitleTemplate = "{ChildName} in {ArcName} ({ModeLabel} {EpisodeNumber})",
                ArcName = "Moonlit Meadow",
                OneShotCompanionName = "a gentle friend",
                OneShotSetting = "moonlit meadow paths",
                OneShotMood = "softly adventurous",
                ThemeTrackId = "soft-piano",
                NarrationStyle = "warm-whisper",
                CalmOpener = "A calm hush settles in",
                CalmTransition = "the moment flows gently forward",
                CalmCloser = "Everyone rests with a steady breath.",
                PersistentRecurringCharacterAlias = "Dreamer",
                PosterLayers =
                [
                    new PosterLayerRule { Role = "BACKGROUND", SpeedMultiplier = 0.2 },
                    new PosterLayerRule { Role = "MIDGROUND_1", SpeedMultiplier = 0.5 },
                    new PosterLayerRule { Role = "FOREGROUND", SpeedMultiplier = 1.0 },
                    new PosterLayerRule { Role = "PARTICLES", SpeedMultiplier = 1.3 }
                ]
            },
            AiOrchestration = new AiOrchestrationOptions
            {
                Enabled = true,
                LocalFallbackEnabled = false,
                EnforceOpenRouterEndpoint = true,
                Endpoint = "https://openrouter.ai/api/v1/chat/completions",
                ApiKey = "",
                OpenRouterReferer = "https://storytime.local",
                OpenRouterTitle = "StoryTime",
                Model = "storytime-orchestrator",
                TimeoutSeconds = 15,
                StageResponseFormatInstruction = "Return only valid JSON with shape {\"text\": string|null, \"items\": string[]|null}. Do not include markdown fences.",
                StageNames = new AiStageNameOptions
                {
                    Outline = "outline",
                    ScenePlan = "scene_plan",
                    SceneBatch = "scene_batch",
                    Stitch = "stitch",
                    Polish = "polish"
                }
            },
            NarrativeTemplates = new NarrativeTemplateOptions
            {
                SeriesRecapFirstEpisode = "{Protagonist} begins a calm bedtime adventure in {ArcName}.",
                SeriesRecapContinuation = "Previously: {PreviousSummary}",
                ArcObjective = "Find tonight's calm ending in {ArcName}.",
                ContinuityFact = "Episode {EpisodeNumber} generated at {Timestamp} with {SceneCount} scenes.",
                EpisodeSummary = "A calm episode progressed through {SceneCount} bedtime scenes.",
                PersistedArcObjective = "Find tonight's calm ending in {ArcName}.",
                PersistedEpisodeSummary = "Episode {EpisodeNumber} completed.",
                OneShotOutline = "{Protagonist} and {CompanionName} enjoy a {Mood} one-shot bedtime adventure across {SceneCount} scenes in {Setting} with {ThemeTrackId} underscoring and {NarrationStyle} narration.",
                SeriesOutline = "{Protagonist} explores {ArcContext} through {SceneCount} bedtime scenes with calming progression.",
                SeriesOutlineStandaloneArcContext = "a standalone dream",
                SeriesOutlineArcContext = "the {ArcName} arc",
                ScenePlanStandaloneObjective = "gentle discovery {SceneNumber} in {ArcName}",
                ScenePlanSeriesObjective = "{ArcName} milestone {MilestoneNumber}",
                ScenePlanOpening = "opening calm from outline: {Outline}",
                Scene = "Scene {SceneNumber}: {Opener} as {Protagonist} follows {Objective}; {Transition}.{OneShotDetail}{ArcNote}",
                SceneArcNote = " The arc objective is {ArcObjective}.",
                StitchedArcLead = "Arc {EpisodeNumber}: ",
                OneShotDetailCompanion = "companion: {Value}",
                OneShotDetailSetting = "setting: {Value}",
                OneShotDetailMood = "mood: {Value}"
            }
        },
        ParentGate = new ParentGateOptions
        {
            ChallengeTtlMinutes = 10,
            ChallengeByteLength = 32,
            SessionTtlMinutes = 10,
            RequireAssertion = true,
            RequireChallengeBoundAssertion = true,
            RequireRegisteredCredential = true,
            RequireUserVerification = false,
            RelyingPartyId = "localhost",
            AssertionType = ParentGateAssertionTypes.WebAuthnGet,
            AllowedOrigins = ["http://localhost", "http://127.0.0.1"]
        },
        ParentDefaults = new ParentSettingsDefaults
        {
            NotificationsEnabled = false,
            AnalyticsEnabled = false
        },
        Catalog = new CatalogOptions
        {
            Provider = CatalogProviders.InMemory,
            FilePath = "data/story-catalog.tests.json",
            LibraryTitleWithArcTemplate = "{ModeLabel} - {ArcName} #{EpisodeNumber}",
            LibraryTitleWithoutArcTemplate = "{ModeLabel} story {GeneratedAtHHmm}",
            RecentItemsLimit = 20,
            KidShelfRecentLimit = 8,
            KidShelfFavoritesLimit = 8,
            HashedIdentifierByteLength = 6,
            AnonymousIdentifierFallback = "anonymous",
            NarrativeTextMinWords = 18,
            SemanticNarrativeTextMinWords = 8,
            NarrativeLeakageMarkers =
            [
                "scene ",
                "previously:",
                "episode ",
                " arc ",
                "companion:",
                "setting:",
                "mood:"
            ]
        },
        Cors = new CorsOptions
        {
            AllowedOrigins = ["http://localhost:5173", "http://127.0.0.1:5173"],
            AllowedMethods = ["GET", "POST", "PUT", "OPTIONS"],
            AllowedHeaders = ["Content-Type", "Authorization"]
        },
        Checkout = new CheckoutOptions
        {
            DefaultTier = "Trial",
            UpgradeTier = "Premium",
            UpgradeUrl = "/subscribe",
            SessionTtlMinutes = 15,
            Provider = new CheckoutProviderOptions
            {
                Mode = CheckoutProviderModes.External,
                LocalFallbackEnabled = true,
                Endpoint = BuildProviderEndpoint("/storytime/checkout"),
                ApiKey = "",
                TimeoutSeconds = 1
            }
        },
        Messages = new MessageTemplateOptions
        {
            UpgradeForLongerStories = "Upgrade to {UpgradeTier} for longer bedtime stories.",
            InvalidSubscriptionPayload = "Invalid subscription payload.",
            UnableToCreateCheckoutSession = "Unable to create checkout session.",
            InvalidOrExpiredCheckoutSession = "Invalid or expired checkout session.",
            SoftUserIdRequired = "softUserId is required.",
            InvalidParentCredential = "Invalid parent credential.",
            DurationMinutesMustBeGreaterThanZero = "durationMinutes must be greater than zero.",
            SubscriptionDurationExceedsTier = "Tier '{Tier}' supports up to {MaxDurationMinutes} minutes.",
            SubscriptionCooldownActive = "Cooldown active.",
            SubscriptionConcurrencyLimitReached = "Concurrency limit reached.",
            SubscriptionAllowed = "Allowed",
            UnsupportedCatalogProvider = "Unsupported StoryTime catalog provider '{Provider}'.",
            InternalErrors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["TierLimitsMustDefineTrial"] = "StoryTime:TierLimits must define at least the Trial tier.",
                ["CorsConfigurationRequired"] = "StoryTime:Cors configuration is required.",
                ["PosterLayerConfigMustDefine3To5Layers"] = "StoryTime:Generation poster layer config must define 3-5 layers.",
                ["ProceduralAudioWeightsMustSumPositive"] = "StoryTime:Generation:ProceduralAudio harmonic weights must add up to a positive value.",
                ["PosterModelProviderEndpointRequiredWhenEnabled"] = "StoryTime:Generation:PosterModelProvider:Endpoint must be configured when enabled.",
                ["PosterModelProviderFailedWithStatus"] = "StoryTime:Generation:PosterModelProvider failed with status {StatusCode}.",
                ["PosterModelProviderReturnedNoLayers"] = "StoryTime:Generation:PosterModelProvider returned no poster layers.",
                ["PosterModelProviderResponseMissingLayer"] = "StoryTime:Generation:PosterModelProvider response is missing '{Role}' layer.",
                ["NarrationProviderEndpointRequiredWhenEnabled"] = "StoryTime:Generation:NarrationProvider:Endpoint must be configured when enabled.",
                ["NarrationProviderFailedWithStatus"] = "StoryTime:Generation:NarrationProvider failed with status {StatusCode}.",
                ["NarrationProviderReturnedEmptyAudio"] = "StoryTime:Generation:NarrationProvider returned empty audio.",
                ["NarrationProviderReturnedNonAudioPayload"] = "StoryTime:Generation:NarrationProvider returned a non-audio payload.",
                ["ProceduralPosterFallbackBudgetMustBePositive"] = "StoryTime:Generation:ProceduralPosterFallbackBudgetMilliseconds must be greater than zero.",
                ["ProceduralPosterFallbackExceededBudget"] = "StoryTime procedural poster fallback exceeded budget: {ElapsedMs}ms > {BudgetMs}ms.",
                ["PosterRoleSpeedMultipliersMustDefineAtLeastOneRole"] = "StoryTime:Generation:PosterRoleSpeedMultipliers must define at least one role.",
                ["PosterLayerConfigIncludesUnsupportedRoles"] = "StoryTime:Generation poster layer config includes unsupported roles: {Roles}.",
                ["PosterLayerConfigMustNormalize3To5Layers"] = "StoryTime:Generation poster layer config must normalize to 3-5 layers.",
                ["PosterLayerConfigMustIncludeRequiredRoles"] = "StoryTime:Generation poster layer config must include {BackgroundRole}, {ForegroundRole}, and {ParticlesRole} roles.",
                ["PosterLayerConfigMidgroundRoleDependency"] = "StoryTime:Generation poster layer config cannot include {Midground2Role} without {Midground1Role}.",
                ["CheckoutProviderEndpointRequiredWhenModeExternal"] = "StoryTime:Checkout:Provider:Endpoint must be configured when Mode is External.",
                ["CheckoutProviderCreateSessionFailedWithStatus"] = "StoryTime:Checkout:Provider create session failed with status {StatusCode}.",
                ["CheckoutProviderCreateSessionReturnedInvalidPayload"] = "StoryTime:Checkout:Provider create session returned an invalid payload.",
                ["CheckoutProviderReturnedUnsupportedTier"] = "StoryTime:Checkout:Provider returned unsupported tier '{Tier}'.",
                ["CheckoutProviderCompletionFailedWithStatus"] = "StoryTime:Checkout:Provider completion failed with status {StatusCode}.",
                ["CheckoutProviderCompletionReturnedEmptyPayload"] = "StoryTime:Checkout:Provider completion returned an empty payload.",
                ["CheckoutDefaultTierMustBeConfigured"] = "StoryTime:Checkout:DefaultTier must be configured.",
                ["CheckoutDefaultTierMustMatchTierLimits"] = "StoryTime:Checkout:DefaultTier '{Tier}' must match one of the configured StoryTime:TierLimits keys.",
                ["AiOrchestrationEndpointRequiredWhenEnabled"] = "StoryTime:Generation:AiOrchestration:Endpoint must be configured when enabled.",
                ["AiOrchestrationEndpointMustTargetOpenRouter"] = "StoryTime:Generation:AiOrchestration:Endpoint must target openrouter.ai.",
                ["AiOrchestrationStageFailedWithStatus"] = "AI orchestration stage '{Stage}' failed with status {StatusCode}.",
                ["AiOrchestrationStageReturnedEmptyResponse"] = "AI orchestration stage '{Stage}' returned an empty response.",
                ["PersistentRecurringCharacterAliasMustBeConfigured"] = "Persistent recurring character alias must be configured."
            }
        }
    };
}
