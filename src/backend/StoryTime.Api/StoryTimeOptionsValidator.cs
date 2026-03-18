using Microsoft.Extensions.Options;
using StoryTime.Api.Domain;

namespace StoryTime.Api;

public sealed class StoryTimeOptionsValidator : IValidateOptions<StoryTimeOptions>
{
    public ValidateOptionsResult Validate(string? name, StoryTimeOptions options)
    {
        var errors = new List<string>();
        var posterRoleSpeedByRole = NormalizePosterRoleSpeedByRole(options.Generation.PosterRoleSpeedMultipliers, errors);

        if (options.Ui.DurationMinMinutes <= 0 || options.Ui.DurationMaxMinutes < options.Ui.DurationMinMinutes)
        {
            errors.Add("StoryTime:Ui duration bounds are invalid.");
        }

        if (options.Ui.DurationDefaultMinutes < options.Ui.DurationMinMinutes ||
            options.Ui.DurationDefaultMinutes > options.Ui.DurationMaxMinutes)
        {
            errors.Add("StoryTime:Ui:DurationDefaultMinutes must be between min and max.");
        }

        if (string.IsNullOrWhiteSpace(options.Ui.DefaultChildName))
        {
            errors.Add("StoryTime:Ui:DefaultChildName is required.");
        }

        ValidateApiRoutes(options.ApiRoutes, errors);

        if (options.Generation.MinSceneCount <= 0 || options.Generation.MaxSceneCount < options.Generation.MinSceneCount)
        {
            errors.Add("StoryTime:Generation scene counts are invalid.");
        }

        if (options.Generation.MinutesPerScene <= 0)
        {
            errors.Add("StoryTime:Generation:MinutesPerScene must be greater than zero.");
        }

        if (options.Generation.ContinuityFactRetentionLimit <= 0)
        {
            errors.Add("StoryTime:Generation:ContinuityFactRetentionLimit must be greater than zero.");
        }

        if (options.Generation.TeaserDurationSeconds <= 0 || options.Generation.FullDurationSeconds <= 0)
        {
            errors.Add("StoryTime:Generation audio durations must be greater than zero.");
        }

        if (options.Generation.PosterModelFailureRate is < 0 or > 1)
        {
            errors.Add("StoryTime:Generation:PosterModelFailureRate must be between 0 and 1.");
        }

        if (options.Generation.TeaserAudioAmplitudeScale is <= 0 or > 1 ||
            options.Generation.FullAudioAmplitudeScale is <= 0 or > 1)
        {
            errors.Add("StoryTime:Generation audio amplitude scales must be greater than zero and less than or equal to 1.");
        }

        if (options.Generation.AudioSampleRate is < 8000 or > 48000)
        {
            errors.Add("StoryTime:Generation:AudioSampleRate must be between 8000 and 48000.");
        }

        if (options.Generation.AudioBaseFrequencyHz is < 120 or > 1200)
        {
            errors.Add("StoryTime:Generation:AudioBaseFrequencyHz must be between 120 and 1200.");
        }

        if (string.IsNullOrWhiteSpace(options.Generation.DataUris.AudioWavBase64Prefix) ||
            string.IsNullOrWhiteSpace(options.Generation.DataUris.AudioPayloadPrefix) ||
            string.IsNullOrWhiteSpace(options.Generation.DataUris.PosterSvgBase64Prefix))
        {
            errors.Add("StoryTime:Generation:DataUris values are required.");
        }
        else if (!options.Generation.DataUris.AudioWavBase64Prefix.StartsWith(
                     options.Generation.DataUris.AudioPayloadPrefix,
                     StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("StoryTime:Generation:DataUris:AudioWavBase64Prefix must start with AudioPayloadPrefix.");
        }

        if (options.Generation.ProceduralAudio.IdentifierOffsetRange <= 0)
        {
            errors.Add("StoryTime:Generation:ProceduralAudio:IdentifierOffsetRange must be greater than zero.");
        }

        if (options.Generation.ProceduralAudio.SegmentDivisor <= 0)
        {
            errors.Add("StoryTime:Generation:ProceduralAudio:SegmentDivisor must be greater than zero.");
        }

        if (options.Generation.ProceduralAudio.PhraseEnvelopeExponent <= 0)
        {
            errors.Add("StoryTime:Generation:ProceduralAudio:PhraseEnvelopeExponent must be greater than zero.");
        }

        if (options.Generation.ProceduralAudio.MelodicBendAmplitude < 0 ||
            options.Generation.ProceduralAudio.BreathNoiseAmplitude < 0)
        {
            errors.Add("StoryTime:Generation:ProceduralAudio amplitudes must be zero or greater.");
        }

        if (options.Generation.ProceduralAudio.CarrierWeight <= 0 ||
            options.Generation.ProceduralAudio.Harmonic2Weight < 0 ||
            options.Generation.ProceduralAudio.Harmonic3Weight < 0)
        {
            errors.Add("StoryTime:Generation:ProceduralAudio harmonic weights must be non-negative and include a positive carrier weight.");
        }

        if (options.Generation.ProceduralPoster.RichDetailOpacity is < 0 or > 1 ||
            options.Generation.ProceduralPoster.FallbackOpacity is < 0 or > 1)
        {
            errors.Add("StoryTime:Generation:ProceduralPoster opacity values must be between 0 and 1.");
        }

        if (options.Generation.ProceduralPoster.RichDetailStarCount <= 0 ||
            options.Generation.ProceduralPoster.FallbackStarCount <= 0)
        {
            errors.Add("StoryTime:Generation:ProceduralPoster star counts must be greater than zero.");
        }

        if (options.Generation.ProceduralPosterGeometry.CanvasWidth <= 0 ||
            options.Generation.ProceduralPosterGeometry.CanvasHeight <= 0 ||
            options.Generation.ProceduralPosterGeometry.ViewBoxWidth <= 0 ||
            options.Generation.ProceduralPosterGeometry.ViewBoxHeight <= 0)
        {
            errors.Add("StoryTime:Generation:ProceduralPosterGeometry canvas and viewBox values must be greater than zero.");
        }

        if (options.Generation.ProceduralPosterGeometry.HorizonVariance <= 0 ||
            options.Generation.ProceduralPosterGeometry.DriftVariance <= 0)
        {
            errors.Add("StoryTime:Generation:ProceduralPosterGeometry horizon and drift variance values must be greater than zero.");
        }

        if (options.Generation.ProceduralPosterGeometry.StarRangeX <= 0 ||
            options.Generation.ProceduralPosterGeometry.StarRangeY <= 0 ||
            options.Generation.ProceduralPosterGeometry.StarBaseRadius <= 0 ||
            options.Generation.ProceduralPosterGeometry.StarRadiusRange <= 0)
        {
            errors.Add("StoryTime:Generation:ProceduralPosterGeometry star range and radius values must be greater than zero.");
        }

        if (options.Generation.ProceduralPosterGeometry.StarOpacity is < 0 or > 1)
        {
            errors.Add("StoryTime:Generation:ProceduralPosterGeometry:StarOpacity must be between 0 and 1.");
        }

        if (options.Generation.ProceduralPosterFallbackBudgetMilliseconds <= 0)
        {
            errors.Add("StoryTime:Generation:ProceduralPosterFallbackBudgetMilliseconds must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.Generation.PolishToneTag))
        {
            errors.Add("StoryTime:Generation:PolishToneTag is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Generation.ModeLabels.Series) ||
            string.IsNullOrWhiteSpace(options.Generation.ModeLabels.OneShot))
        {
            errors.Add("StoryTime:Generation:ModeLabels:Series and StoryTime:Generation:ModeLabels:OneShot are required.");
        }

        if (string.IsNullOrWhiteSpace(options.Generation.Fallbacks.TitleTemplate) ||
            string.IsNullOrWhiteSpace(options.Generation.Fallbacks.ArcName) ||
            string.IsNullOrWhiteSpace(options.Generation.Fallbacks.OneShotCompanionName) ||
            string.IsNullOrWhiteSpace(options.Generation.Fallbacks.OneShotSetting) ||
            string.IsNullOrWhiteSpace(options.Generation.Fallbacks.OneShotMood) ||
            string.IsNullOrWhiteSpace(options.Generation.Fallbacks.ThemeTrackId) ||
            string.IsNullOrWhiteSpace(options.Generation.Fallbacks.NarrationStyle) ||
            string.IsNullOrWhiteSpace(options.Generation.Fallbacks.CalmOpener) ||
            string.IsNullOrWhiteSpace(options.Generation.Fallbacks.CalmTransition) ||
            string.IsNullOrWhiteSpace(options.Generation.Fallbacks.CalmCloser) ||
            string.IsNullOrWhiteSpace(options.Generation.Fallbacks.PersistentRecurringCharacterAlias))
        {
            errors.Add("StoryTime:Generation:Fallbacks textual values are required.");
        }

        if (string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.SeriesRecapFirstEpisode) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.SeriesRecapContinuation) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.ArcObjective) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.ContinuityFact) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.EpisodeSummary) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.PersistedArcObjective) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.PersistedEpisodeSummary) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.OneShotOutline) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.SeriesOutline) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.SeriesOutlineStandaloneArcContext) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.SeriesOutlineArcContext) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.ScenePlanStandaloneObjective) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.ScenePlanSeriesObjective) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.ScenePlanOpening) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.Scene) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.SceneArcNote) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.StitchedArcLead) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.OneShotDetailCompanion) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.OneShotDetailSetting) ||
            string.IsNullOrWhiteSpace(options.Generation.NarrativeTemplates.OneShotDetailMood))
        {
            errors.Add("StoryTime:Generation:NarrativeTemplates values are required.");
        }

        ValidatePosterLayerContract(
            options.Generation.PosterLayers,
            "StoryTime:Generation:PosterLayers",
            posterRoleSpeedByRole,
            errors);
        ValidatePosterLayerContract(
            options.Generation.Fallbacks.PosterLayers,
            "StoryTime:Generation:Fallbacks:PosterLayers",
            posterRoleSpeedByRole,
            errors);

        if (options.ParentGate.ChallengeTtlMinutes <= 0 || options.ParentGate.SessionTtlMinutes <= 0)
        {
            errors.Add("StoryTime:ParentGate ttl values must be greater than zero.");
        }

        if (options.ParentGate.ChallengeByteLength <= 0)
        {
            errors.Add("StoryTime:ParentGate:ChallengeByteLength must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.ParentGate.RelyingPartyId))
        {
            errors.Add("StoryTime:ParentGate:RelyingPartyId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ParentGate.AssertionType))
        {
            errors.Add("StoryTime:ParentGate:AssertionType is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ParentGate.StateFilePath))
        {
            errors.Add("StoryTime:ParentGate:StateFilePath is required.");
        }

        ValidateCorsOptions(options.Cors, errors);

        if (options.Catalog.RecentItemsLimit <= 0 ||
            options.Catalog.KidShelfRecentLimit <= 0 ||
            options.Catalog.KidShelfFavoritesLimit <= 0)
        {
            errors.Add("StoryTime:Catalog list limits must be greater than zero.");
        }

        if (options.Catalog.HashedIdentifierByteLength is <= 0 or > 32)
        {
            errors.Add("StoryTime:Catalog:HashedIdentifierByteLength must be between 1 and 32.");
        }

        if (string.IsNullOrWhiteSpace(options.Catalog.Provider))
        {
            errors.Add("StoryTime:Catalog:Provider is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Catalog.AnonymousIdentifierFallback))
        {
            errors.Add("StoryTime:Catalog:AnonymousIdentifierFallback is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Catalog.LibraryTitleWithArcTemplate) ||
            string.IsNullOrWhiteSpace(options.Catalog.LibraryTitleWithoutArcTemplate))
        {
            errors.Add("StoryTime:Catalog library title templates are required.");
        }
        else
        {
            var provider = options.Catalog.Provider.Trim();
            if (!string.Equals(provider, CatalogProviders.InMemory, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(provider, CatalogProviders.FileSystem, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"StoryTime:Catalog:Provider must be one of {CatalogProviders.InMemory} or {CatalogProviders.FileSystem}.");
            }

            if (string.Equals(provider, CatalogProviders.FileSystem, StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(options.Catalog.FilePath))
            {
                errors.Add($"StoryTime:Catalog:FilePath is required when Provider is {CatalogProviders.FileSystem}.");
            }
        }

        if (options.Catalog.NarrativeLeakageMarkers.Count == 0 ||
            options.Catalog.NarrativeLeakageMarkers.All(string.IsNullOrWhiteSpace))
        {
            errors.Add("StoryTime:Catalog:NarrativeLeakageMarkers must define at least one marker.");
        }

        if (options.Catalog.NarrativeTextMinWords <= 0 ||
            options.Catalog.SemanticNarrativeTextMinWords <= 0)
        {
            errors.Add("StoryTime:Catalog narrative leakage word thresholds must be greater than zero.");
        }
        else if (options.Catalog.SemanticNarrativeTextMinWords > options.Catalog.NarrativeTextMinWords)
        {
            errors.Add("StoryTime:Catalog:SemanticNarrativeTextMinWords must be less than or equal to NarrativeTextMinWords.");
        }

        if (string.IsNullOrWhiteSpace(options.Checkout.DefaultTier) ||
            string.IsNullOrWhiteSpace(options.Checkout.UpgradeUrl))
        {
            errors.Add("StoryTime:Checkout values are required.");
        }
        else if (!options.TierLimits.ContainsKey(options.Checkout.DefaultTier))
        {
            errors.Add("StoryTime:Checkout:DefaultTier must match one of the configured StoryTime:TierLimits keys.");
        }

        if (options.TierLimits.Count == 0)
        {
            errors.Add("StoryTime:TierLimits must define at least one tier.");
        }

        foreach (var (tier, limits) in options.TierLimits)
        {
            if (string.IsNullOrWhiteSpace(tier))
            {
                errors.Add("StoryTime:TierLimits cannot contain a blank tier key.");
                continue;
            }

            if (limits.Concurrency <= 0)
            {
                errors.Add($"StoryTime:TierLimits:{tier}:Concurrency must be greater than zero.");
            }

            if (limits.CooldownMinutes < 0)
            {
                errors.Add($"StoryTime:TierLimits:{tier}:CooldownMinutes must be zero or greater.");
            }

            if (limits.MaxDurationMinutes <= 0)
            {
                errors.Add($"StoryTime:TierLimits:{tier}:MaxDurationMinutes must be greater than zero.");
            }
        }

        var tierOrder = options.Checkout.TierOrder
            .Where(tier => !string.IsNullOrWhiteSpace(tier))
            .Select(tier => tier.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (tierOrder.Length < 2)
        {
            errors.Add("StoryTime:Checkout:TierOrder must define at least two tiers.");
        }
        else
        {
            if (!string.Equals(tierOrder[0], options.Checkout.DefaultTier, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("StoryTime:Checkout:TierOrder must start with StoryTime:Checkout:DefaultTier.");
            }

            foreach (var tier in tierOrder)
            {
                if (!options.TierLimits.ContainsKey(tier))
                {
                    errors.Add($"StoryTime:Checkout:TierOrder contains unsupported tier '{tier}'.");
                }
            }
        }

        if (options.Checkout.SessionTtlMinutes <= 0)
        {
            errors.Add("StoryTime:Checkout:SessionTtlMinutes must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.Checkout.StateFilePath))
        {
            errors.Add("StoryTime:Checkout:StateFilePath is required.");
        }

        var checkoutProviderMode = options.Checkout.Provider.Mode.Trim();
        if (!string.Equals(checkoutProviderMode, CheckoutProviderModes.InMemory, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(checkoutProviderMode, CheckoutProviderModes.External, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"StoryTime:Checkout:Provider:Mode must be one of {CheckoutProviderModes.InMemory} or {CheckoutProviderModes.External}.");
        }
        else if (string.Equals(checkoutProviderMode, CheckoutProviderModes.External, StringComparison.OrdinalIgnoreCase))
        {
            var hasEndpoint = !string.IsNullOrWhiteSpace(options.Checkout.Provider.Endpoint);
            if (!hasEndpoint && !options.Checkout.Provider.LocalFallbackEnabled)
            {
                errors.Add($"StoryTime:Checkout:Provider must configure Endpoint or enable LocalFallbackEnabled when Mode is {CheckoutProviderModes.External}.");
            }

            if (options.Checkout.Provider.TimeoutSeconds <= 0)
            {
                errors.Add($"StoryTime:Checkout:Provider:TimeoutSeconds must be greater than zero when Mode is {CheckoutProviderModes.External}.");
            }
        }

        if (string.IsNullOrWhiteSpace(options.Messages.UpgradeForLongerStories))
        {
            errors.Add("StoryTime:Messages:UpgradeForLongerStories is required.");
        }
        else if (!options.Messages.UpgradeForLongerStories.Contains("{UpgradeTier}", StringComparison.Ordinal))
        {
            errors.Add("StoryTime:Messages:UpgradeForLongerStories must include the {UpgradeTier} token.");
        }

        if (string.IsNullOrWhiteSpace(options.Messages.InvalidSubscriptionPayload) ||
            string.IsNullOrWhiteSpace(options.Messages.UnableToCreateCheckoutSession) ||
            string.IsNullOrWhiteSpace(options.Messages.InvalidOrExpiredCheckoutSession) ||
            string.IsNullOrWhiteSpace(options.Messages.SoftUserIdRequired) ||
            string.IsNullOrWhiteSpace(options.Messages.InvalidParentCredential) ||
            string.IsNullOrWhiteSpace(options.Messages.DurationMinutesMustBeGreaterThanZero) ||
            string.IsNullOrWhiteSpace(options.Messages.SubscriptionDurationExceedsTier) ||
            string.IsNullOrWhiteSpace(options.Messages.SubscriptionCooldownActive) ||
            string.IsNullOrWhiteSpace(options.Messages.SubscriptionConcurrencyLimitReached) ||
            string.IsNullOrWhiteSpace(options.Messages.SubscriptionAllowed) ||
            string.IsNullOrWhiteSpace(options.Messages.UnsupportedCatalogProvider))
        {
            errors.Add("StoryTime:Messages API error templates are required.");
        }
        else if (!options.Messages.SubscriptionDurationExceedsTier.Contains("{Tier}", StringComparison.Ordinal) ||
                 !options.Messages.SubscriptionDurationExceedsTier.Contains("{MaxDurationMinutes}", StringComparison.Ordinal))
        {
            errors.Add("StoryTime:Messages:SubscriptionDurationExceedsTier must include {Tier} and {MaxDurationMinutes} tokens.");
        }
        else if (!options.Messages.UnsupportedCatalogProvider.Contains("{Provider}", StringComparison.Ordinal))
        {
            errors.Add("StoryTime:Messages:UnsupportedCatalogProvider must include the {Provider} token.");
        }

        var aiOptions = options.Generation.AiOrchestration;
        var aiEndpoint = aiOptions.Endpoint;
        if (!string.IsNullOrWhiteSpace(aiEndpoint) && !IsOpenRouterEndpoint(aiEndpoint))
        {
            errors.Add("StoryTime:Generation:AiOrchestration:Endpoint must target openrouter.ai.");
        }

        if (aiOptions.LocalFallbackEnabled)
        {
            errors.Add("StoryTime:Generation:AiOrchestration:LocalFallbackEnabled must be false.");
        }

        if (!aiOptions.EnforceOpenRouterEndpoint)
        {
            errors.Add("StoryTime:Generation:AiOrchestration:EnforceOpenRouterEndpoint must be true.");
        }

        if (aiOptions.Enabled)
        {
            var hasEndpoint = !string.IsNullOrWhiteSpace(aiOptions.Endpoint);
            if (!hasEndpoint)
            {
                errors.Add("StoryTime:Generation:AiOrchestration:Endpoint is required when enabled.");
            }

            if (string.IsNullOrWhiteSpace(aiOptions.Model))
            {
                errors.Add("StoryTime:Generation:AiOrchestration:Model is required when enabled.");
            }

            if (aiOptions.TimeoutSeconds <= 0)
            {
                errors.Add("StoryTime:Generation:AiOrchestration:TimeoutSeconds must be greater than zero when enabled.");
            }

            if (string.IsNullOrWhiteSpace(aiOptions.StageResponseFormatInstruction))
            {
                errors.Add(
                    "StoryTime:Generation:AiOrchestration:StageResponseFormatInstruction is required when enabled.");
            }

            if (hasEndpoint)
            {
                if (string.IsNullOrWhiteSpace(aiOptions.OpenRouterReferer))
                {
                    errors.Add("StoryTime:Generation:AiOrchestration:OpenRouterReferer is required when using openrouter.ai endpoint.");
                }

                if (string.IsNullOrWhiteSpace(aiOptions.OpenRouterTitle))
                {
                    errors.Add("StoryTime:Generation:AiOrchestration:OpenRouterTitle is required when using openrouter.ai endpoint.");
                }
            }

            if (string.IsNullOrWhiteSpace(aiOptions.StageNames.Outline) ||
                string.IsNullOrWhiteSpace(aiOptions.StageNames.ScenePlan) ||
                string.IsNullOrWhiteSpace(aiOptions.StageNames.SceneBatch) ||
                string.IsNullOrWhiteSpace(aiOptions.StageNames.Stitch) ||
                string.IsNullOrWhiteSpace(aiOptions.StageNames.Polish))
            {
                errors.Add("StoryTime:Generation:AiOrchestration:StageNames values are required when enabled.");
            }
        }

        ValidateExternalMediaProvider(
            options.Generation.PosterModelProvider,
            "StoryTime:Generation:PosterModelProvider",
            errors);
        ValidateExternalMediaProvider(
            options.Generation.NarrationProvider,
            "StoryTime:Generation:NarrationProvider",
            errors);

        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }

    private static void ValidateApiRoutes(ApiRoutes options, List<string> errors)
    {
        var entries = new[]
        {
            ("HomeStatus", options.HomeStatus),
            ("SubscriptionWebhook", options.SubscriptionWebhook),
            ("SubscriptionPaywall", options.SubscriptionPaywall),
            ("SubscriptionCheckoutSession", options.SubscriptionCheckoutSession),
            ("SubscriptionCheckoutComplete", options.SubscriptionCheckoutComplete),
            ("ParentGateRegister", options.ParentGateRegister),
            ("ParentGateChallenge", options.ParentGateChallenge),
            ("ParentGateVerify", options.ParentGateVerify),
            ("ParentSettings", options.ParentSettings),
            ("StoriesGenerate", options.StoriesGenerate),
            ("StoryApprove", options.StoryApprove),
            ("StoryFavorite", options.StoryFavorite),
            ("Library", options.Library),
            ("LibraryStorageAudit", options.LibraryStorageAudit)
        };

        foreach (var (name, route) in entries)
        {
            if (string.IsNullOrWhiteSpace(route))
            {
                errors.Add($"StoryTime:ApiRoutes:{name} is required.");
                continue;
            }

            if (!route.StartsWith("/", StringComparison.Ordinal))
            {
                errors.Add($"StoryTime:ApiRoutes:{name} must start with '/'.");
            }
        }
    }

    private static void ValidateExternalMediaProvider(
        ExternalMediaProviderOptions options,
        string path,
        List<string> errors)
    {
        if (!options.Enabled)
        {
            return;
        }

        var hasEndpoint = !string.IsNullOrWhiteSpace(options.Endpoint);
        if (!hasEndpoint && !options.LocalFallbackEnabled)
        {
            errors.Add($"{path} must configure Endpoint or enable LocalFallbackEnabled when enabled.");
        }

        if (options.TimeoutSeconds <= 0)
        {
            errors.Add($"{path}:TimeoutSeconds must be greater than zero when enabled.");
        }
    }

    private static Dictionary<string, double> NormalizePosterRoleSpeedByRole(
        IReadOnlyDictionary<string, double> configuredMap,
        List<string> errors)
    {
        var roleSpeedByRole = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in configuredMap)
        {
            var role = entry.Key?.Trim();
            if (string.IsNullOrWhiteSpace(role))
            {
                continue;
            }

            if (entry.Value <= 0)
            {
                errors.Add($"StoryTime:Generation:PosterRoleSpeedMultipliers role '{role}' must have a positive speed multiplier.");
                continue;
            }

            roleSpeedByRole[role.ToUpperInvariant()] = entry.Value;
        }

        if (roleSpeedByRole.Count == 0)
        {
            errors.Add("StoryTime:Generation:PosterRoleSpeedMultipliers must define at least one role.");
            return roleSpeedByRole;
        }

        foreach (var requiredRole in PosterRoles.Required)
        {
            if (!roleSpeedByRole.ContainsKey(requiredRole))
            {
                errors.Add($"StoryTime:Generation:PosterRoleSpeedMultipliers must include '{requiredRole}'.");
            }
        }

        if (roleSpeedByRole.ContainsKey(PosterRoles.Midground2) && !roleSpeedByRole.ContainsKey(PosterRoles.Midground1))
        {
            errors.Add(
                $"StoryTime:Generation:PosterRoleSpeedMultipliers cannot include {PosterRoles.Midground2} without {PosterRoles.Midground1}.");
        }

        return roleSpeedByRole;
    }

    private static void ValidateCorsOptions(CorsOptions options, List<string> errors)
    {
        var origins = options.AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (origins.Length == 0)
        {
            errors.Add("StoryTime:Cors:AllowedOrigins must define at least one origin.");
        }
        else
        {
            foreach (var origin in origins)
            {
                if (string.Equals(origin, "*", StringComparison.Ordinal))
                {
                    errors.Add("StoryTime:Cors:AllowedOrigins cannot include wildcard '*'.");
                    continue;
                }

                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                    string.IsNullOrWhiteSpace(uri.Scheme) ||
                    string.IsNullOrWhiteSpace(uri.Host))
                {
                    errors.Add($"StoryTime:Cors:AllowedOrigins contains invalid origin '{origin}'.");
                }
            }
        }

        if (options.AllowedMethods.All(string.IsNullOrWhiteSpace))
        {
            errors.Add("StoryTime:Cors:AllowedMethods must define at least one method.");
        }

        if (options.AllowedHeaders.All(string.IsNullOrWhiteSpace))
        {
            errors.Add("StoryTime:Cors:AllowedHeaders must define at least one header.");
        }
    }

    private static void ValidatePosterLayerContract(
        IReadOnlyList<PosterLayerRule> layerRules,
        string path,
        IReadOnlyDictionary<string, double> posterRoleSpeedByRole,
        List<string> errors)
    {
        if (layerRules.Count is < 3 or > 5)
        {
            errors.Add($"{path} must define 3-5 layers.");
            return;
        }

        var normalizedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in layerRules)
        {
            var role = rule.Role?.Trim() ?? string.Empty;
            if (!posterRoleSpeedByRole.TryGetValue(role, out var expectedSpeed))
            {
                errors.Add($"{path} includes unsupported role '{rule.Role}'.");
                continue;
            }

            if (!normalizedRoles.Add(role))
            {
                errors.Add($"{path} includes duplicate role '{role}'.");
            }

            if (Math.Abs(rule.SpeedMultiplier - expectedSpeed) > 0.0001)
            {
                errors.Add($"{path} role '{role}' must use SpeedMultiplier {expectedSpeed:0.###}.");
            }
        }

        foreach (var requiredRole in PosterRoles.Required)
        {
            if (!normalizedRoles.Contains(requiredRole))
            {
                errors.Add($"{path} must include '{requiredRole}'.");
            }
        }

        if (normalizedRoles.Contains(PosterRoles.Midground2) && !normalizedRoles.Contains(PosterRoles.Midground1))
        {
            errors.Add($"{path} cannot include {PosterRoles.Midground2} without {PosterRoles.Midground1}.");
        }
    }

    private static bool IsOpenRouterEndpoint(string endpoint)
    {
        if (!Uri.TryCreate(endpoint.Trim(), UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        return string.Equals(uri.Host, "openrouter.ai", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.EndsWith(".openrouter.ai", StringComparison.OrdinalIgnoreCase);
    }
}
