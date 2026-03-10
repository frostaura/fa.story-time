# Configuration Reference

This document is the operational reference for runtime configuration used by StoryTime backend and frontend.

## Sources of truth

- Backend defaults: `src/backend/StoryTime.Api/appsettings.json` (`StoryTime` section)
- Backend schema: `src/backend/StoryTime.Api/StoryTimeOptions.cs`
- Backend validation: `src/backend/StoryTime.Api/StoryTimeOptionsValidator.cs`
- Frontend defaults: `src/frontend/.env.example` parsed by `src/frontend/src/config/runtime.ts`

## Backend keys (`StoryTime:*`)

| Key | Type | Default (`appsettings.json`) |
| --- | --- | --- |
| `StoryTime:DefaultApprovalRequired` | boolean | `true` |
| `StoryTime:Ui:QuickGenerateVisible` | boolean | `true` |
| `StoryTime:Ui:DurationSliderVisible` | boolean | `true` |
| `StoryTime:Ui:ParentControlsEnabled` | boolean | `true` |
| `StoryTime:Ui:DurationMinMinutes` | number | `5` |
| `StoryTime:Ui:DurationMaxMinutes` | number | `15` |
| `StoryTime:Ui:DurationDefaultMinutes` | number | `6` |
| `StoryTime:Ui:DefaultChildName` | string | `"Dreamer"` |
| `StoryTime:ApiRoutes:HomeStatus` | string | `"/api/home/status"` |
| `StoryTime:ApiRoutes:SubscriptionWebhook` | string | `"/api/subscription/webhook"` |
| `StoryTime:ApiRoutes:SubscriptionPaywall` | string | `"/api/subscription/{softUserId}/paywall"` |
| `StoryTime:ApiRoutes:SubscriptionCheckoutSession` | string | `"/api/subscription/{softUserId}/checkout/session"` |
| `StoryTime:ApiRoutes:SubscriptionCheckoutComplete` | string | `"/api/subscription/{softUserId}/checkout/complete"` |
| `StoryTime:ApiRoutes:ParentGateRegister` | string | `"/api/parent/{softUserId}/gate/register"` |
| `StoryTime:ApiRoutes:ParentGateChallenge` | string | `"/api/parent/{softUserId}/gate/challenge"` |
| `StoryTime:ApiRoutes:ParentGateVerify` | string | `"/api/parent/{softUserId}/gate/verify"` |
| `StoryTime:ApiRoutes:ParentSettings` | string | `"/api/parent/{softUserId}/settings"` |
| `StoryTime:ApiRoutes:StoriesGenerate` | string | `"/api/stories/generate"` |
| `StoryTime:ApiRoutes:StoryApprove` | string | `"/api/stories/{storyId}/approve"` |
| `StoryTime:ApiRoutes:StoryFavorite` | string | `"/api/stories/{storyId}/favorite"` |
| `StoryTime:ApiRoutes:Library` | string | `"/api/library/{softUserId}"` |
| `StoryTime:ApiRoutes:LibraryStorageAudit` | string | `"/api/library/{softUserId}/storage-audit"` |
| `StoryTime:Cors:AllowedOrigins` | array | `["http://localhost:5173", "http://127.0.0.1:5173"]` |
| `StoryTime:Cors:AllowedMethods` | array | `["GET", "POST", "PUT", "OPTIONS"]` |
| `StoryTime:Cors:AllowedHeaders` | array | `["Content-Type", "Authorization"]` |
| `StoryTime:TierLimits:Trial:Concurrency` | number | `1` |
| `StoryTime:TierLimits:Trial:CooldownMinutes` | number | `30` |
| `StoryTime:TierLimits:Trial:MaxDurationMinutes` | number | `10` |
| `StoryTime:TierLimits:Plus:Concurrency` | number | `1` |
| `StoryTime:TierLimits:Plus:CooldownMinutes` | number | `30` |
| `StoryTime:TierLimits:Plus:MaxDurationMinutes` | number | `10` |
| `StoryTime:TierLimits:Premium:Concurrency` | number | `3` |
| `StoryTime:TierLimits:Premium:CooldownMinutes` | number | `15` |
| `StoryTime:TierLimits:Premium:MaxDurationMinutes` | number | `15` |
| `StoryTime:Generation:ForceProceduralPosterFallback` | boolean | `false` |
| `StoryTime:Generation:PersistSeriesStoryBible` | boolean | `true` |
| `StoryTime:Generation:PersistContinuityFacts` | boolean | `true` |
| `StoryTime:Generation:ContinuityFactRetentionLimit` | number | `30` |
| `StoryTime:Generation:StoryBibleFilePath` | string | `"data/story-bibles.json"` |
| `StoryTime:Generation:MinutesPerScene` | number | `2` |
| `StoryTime:Generation:MinSceneCount` | number | `3` |
| `StoryTime:Generation:MaxSceneCount` | number | `8` |
| `StoryTime:Generation:PosterModelRetryCount` | number | `2` |
| `StoryTime:Generation:PosterModelFailureRate` | number | `0` |
| `StoryTime:Generation:PosterModelFailureSeedPrefixes` | array | `[]` |
| `StoryTime:Generation:OneShotModeAliases` | array | `["one-shot", "oneshot"]` |
| `StoryTime:Generation:TeaserDurationSeconds` | number | `8` |
| `StoryTime:Generation:FullDurationSeconds` | number | `24` |
| `StoryTime:Generation:TeaserAudioAmplitudeScale` | number | `0.06` |
| `StoryTime:Generation:FullAudioAmplitudeScale` | number | `0.07` |
| `StoryTime:Generation:AudioSampleRate` | number | `16000` |
| `StoryTime:Generation:AudioBaseFrequencyHz` | number | `261.63` |
| `StoryTime:Generation:DataUris:AudioWavBase64Prefix` | string | `"data:audio/wav;base64,"` |
| `StoryTime:Generation:DataUris:AudioPayloadPrefix` | string | `"data:audio/"` |
| `StoryTime:Generation:DataUris:PosterSvgBase64Prefix` | string | `"data:image/svg+xml;base64,"` |
| `StoryTime:Generation:ProceduralAudio:IdentifierOffsetRange` | number | `64` |
| `StoryTime:Generation:ProceduralAudio:SegmentDivisor` | number | `5` |
| `StoryTime:Generation:ProceduralAudio:PhraseEnvelopeExponent` | number | `0.7` |
| `StoryTime:Generation:ProceduralAudio:MelodicBendAmplitude` | number | `0.06` |
| `StoryTime:Generation:ProceduralAudio:BreathNoiseAmplitude` | number | `0.05` |
| `StoryTime:Generation:ProceduralAudio:CarrierWeight` | number | `0.62` |
| `StoryTime:Generation:ProceduralAudio:Harmonic2Weight` | number | `0.25` |
| `StoryTime:Generation:ProceduralAudio:Harmonic3Weight` | number | `0.13` |
| `StoryTime:Generation:ProceduralPoster:RichDetailOpacity` | number | `0.35` |
| `StoryTime:Generation:ProceduralPoster:FallbackOpacity` | number | `0.24` |
| `StoryTime:Generation:ProceduralPoster:RichDetailStarCount` | number | `18` |
| `StoryTime:Generation:ProceduralPoster:FallbackStarCount` | number | `9` |
| `StoryTime:Generation:ProceduralPosterGeometry:CanvasWidth` | number | `1024` |
| `StoryTime:Generation:ProceduralPosterGeometry:CanvasHeight` | number | `1024` |
| `StoryTime:Generation:ProceduralPosterGeometry:ViewBoxWidth` | number | `1024` |
| `StoryTime:Generation:ProceduralPosterGeometry:ViewBoxHeight` | number | `1024` |
| `StoryTime:Generation:ProceduralPosterGeometry:HorizonBaseY` | number | `540` |
| `StoryTime:Generation:ProceduralPosterGeometry:HorizonVariance` | number | `140` |
| `StoryTime:Generation:ProceduralPosterGeometry:DriftVariance` | number | `160` |
| `StoryTime:Generation:ProceduralPosterGeometry:DriftCenterOffset` | number | `80` |
| `StoryTime:Generation:ProceduralPosterGeometry:MoonCenterX` | number | `512` |
| `StoryTime:Generation:ProceduralPosterGeometry:MoonCenterY` | number | `300` |
| `StoryTime:Generation:ProceduralPosterGeometry:StarBaseX` | number | `60` |
| `StoryTime:Generation:ProceduralPosterGeometry:StarRangeX` | number | `900` |
| `StoryTime:Generation:ProceduralPosterGeometry:StarBaseY` | number | `50` |
| `StoryTime:Generation:ProceduralPosterGeometry:StarRangeY` | number | `420` |
| `StoryTime:Generation:ProceduralPosterGeometry:StarBaseRadius` | number | `1` |
| `StoryTime:Generation:ProceduralPosterGeometry:StarRadiusRange` | number | `3` |
| `StoryTime:Generation:ProceduralPosterGeometry:StarOpacity` | number | `0.55` |
| `StoryTime:Generation:PosterModelProvider:Enabled` | boolean | `true` |
| `StoryTime:Generation:PosterModelProvider:LocalFallbackEnabled` | boolean | `true` |
| `StoryTime:Generation:PosterModelProvider:Endpoint` | string | `""` |
| `StoryTime:Generation:PosterModelProvider:ApiKey` | string | `""` |
| `StoryTime:Generation:PosterModelProvider:TimeoutSeconds` | number | `1` |
| `StoryTime:Generation:NarrationProvider:Enabled` | boolean | `true` |
| `StoryTime:Generation:NarrationProvider:LocalFallbackEnabled` | boolean | `true` |
| `StoryTime:Generation:NarrationProvider:Endpoint` | string | `""` |
| `StoryTime:Generation:NarrationProvider:ApiKey` | string | `""` |
| `StoryTime:Generation:NarrationProvider:TimeoutSeconds` | number | `1` |
| `StoryTime:Generation:ProceduralPosterFallbackBudgetMilliseconds` | number | `200` |
| `StoryTime:Generation:PolishToneTag` | string | `"calm-bedtime"` |
| `StoryTime:Generation:ModeLabels:Series` | string | `"Series"` |
| `StoryTime:Generation:ModeLabels:OneShot` | string | `"One-shot"` |
| `StoryTime:Generation:TitleTemplates` | array | `["{ChildName} and the {ArcName} ({ModeLabel} {EpisodeNumber})", "{ArcName} Night {EpisodeNumber} - {ChildName}", "{ChildName} in {ArcName} on {DateStamp}"]` |
| `StoryTime:Generation:CalmOpeners` | array | `["A soft lantern glows", "Moonlight settles over the path", "A hush of starlight appears"]` |
| `StoryTime:Generation:CalmTransitions` | array | `["the next moment stays steady and kind", "the journey keeps a gentle rhythm", "comfort grows with each step"]` |
| `StoryTime:Generation:CalmClosers` | array | `["The night wraps everyone in peaceful rest.", "A final deep breath brings sleepy smiles.", "The adventure rests quietly until tomorrow night."]` |
| `StoryTime:Generation:ArcNames` | array | `["Moonlit Meadow", "Star Lantern Trail", "Cloud Harbor"]` |
| `StoryTime:Generation:ThemeTrackIds` | array | `["soft-piano", "night-chimes", "gentle-waves"]` |
| `StoryTime:Generation:NarrationStyles` | array | `["warm-whisper", "calm-storyteller"]` |
| `StoryTime:Generation:PosterLayers:0:Role` | string | `"BACKGROUND"` |
| `StoryTime:Generation:PosterLayers:0:SpeedMultiplier` | number | `0.2` |
| `StoryTime:Generation:PosterLayers:1:Role` | string | `"MIDGROUND_1"` |
| `StoryTime:Generation:PosterLayers:1:SpeedMultiplier` | number | `0.5` |
| `StoryTime:Generation:PosterLayers:2:Role` | string | `"FOREGROUND"` |
| `StoryTime:Generation:PosterLayers:2:SpeedMultiplier` | number | `1.0` |
| `StoryTime:Generation:PosterLayers:3:Role` | string | `"PARTICLES"` |
| `StoryTime:Generation:PosterLayers:3:SpeedMultiplier` | number | `1.3` |
| `StoryTime:Generation:PosterRoleSpeedMultipliers:BACKGROUND` | number | `0.2` |
| `StoryTime:Generation:PosterRoleSpeedMultipliers:MIDGROUND_1` | number | `0.5` |
| `StoryTime:Generation:PosterRoleSpeedMultipliers:MIDGROUND_2` | number | `0.8` |
| `StoryTime:Generation:PosterRoleSpeedMultipliers:FOREGROUND` | number | `1.0` |
| `StoryTime:Generation:PosterRoleSpeedMultipliers:PARTICLES` | number | `1.3` |
| `StoryTime:Generation:Fallbacks:TitleTemplate` | string | `"{ChildName} in {ArcName} ({ModeLabel} {EpisodeNumber})"` |
| `StoryTime:Generation:Fallbacks:ArcName` | string | `"Moonlit Meadow"` |
| `StoryTime:Generation:Fallbacks:OneShotCompanionName` | string | `"a gentle friend"` |
| `StoryTime:Generation:Fallbacks:OneShotSetting` | string | `"moonlit meadow paths"` |
| `StoryTime:Generation:Fallbacks:OneShotMood` | string | `"softly adventurous"` |
| `StoryTime:Generation:Fallbacks:ThemeTrackId` | string | `"soft-piano"` |
| `StoryTime:Generation:Fallbacks:NarrationStyle` | string | `"warm-whisper"` |
| `StoryTime:Generation:Fallbacks:CalmOpener` | string | `"A calm hush settles in"` |
| `StoryTime:Generation:Fallbacks:CalmTransition` | string | `"the moment flows gently forward"` |
| `StoryTime:Generation:Fallbacks:CalmCloser` | string | `"Everyone rests with a steady breath."` |
| `StoryTime:Generation:Fallbacks:PersistentRecurringCharacterAlias` | string | `"Dreamer"` |
| `StoryTime:Generation:Fallbacks:PosterLayers:0:Role` | string | `"BACKGROUND"` |
| `StoryTime:Generation:Fallbacks:PosterLayers:0:SpeedMultiplier` | number | `0.2` |
| `StoryTime:Generation:Fallbacks:PosterLayers:1:Role` | string | `"MIDGROUND_1"` |
| `StoryTime:Generation:Fallbacks:PosterLayers:1:SpeedMultiplier` | number | `0.5` |
| `StoryTime:Generation:Fallbacks:PosterLayers:2:Role` | string | `"FOREGROUND"` |
| `StoryTime:Generation:Fallbacks:PosterLayers:2:SpeedMultiplier` | number | `1.0` |
| `StoryTime:Generation:Fallbacks:PosterLayers:3:Role` | string | `"PARTICLES"` |
| `StoryTime:Generation:Fallbacks:PosterLayers:3:SpeedMultiplier` | number | `1.3` |
| `StoryTime:Generation:AiOrchestration:Enabled` | boolean | `true` |
| `StoryTime:Generation:AiOrchestration:LocalFallbackEnabled` | boolean | `false` |
| `StoryTime:Generation:AiOrchestration:EnforceOpenRouterEndpoint` | boolean | `true` |
| `StoryTime:Generation:AiOrchestration:Endpoint` | string | `"https://openrouter.ai/api/v1/chat/completions"` |
| `StoryTime:Generation:AiOrchestration:ApiKey` | string | `""` |
| `StoryTime:Generation:AiOrchestration:OpenRouterReferer` | string | `"https://storytime.local"` |
| `StoryTime:Generation:AiOrchestration:OpenRouterTitle` | string | `"StoryTime"` |
| `StoryTime:Generation:AiOrchestration:Model` | string | `"storytime-orchestrator"` |
| `StoryTime:Generation:AiOrchestration:TimeoutSeconds` | number | `15` |
| `StoryTime:Generation:AiOrchestration:StageResponseFormatInstruction` | string | `"Return only valid JSON with shape {\"text\": string|null, \"items\": string[]|null}. Do not include markdown fences."` |
| `StoryTime:Generation:AiOrchestration:StageNames:Outline` | string | `"outline"` |
| `StoryTime:Generation:AiOrchestration:StageNames:ScenePlan` | string | `"scene_plan"` |
| `StoryTime:Generation:AiOrchestration:StageNames:SceneBatch` | string | `"scene_batch"` |
| `StoryTime:Generation:AiOrchestration:StageNames:Stitch` | string | `"stitch"` |
| `StoryTime:Generation:AiOrchestration:StageNames:Polish` | string | `"polish"` |
| `StoryTime:Generation:NarrativeTemplates:SeriesRecapFirstEpisode` | string | `"{Protagonist} begins a calm bedtime adventure in {ArcName}."` |
| `StoryTime:Generation:NarrativeTemplates:SeriesRecapContinuation` | string | `"Previously: {PreviousSummary}"` |
| `StoryTime:Generation:NarrativeTemplates:ArcObjective` | string | `"Find tonight's calm ending in {ArcName}."` |
| `StoryTime:Generation:NarrativeTemplates:ContinuityFact` | string | `"Episode {EpisodeNumber} generated at {Timestamp} with {SceneCount} scenes."` |
| `StoryTime:Generation:NarrativeTemplates:EpisodeSummary` | string | `"A calm episode progressed through {SceneCount} bedtime scenes."` |
| `StoryTime:Generation:NarrativeTemplates:PersistedArcObjective` | string | `"Find tonight's calm ending in {ArcName}."` |
| `StoryTime:Generation:NarrativeTemplates:PersistedEpisodeSummary` | string | `"Episode {EpisodeNumber} completed."` |
| `StoryTime:Generation:NarrativeTemplates:OneShotOutline` | string | `"{Protagonist} and {CompanionName} enjoy a {Mood} one-shot bedtime adventure across {SceneCount} scenes in {Setting} with {ThemeTrackId} underscoring and {NarrationStyle} narration."` |
| `StoryTime:Generation:NarrativeTemplates:SeriesOutline` | string | `"{Protagonist} explores {ArcContext} through {SceneCount} bedtime scenes with calming progression."` |
| `StoryTime:Generation:NarrativeTemplates:SeriesOutlineStandaloneArcContext` | string | `"a standalone dream"` |
| `StoryTime:Generation:NarrativeTemplates:SeriesOutlineArcContext` | string | `"the {ArcName} arc"` |
| `StoryTime:Generation:NarrativeTemplates:ScenePlanStandaloneObjective` | string | `"gentle discovery {SceneNumber} in {ArcName}"` |
| `StoryTime:Generation:NarrativeTemplates:ScenePlanSeriesObjective` | string | `"{ArcName} milestone {MilestoneNumber}"` |
| `StoryTime:Generation:NarrativeTemplates:ScenePlanOpening` | string | `"opening calm from outline: {Outline}"` |
| `StoryTime:Generation:NarrativeTemplates:Scene` | string | `"Scene {SceneNumber}: {Opener} as {Protagonist} follows {Objective}; {Transition}.{OneShotDetail}{ArcNote}"` |
| `StoryTime:Generation:NarrativeTemplates:SceneArcNote` | string | `" The arc objective is {ArcObjective}."` |
| `StoryTime:Generation:NarrativeTemplates:StitchedArcLead` | string | `"Arc {EpisodeNumber}: "` |
| `StoryTime:Generation:NarrativeTemplates:OneShotDetailCompanion` | string | `"companion: {Value}"` |
| `StoryTime:Generation:NarrativeTemplates:OneShotDetailSetting` | string | `"setting: {Value}"` |
| `StoryTime:Generation:NarrativeTemplates:OneShotDetailMood` | string | `"mood: {Value}"` |
| `StoryTime:ParentGate:ChallengeTtlMinutes` | number | `10` |
| `StoryTime:ParentGate:ChallengeByteLength` | number | `32` |
| `StoryTime:ParentGate:SessionTtlMinutes` | number | `10` |
| `StoryTime:ParentGate:RequireAssertion` | boolean | `true` |
| `StoryTime:ParentGate:RequireChallengeBoundAssertion` | boolean | `true` |
| `StoryTime:ParentGate:RequireRegisteredCredential` | boolean | `true` |
| `StoryTime:ParentGate:RequireUserVerification` | boolean | `false` |
| `StoryTime:ParentGate:RelyingPartyId` | string | `"localhost"` |
| `StoryTime:ParentGate:AssertionType` | string | `"webauthn.get"` |
| `StoryTime:ParentGate:AllowedOrigins` | array | `["http://localhost", "http://127.0.0.1"]` |
| `StoryTime:ParentDefaults:NotificationsEnabled` | boolean | `false` |
| `StoryTime:ParentDefaults:AnalyticsEnabled` | boolean | `false` |
| `StoryTime:Catalog:Provider` | string | `"FileSystem"` |
| `StoryTime:Catalog:FilePath` | string | `"data/story-catalog.json"` |
| `StoryTime:Catalog:LibraryTitleWithArcTemplate` | string | `"{ModeLabel} - {ArcName} #{EpisodeNumber}"` |
| `StoryTime:Catalog:LibraryTitleWithoutArcTemplate` | string | `"{ModeLabel} story {GeneratedAtHHmm}"` |
| `StoryTime:Catalog:RecentItemsLimit` | number | `20` |
| `StoryTime:Catalog:KidShelfRecentLimit` | number | `8` |
| `StoryTime:Catalog:KidShelfFavoritesLimit` | number | `8` |
| `StoryTime:Catalog:HashedIdentifierByteLength` | number | `6` |
| `StoryTime:Catalog:AnonymousIdentifierFallback` | string | `"anonymous"` |
| `StoryTime:Catalog:NarrativeTextMinWords` | number | `18` |
| `StoryTime:Catalog:SemanticNarrativeTextMinWords` | number | `8` |
| `StoryTime:Catalog:NarrativeLeakageMarkers` | array | `["scene ", "previously:", "episode ", " arc ", "companion:", "setting:", "mood:"]` |
| `StoryTime:Checkout:DefaultTier` | string | `"Trial"` |
| `StoryTime:Checkout:UpgradeTier` | string | `"Premium"` |
| `StoryTime:Checkout:UpgradeUrl` | string | `"/subscribe"` |
| `StoryTime:Checkout:SessionTtlMinutes` | number | `15` |
| `StoryTime:Checkout:Provider:Mode` | string | `"External"` |
| `StoryTime:Checkout:Provider:LocalFallbackEnabled` | boolean | `true` |
| `StoryTime:Checkout:Provider:Endpoint` | string | `""` |
| `StoryTime:Checkout:Provider:ApiKey` | string | `""` |
| `StoryTime:Checkout:Provider:TimeoutSeconds` | number | `1` |
| `StoryTime:Messages:UpgradeForLongerStories` | string | `"Upgrade to {UpgradeTier} for longer bedtime stories."` |
| `StoryTime:Messages:InvalidSubscriptionPayload` | string | `"Invalid subscription payload."` |
| `StoryTime:Messages:UnableToCreateCheckoutSession` | string | `"Unable to create checkout session."` |
| `StoryTime:Messages:InvalidOrExpiredCheckoutSession` | string | `"Invalid or expired checkout session."` |
| `StoryTime:Messages:SoftUserIdRequired` | string | `"softUserId is required."` |
| `StoryTime:Messages:InvalidParentCredential` | string | `"Invalid parent credential."` |
| `StoryTime:Messages:DurationMinutesMustBeGreaterThanZero` | string | `"durationMinutes must be greater than zero."` |
| `StoryTime:Messages:SubscriptionDurationExceedsTier` | string | `"Tier '{Tier}' supports up to {MaxDurationMinutes} minutes."` |
| `StoryTime:Messages:SubscriptionCooldownActive` | string | `"Cooldown active."` |
| `StoryTime:Messages:SubscriptionConcurrencyLimitReached` | string | `"Concurrency limit reached."` |
| `StoryTime:Messages:SubscriptionAllowed` | string | `"Allowed"` |
| `StoryTime:Messages:UnsupportedCatalogProvider` | string | `"Unsupported StoryTime catalog provider '{Provider}'."` |
| `StoryTime:Messages:InternalErrors:TierLimitsMustDefineTrial` | string | `"StoryTime:TierLimits must define at least the Trial tier."` |
| `StoryTime:Messages:InternalErrors:CorsConfigurationRequired` | string | `"StoryTime:Cors configuration is required."` |
| `StoryTime:Messages:InternalErrors:PosterLayerConfigMustDefine3To5Layers` | string | `"StoryTime:Generation poster layer config must define 3-5 layers."` |
| `StoryTime:Messages:InternalErrors:ProceduralAudioWeightsMustSumPositive` | string | `"StoryTime:Generation:ProceduralAudio harmonic weights must add up to a positive value."` |
| `StoryTime:Messages:InternalErrors:PosterModelProviderEndpointRequiredWhenEnabled` | string | `"StoryTime:Generation:PosterModelProvider:Endpoint must be configured when enabled."` |
| `StoryTime:Messages:InternalErrors:PosterModelProviderFailedWithStatus` | string | `"StoryTime:Generation:PosterModelProvider failed with status {StatusCode}."` |
| `StoryTime:Messages:InternalErrors:PosterModelProviderReturnedNoLayers` | string | `"StoryTime:Generation:PosterModelProvider returned no poster layers."` |
| `StoryTime:Messages:InternalErrors:PosterModelProviderResponseMissingLayer` | string | `"StoryTime:Generation:PosterModelProvider response is missing '{Role}' layer."` |
| `StoryTime:Messages:InternalErrors:NarrationProviderEndpointRequiredWhenEnabled` | string | `"StoryTime:Generation:NarrationProvider:Endpoint must be configured when enabled."` |
| `StoryTime:Messages:InternalErrors:NarrationProviderFailedWithStatus` | string | `"StoryTime:Generation:NarrationProvider failed with status {StatusCode}."` |
| `StoryTime:Messages:InternalErrors:NarrationProviderReturnedEmptyAudio` | string | `"StoryTime:Generation:NarrationProvider returned empty audio."` |
| `StoryTime:Messages:InternalErrors:NarrationProviderReturnedNonAudioPayload` | string | `"StoryTime:Generation:NarrationProvider returned a non-audio payload."` |
| `StoryTime:Messages:InternalErrors:ProceduralPosterFallbackBudgetMustBePositive` | string | `"StoryTime:Generation:ProceduralPosterFallbackBudgetMilliseconds must be greater than zero."` |
| `StoryTime:Messages:InternalErrors:ProceduralPosterFallbackExceededBudget` | string | `"StoryTime procedural poster fallback exceeded budget: {ElapsedMs}ms > {BudgetMs}ms."` |
| `StoryTime:Messages:InternalErrors:PosterRoleSpeedMultipliersMustDefineAtLeastOneRole` | string | `"StoryTime:Generation:PosterRoleSpeedMultipliers must define at least one role."` |
| `StoryTime:Messages:InternalErrors:PosterLayerConfigIncludesUnsupportedRoles` | string | `"StoryTime:Generation poster layer config includes unsupported roles: {Roles}."` |
| `StoryTime:Messages:InternalErrors:PosterLayerConfigMustNormalize3To5Layers` | string | `"StoryTime:Generation poster layer config must normalize to 3-5 layers."` |
| `StoryTime:Messages:InternalErrors:PosterLayerConfigMustIncludeRequiredRoles` | string | `"StoryTime:Generation poster layer config must include {BackgroundRole}, {ForegroundRole}, and {ParticlesRole} roles."` |
| `StoryTime:Messages:InternalErrors:PosterLayerConfigMidgroundRoleDependency` | string | `"StoryTime:Generation poster layer config cannot include {Midground2Role} without {Midground1Role}."` |
| `StoryTime:Messages:InternalErrors:CheckoutProviderEndpointRequiredWhenModeExternal` | string | `"StoryTime:Checkout:Provider:Endpoint must be configured when Mode is External."` |
| `StoryTime:Messages:InternalErrors:CheckoutProviderCreateSessionFailedWithStatus` | string | `"StoryTime:Checkout:Provider create session failed with status {StatusCode}."` |
| `StoryTime:Messages:InternalErrors:CheckoutProviderCreateSessionReturnedInvalidPayload` | string | `"StoryTime:Checkout:Provider create session returned an invalid payload."` |
| `StoryTime:Messages:InternalErrors:CheckoutProviderReturnedUnsupportedTier` | string | `"StoryTime:Checkout:Provider returned unsupported tier '{Tier}'."` |
| `StoryTime:Messages:InternalErrors:CheckoutProviderCompletionFailedWithStatus` | string | `"StoryTime:Checkout:Provider completion failed with status {StatusCode}."` |
| `StoryTime:Messages:InternalErrors:CheckoutProviderCompletionReturnedEmptyPayload` | string | `"StoryTime:Checkout:Provider completion returned an empty payload."` |
| `StoryTime:Messages:InternalErrors:CheckoutDefaultTierMustBeConfigured` | string | `"StoryTime:Checkout:DefaultTier must be configured."` |
| `StoryTime:Messages:InternalErrors:CheckoutDefaultTierMustMatchTierLimits` | string | `"StoryTime:Checkout:DefaultTier '{Tier}' must match one of the configured StoryTime:TierLimits keys."` |
| `StoryTime:Messages:InternalErrors:AiOrchestrationEndpointRequiredWhenEnabled` | string | `"StoryTime:Generation:AiOrchestration:Endpoint must be configured when enabled."` |
| `StoryTime:Messages:InternalErrors:AiOrchestrationEndpointMustTargetOpenRouter` | string | `"StoryTime:Generation:AiOrchestration:Endpoint must target openrouter.ai."` |
| `StoryTime:Messages:InternalErrors:AiOrchestrationStageFailedWithStatus` | string | `"AI orchestration stage '{Stage}' failed with status {StatusCode}."` |
| `StoryTime:Messages:InternalErrors:AiOrchestrationStageReturnedEmptyResponse` | string | `"AI orchestration stage '{Stage}' returned an empty response."` |
| `StoryTime:Messages:InternalErrors:PersistentRecurringCharacterAliasMustBeConfigured` | string | `"Persistent recurring character alias must be configured."` |

### Backend validation highlights

- `StoryTime:TierLimits` must include at least the configured default tier (trial by default).
- `StoryTime:Generation:PosterLayers` normalizes to 3-5 layers and must include required roles (`BACKGROUND`, `FOREGROUND`, `PARTICLES`).
- `StoryTime:Generation:PosterRoleSpeedMultipliers` must define at least one role and compatible role dependencies.
- `StoryTime:Generation:AiOrchestration` always requires OpenRouter endpoint semantics (`Endpoint` targets `openrouter.ai`, `EnforceOpenRouterEndpoint=true`, `LocalFallbackEnabled=false`); when `Enabled=true`, `Model`, `TimeoutSeconds`, `StageResponseFormatInstruction`, and `StageNames:*` are required.
- `StoryTime:Generation:AiOrchestration:OpenRouterReferer` and `OpenRouterTitle` are required when AI orchestration is enabled.
- External providers require endpoints when enabled (`PosterModelProvider`, `NarrationProvider`, `AiOrchestration`, `Checkout:Provider` in external mode).
- Procedural limits enforce positive values (for example poster fallback budget, audio harmonic mix).

## Frontend keys (`VITE_*`)

| Key | Default (`.env.example`) |
| --- | --- |
| `VITE_API_BASE_URL` | `` |
| `VITE_API_ROUTE_HOME_STATUS` | `/api/home/status` |
| `VITE_API_ROUTE_STORIES_BASE` | `/api/stories` |
| `VITE_API_ROUTE_STORIES_GENERATE` | `/api/stories/generate` |
| `VITE_API_ROUTE_SUBSCRIPTION_BASE` | `/api/subscription` |
| `VITE_API_ROUTE_PARENT_BASE` | `/api/parent` |
| `VITE_API_ROUTE_LIBRARY_BASE` | `/api/library` |
| `VITE_DEFAULT_DURATION_MINUTES` | `5` |
| `VITE_DEFAULT_DURATION_MAX_MINUTES` | `15` |
| `VITE_DEFAULT_DURATION_SELECTION` | `6` |
| `VITE_DEFAULT_CHILD_NAME` | `Dreamer` |
| `VITE_DEFAULT_NOTIFICATIONS_ENABLED` | `false` |
| `VITE_DEFAULT_ANALYTICS_ENABLED` | `false` |
| `VITE_HOME_STATUS_QUICK_GENERATE_VISIBLE` | `true` |
| `VITE_HOME_STATUS_DURATION_SLIDER_VISIBLE` | `true` |
| `VITE_HOME_STATUS_PARENT_CONTROLS_ENABLED` | `true` |
| `VITE_SERVICE_WORKER_PATH` | `/service-worker.js` |
| `VITE_SERVICE_WORKER_CACHE_NAME` | `storytime-static-v1` |
| `VITE_SERVICE_WORKER_APP_SHELL` | `/,/index.html` |
| `VITE_LIBRARY_RECENT_LIMIT` | `20` |
| `VITE_STORAGE_KEY_STORY_ARTIFACTS` | `storyArtifacts` |
| `VITE_STORAGE_KEY_SOFT_USER_ID` | `softUserId` |
| `VITE_STORAGE_KEY_CHILD_PROFILE` | `childProfile` |
| `VITE_STORAGE_KEY_PARENT_CREDENTIAL` | `parentCredential` |
| `VITE_PARENT_GATE_WEBAUTHN_RP_DISPLAY_NAME` | `StoryTime Parent Gate` |
| `VITE_PARENT_GATE_WEBAUTHN_USER_DISPLAY_NAME` | `Parent` |
| `VITE_PARENT_GATE_WEBAUTHN_TIMEOUT_MS` | `60000` |
| `VITE_PARENT_GATE_WEBAUTHN_USER_ID_MAX_LENGTH` | `64` |
| `VITE_PARENT_GATE_WEBAUTHN_USER_ID_PROTOCOL_MAX_LENGTH` | `64` |
| `VITE_PARENT_GATE_WEBAUTHN_USER_NAME_PREFIX` | `parent-` |
| `VITE_PARENT_GATE_WEBAUTHN_ATTESTATION` | `none` |
| `VITE_PARENT_GATE_WEBAUTHN_RESIDENT_KEY` | `discouraged` |
| `VITE_PARENT_GATE_WEBAUTHN_COSE_ALGORITHM` | `-7` |
| `VITE_PARENT_GATE_WEBAUTHN_USER_VERIFICATION` | `preferred` |
| `VITE_PARENT_GATE_CREDENTIAL_KIND` | `webauthn` |
| `VITE_PARENT_GATE_ASSERTION_TYPE` | `webauthn.get` |
| `VITE_POSTER_PARALLAX_MIN_DURATION_SECONDS` | `7` |
| `VITE_POSTER_PARALLAX_DURATION_DIVISOR` | `24` |
| `VITE_POSTER_PARALLAX_MAX_DELAY_SECONDS` | `2` |
| `VITE_POSTER_PARALLAX_DELAY_DIVISOR` | `3` |
| `VITE_POSTER_PARALLAX_DEPTH_SCALE` | `4` |
| `VITE_APP_MESSAGES_JSON` | `` |

### Frontend validation highlights

- `runtime.ts` requires all configured keys; missing or invalid values throw during startup.
- WebAuthn fields are constrained to valid enum/string domains (`attestation`, `residentKey`, `userVerification`).
- `VITE_PARENT_GATE_WEBAUTHN_USER_ID_MAX_LENGTH` must be a positive integer ≤ `VITE_PARENT_GATE_WEBAUTHN_USER_ID_PROTOCOL_MAX_LENGTH`.
- Route keys are normalized to leading-slash route paths.

### Intentional code-local constants

- Business behavior stays config-driven (`StoryTime:*` and `VITE_*`), but some values intentionally remain in code because they are protocol, algorithm, or test-harness details rather than product behavior.
- Examples:
  - procedural media synthesis constants in backend services,
  - browser/test host defaults such as Playwright and Vitest local ports (overrideable through environment variables),
  - local-development WebAuthn/CORS defaults that are explicitly overridden for production deployment.
- The rule of thumb in this repo is: entitlement, routing, limits, safety policy, and user-facing defaults belong in configuration; rendering math, protocol constants, and deterministic test harness defaults may remain code-local when they do not change shipped product behavior.

### Production deployment notes

- **`StoryTime:ParentGate:RelyingPartyId`**: Defaults to `"localhost"` for local development. In production, this **must** be overridden via environment variable (`StoryTime__ParentGate__RelyingPartyId`) or a production `appsettings.Production.json` to match the deployment domain (e.g., `"storytime.example.com"`). WebAuthn assertions will fail if the relying party ID does not match the origin used by the browser.
- **`StoryTime:ParentGate:AllowedOrigins`**: Must be updated to include the production origin(s) (e.g., `["https://storytime.example.com"]`).
- **`StoryTime:Cors:AllowedOrigins`**: Must include the production frontend origin.
- **API key placeholders** (`StoryTime:Generation:PosterModelProvider:ApiKey`, etc.): Empty in source; must be provided via environment variables or secret management in production.
