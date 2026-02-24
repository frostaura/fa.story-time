# TaleWeaver — Class Diagrams

> Mermaid class diagrams for backend entities, service interfaces, and pipeline DTOs.

---

## 1 Backend Entities

```mermaid
classDiagram
    class BaseEntity {
        +Guid Id
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +bool IsDeleted
    }

    class Tier {
        +string Name
        +int Concurrency
        +int CooldownMinutes
        +string[] AllowedLengths
        +bool HasLockScreenArt
        +bool HasLongStories
        +bool HasHighQualityBudget
        +ICollection~SubscriptionPlan~ SubscriptionPlans
        +ICollection~CooldownState~ CooldownStates
    }

    class SubscriptionPlan {
        +Guid TierId
        +string StripePriceId
        +string Name
        +int MonthlyPriceCents
        +int TrialDays
        +Tier Tier
        +ICollection~Subscription~ Subscriptions
    }

    class Subscription {
        +string SoftUserId
        +Guid PlanId
        +string StripeSubscriptionId
        +string StripeCustomerId
        +SubscriptionStatus Status
        +DateTime CurrentPeriodStart
        +DateTime CurrentPeriodEnd
        +DateTime? TrialEnd
        +SubscriptionPlan Plan
    }

    class FeatureFlag {
        +string Key
        +string Value
        +string Description
    }

    class AppConfig {
        +string Key
        +string Value
        +string Category
    }

    class CooldownState {
        +string SoftUserId
        +DateTime LastGenerationAt
        +Guid TierId
        +Tier Tier
    }

    class SubscriptionStatus {
        <<enumeration>>
        Trialing
        Active
        PastDue
        Canceled
    }

    BaseEntity <|-- Tier
    BaseEntity <|-- SubscriptionPlan
    BaseEntity <|-- Subscription
    BaseEntity <|-- FeatureFlag
    BaseEntity <|-- AppConfig
    BaseEntity <|-- CooldownState

    Tier "1" --> "*" SubscriptionPlan : has many
    Tier "1" --> "*" CooldownState : determines cooldown
    SubscriptionPlan "1" --> "*" Subscription : has many
    Subscription --> SubscriptionStatus : uses
```

---

## 2 Backend Service Interfaces

```mermaid
classDiagram
    class IOpenRouterService {
        <<interface>>
        +GenerateTextAsync(prompt: string, model: string) Task~string~
        +GenerateImageAsync(prompt: string, model: string) Task~byte[]~
        +GenerateTextStreamAsync(prompt: string, model: string) IAsyncEnumerable~string~
    }

    class IStoryGenerationPipeline {
        <<interface>>
        +GenerateOutlineAsync(request: OutlineRequest) Task~OutlineResponse~
        +PlanScenesAsync(outline: OutlineResponse, bible: StoryBible?) Task~ScenePlan~
        +GenerateSceneBatchAsync(plan: ScenePlan, outline: OutlineResponse) Task~Scene[]~
        +StitchScenesAsync(scenes: Scene[]) Task~string~
        +PolishStoryAsync(stitchedText: string, profile: ChildProfileSnapshot) Task~GenerationResult~
        +ExecuteFullPipelineAsync(request: OutlineRequest) Task~GenerationResult~
    }

    class IStripeWebhookHandler {
        <<interface>>
        +HandleCheckoutCompletedAsync(session: StripeSession) Task
        +HandleInvoicePaidAsync(invoice: StripeInvoice) Task
        +HandleInvoicePaymentFailedAsync(invoice: StripeInvoice) Task
        +HandleSubscriptionDeletedAsync(subscription: StripeSubscription) Task
        +HandleSubscriptionUpdatedAsync(subscription: StripeSubscription) Task
        +ValidateSignatureAsync(payload: string, signature: string) Task~bool~
    }

    class ITtsService {
        <<interface>>
        +SynthesizeAsync(text: string, voiceId: string) Task~byte[]~
        +GetAvailableVoicesAsync() Task~TtsVoice[]~
        +SynthesizeScenesAsync(scenes: Scene[], voiceId: string) Task~AudioResult[]~
    }

    class ICooldownService {
        <<interface>>
        +CheckCooldownAsync(softUserId: string) Task~CooldownCheck~
        +RecordGenerationAsync(softUserId: string) Task
        +GetRemainingSecondsAsync(softUserId: string) Task~int~
    }

    class ISubscriptionService {
        <<interface>>
        +GetActiveSubscriptionAsync(softUserId: string) Task~Subscription?~
        +GetTierAsync(softUserId: string) Task~Tier~
        +CreateTrialAsync(softUserId: string) Task~Subscription~
        +CreateCheckoutSessionAsync(softUserId: string, planId: Guid) Task~string~
    }

    class IConfigService {
        <<interface>>
        +GetFeatureFlagAsync(key: string) Task~string?~
        +GetAppConfigAsync(key: string) Task~string?~
        +GetAppConfigByCategoryAsync(category: string) Task~Dictionary~string, string~~
        +GetTiersAsync() Task~Tier[]~
        +GetPlansAsync() Task~SubscriptionPlan[]~
    }

    IStoryGenerationPipeline ..> IOpenRouterService : depends on
    IStoryGenerationPipeline ..> ITtsService : depends on
    IStoryGenerationPipeline ..> ICooldownService : depends on
    IStoryGenerationPipeline ..> ISubscriptionService : checks tier
```

---

## 3 Pipeline DTOs

```mermaid
classDiagram
    class OutlineRequest {
        +string SoftUserId
        +ChildProfileSnapshot Profile
        +string Duration
        +StoryBible? Bible
    }

    class ChildProfileSnapshot {
        +string Name
        +string AgeRange
        +string[] Interests
        +string[] Themes
        +string VoiceId
    }

    class OutlineResponse {
        +string Title
        +string Theme
        +string Moral
        +CharacterOutline[] Characters
        +string ActOneSummary
        +string ActTwoSummary
        +string ActThreeSummary
    }

    class CharacterOutline {
        +string Name
        +string Role
        +string Description
        +string VisualDescription
    }

    class ScenePlan {
        +string StoryTitle
        +SceneSpec[] Scenes
        +int TotalSceneCount
    }

    class SceneSpec {
        +int Index
        +string Setting
        +string[] CharactersPresent
        +string[] KeyEvents
        +string EmotionalBeat
        +string IllustrationPromptSeed
    }

    class Scene {
        +int Index
        +string Text
        +string IllustrationPrompt
        +byte[]? ImageData
        +string? PosterLayerSetId
        +string? AudioEntryId
    }

    class StoryBible {
        +string Id
        +string SeriesTitle
        +BibleCharacter[] Characters
        +string[] WorldRules
        +BiblePlotPoint[] PlotPoints
    }

    class BibleCharacter {
        +string Name
        +string Description
        +string[] Traits
        +string VisualDescription
    }

    class BiblePlotPoint {
        +string EpisodeId
        +string Summary
        +string Impact
    }

    class PosterLayerSet {
        +string Id
        +string StoryId
        +int SceneIndex
        +PosterLayer[] Layers
    }

    class PosterLayer {
        +PosterDepth Depth
        +byte[] ImageData
    }

    class PosterDepth {
        <<enumeration>>
        Background
        Midground
        Foreground
    }

    class GenerationResult {
        +string StoryTitle
        +Scene[] Scenes
        +StoryBibleDelta? BibleDelta
        +PosterLayerSet[] PosterSets
        +AudioResult[] AudioResults
        +GenerationMetadata Metadata
    }

    class StoryBibleDelta {
        +BibleCharacter[] NewCharacters
        +BibleCharacter[] UpdatedCharacters
        +BiblePlotPoint[] NewPlotPoints
        +string[] NewWorldRules
    }

    class AudioResult {
        +int SceneIndex
        +byte[] AudioData
        +int DurationMs
    }

    class GenerationMetadata {
        +string TraceId
        +int TotalTokensUsed
        +int TotalDurationMs
        +int Pass1DurationMs
        +int Pass2DurationMs
        +int Pass3DurationMs
        +int Pass4DurationMs
        +int Pass5DurationMs
        +int TtsDurationMs
    }

    class CooldownCheck {
        +bool IsAllowed
        +int RemainingSeconds
        +DateTime? NextAllowedAt
    }

    class TtsVoice {
        +string Id
        +string Name
        +string Language
        +string SampleUrl
    }

    OutlineRequest --> ChildProfileSnapshot : contains
    OutlineRequest --> StoryBible : optionally includes
    OutlineResponse --> CharacterOutline : contains
    ScenePlan --> SceneSpec : contains
    GenerationResult --> Scene : contains
    GenerationResult --> StoryBibleDelta : optionally contains
    GenerationResult --> PosterLayerSet : contains
    GenerationResult --> AudioResult : contains
    GenerationResult --> GenerationMetadata : contains
    PosterLayerSet --> PosterLayer : contains
    PosterLayer --> PosterDepth : uses
    StoryBible --> BibleCharacter : contains
    StoryBible --> BiblePlotPoint : contains
    StoryBibleDelta --> BibleCharacter : may contain
    StoryBibleDelta --> BiblePlotPoint : may contain
```

---

## 4 iDesign Layer Architecture

```mermaid
classDiagram
    class StoryController {
        +GenerateStory(request: OutlineRequest) ActionResult~GenerationResult~
        +GetGenerationStatus(id: string) ActionResult~GenerationStatus~
    }

    class SubscriptionController {
        +GetPlans() ActionResult~SubscriptionPlan[]~
        +GetStatus(softUserId: string) ActionResult~SubscriptionInfo~
        +CreateCheckout(request: CheckoutRequest) ActionResult~CheckoutResponse~
        +ActivateTrial(request: TrialRequest) ActionResult~Subscription~
    }

    class ConfigController {
        +GetTiers() ActionResult~Tier[]~
        +GetFeatureFlag(key: string) ActionResult~string~
        +GetConfig(category: string) ActionResult~Dictionary~
    }

    class WebhookController {
        +HandleStripeWebhook() ActionResult
    }

    class StoryGenerationManager {
        -IStoryGenerationPipeline _pipeline
        -ICooldownService _cooldown
        -ISubscriptionService _subscriptions
        +GenerateAsync(request: OutlineRequest) Task~GenerationResult~
    }

    class SubscriptionManager {
        -ISubscriptionService _subscriptions
        -IStripeWebhookHandler _webhookHandler
        +GetPlansAsync() Task~SubscriptionPlan[]~
        +CreateCheckoutAsync(softUserId: string, planId: Guid) Task~string~
        +HandleWebhookAsync(payload: string, signature: string) Task
    }

    class ConfigManager {
        -IConfigService _config
        +GetTiersAsync() Task~Tier[]~
        +GetFlagAsync(key: string) Task~string?~
    }

    class CoherencePipelineEngine {
        -IOpenRouterService _openRouter
        +ExecutePass1(request: OutlineRequest) Task~OutlineResponse~
        +ExecutePass2(outline: OutlineResponse, bible: StoryBible?) Task~ScenePlan~
        +ExecutePass3(plan: ScenePlan, outline: OutlineResponse) Task~Scene[]~
        +ExecutePass4(scenes: Scene[]) Task~string~
        +ExecutePass5(text: string, profile: ChildProfileSnapshot) Task~GenerationResult~
    }

    StoryController --> StoryGenerationManager : uses
    SubscriptionController --> SubscriptionManager : uses
    ConfigController --> ConfigManager : uses
    WebhookController --> SubscriptionManager : uses

    StoryGenerationManager --> CoherencePipelineEngine : orchestrates
    StoryGenerationManager --> ICooldownService : checks
    StoryGenerationManager --> ISubscriptionService : validates tier

    CoherencePipelineEngine --> IOpenRouterService : calls AI
    CoherencePipelineEngine --> ITtsService : generates audio

    SubscriptionManager --> IStripeWebhookHandler : processes events
    SubscriptionManager --> ISubscriptionService : manages state

    note for StoryController "Controllers (API Layer)"
    note for StoryGenerationManager "Managers (Business Logic)"
    note for CoherencePipelineEngine "Engines (Domain Logic)"
```
