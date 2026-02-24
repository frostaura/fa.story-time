# TaleWeaver — Sequence Diagrams

> Mermaid sequence diagrams for the primary system flows.

---

## 1 Quick Generate Flow

Full story generation from user tap to playback-ready result.

```mermaid
sequenceDiagram
    actor Parent
    participant PWA as PWA Client
    participant SW as Service Worker
    participant API as .NET Web API
    participant CD as CooldownEngine
    participant Sub as SubscriptionService
    participant Pipeline as CoherencePipeline
    participant OR as OpenRouter
    participant TTS as Coqui TTS
    participant DB as PostgreSQL

    Parent->>PWA: Tap "Generate Story"
    PWA->>PWA: Load ChildProfile + settings from LocalStorage
    PWA->>API: POST /api/stories/generate<br/>{softUserId, profile, duration, storyBibleId: null}

    API->>DB: Get Subscription by SoftUserId
    DB-->>API: Subscription + Tier
    API->>CD: CheckCooldown(softUserId)
    CD->>DB: Get CooldownState
    DB-->>CD: LastGenerationAt + TierId
    CD-->>API: CooldownCheck {isAllowed, remainingSeconds}

    alt Cooldown Active
        API-->>PWA: 429 Too Many Requests {retryAfter}
        PWA->>Parent: Show countdown timer
    else Cooldown Clear
        API->>Sub: ValidateTierEntitlements(duration)
        Sub-->>API: Tier allows requested duration

        alt Duration Not Allowed
            API-->>PWA: 403 Forbidden {upgrade required}
            PWA->>Parent: Show upgrade prompt
        else Duration Allowed
            API->>Pipeline: ExecuteFullPipeline(request)

            Note over Pipeline,OR: Pass 1 — Outline
            Pipeline->>OR: Generate outline<br/>(Claude 3.5 Sonnet)
            OR-->>Pipeline: OutlineResponse

            Note over Pipeline,OR: Pass 2 — Scene Plan
            Pipeline->>OR: Plan scenes from outline<br/>(Claude 3.5 Sonnet)
            OR-->>Pipeline: ScenePlan

            Note over Pipeline,OR: Pass 3 — Scene Batch
            loop For each scene (parallel batches)
                Pipeline->>OR: Generate scene text<br/>(Claude 3.5 Sonnet)
                OR-->>Pipeline: Scene text
                Pipeline->>OR: Generate illustration<br/>(Flux)
                OR-->>Pipeline: Image data
            end

            Note over Pipeline,OR: Pass 4 — Stitch
            Pipeline->>OR: Stitch scenes together<br/>(Claude 3.5 Sonnet)
            OR-->>Pipeline: Stitched story text

            Note over Pipeline,OR: Pass 5 — Polish
            Pipeline->>OR: Polish + vocab calibration<br/>(Claude 3.5 Sonnet)
            OR-->>Pipeline: Final text + StoryBibleDelta

            Note over Pipeline,TTS: TTS Generation
            loop For each scene
                Pipeline->>TTS: Synthesize(sceneText, voiceId)
                TTS-->>Pipeline: Audio blob
            end

            Pipeline-->>API: GenerationResult

            API->>CD: RecordGeneration(softUserId)
            CD->>DB: Upsert CooldownState
            API-->>PWA: 200 OK {GenerationResult}

            PWA->>PWA: Store story in LocalStorage
            PWA->>PWA: Store poster layers in LocalStorage
            PWA->>PWA: Store audio in LocalStorage
            PWA->>Parent: Open StoryModal (playback)
        end
    end
```

---

## 2 Series Continuation Flow

Generating a new episode using an existing Story Bible for continuity.

```mermaid
sequenceDiagram
    actor Parent
    participant PWA as PWA Client
    participant API as .NET Web API
    participant Pipeline as CoherencePipeline
    participant OR as OpenRouter
    participant TTS as Coqui TTS

    Parent->>PWA: Tap "Continue Series" on StoryCard
    PWA->>PWA: Load StoryBible from LocalStorage
    PWA->>PWA: Load ChildProfile from LocalStorage
    PWA->>Parent: Show QuickGenerateCard<br/>(pre-populated with series context)
    Parent->>PWA: Adjust duration, tap "Generate Next Episode"

    PWA->>API: POST /api/stories/generate<br/>{softUserId, profile, duration,<br/>storyBibleSnapshot: full bible}

    Note right of API: Cooldown + tier validation<br/>(same as Quick Generate)

    API->>Pipeline: ExecuteFullPipeline(request)

    Note over Pipeline,OR: Pass 1 — Outline (with Bible context)
    Pipeline->>OR: Generate outline<br/>including Story Bible characters,<br/>world rules, and plot history
    OR-->>Pipeline: OutlineResponse<br/>(respects continuity)

    Note over Pipeline,OR: Pass 2 — Scene Plan (with Bible context)
    Pipeline->>OR: Plan scenes with<br/>existing character arcs
    OR-->>Pipeline: ScenePlan

    Note over Pipeline,OR: Pass 3 — Scene Batch
    loop For each scene (parallel batches)
        Pipeline->>OR: Generate scene text + images
        OR-->>Pipeline: Scene text + image data
    end

    Note over Pipeline,OR: Pass 4 — Stitch
    Pipeline->>OR: Stitch with continuity awareness
    OR-->>Pipeline: Stitched text

    Note over Pipeline,OR: Pass 5 — Polish (generates Bible delta)
    Pipeline->>OR: Polish + extract new<br/>characters, plot points, world rules
    OR-->>Pipeline: Final text + StoryBibleDelta

    Note over Pipeline,TTS: TTS Generation
    Pipeline->>TTS: Synthesize all scenes
    TTS-->>Pipeline: Audio blobs

    Pipeline-->>API: GenerationResult
    API-->>PWA: 200 OK {GenerationResult}

    PWA->>PWA: Merge StoryBibleDelta into<br/>existing StoryBible (LocalStorage)
    PWA->>PWA: Append new episode to stories[]
    PWA->>PWA: Store poster layers + audio
    PWA->>Parent: Open StoryModal (playback)
```

---

## 3 Stripe Subscription Flow

Checkout initiation, payment, and webhook-driven state synchronization.

```mermaid
sequenceDiagram
    actor Parent
    participant PWA as PWA Client
    participant API as .NET Web API
    participant SubMgr as SubscriptionManager
    participant DB as PostgreSQL
    participant Stripe as Stripe

    Note over Parent,Stripe: Checkout Flow

    Parent->>PWA: Open Parental Settings → Subscription
    PWA->>API: GET /api/subscriptions/plans
    API->>DB: Query SubscriptionPlans + Tiers
    DB-->>API: Plans with tier details
    API-->>PWA: Plan list with pricing

    Parent->>PWA: Select "Plus Monthly" → Tap "Subscribe"
    PWA->>API: POST /api/subscriptions/checkout<br/>{softUserId, planId}
    API->>Stripe: Create Checkout Session<br/>{priceId, successUrl, cancelUrl}
    Stripe-->>API: Session {id, url}
    API-->>PWA: {checkoutUrl}
    PWA->>Stripe: Redirect to Stripe Checkout

    Parent->>Stripe: Complete payment
    Stripe->>PWA: Redirect to success URL

    PWA->>API: GET /api/subscriptions/status<br/>?softUserId=...
    API->>DB: Query Subscription
    DB-->>API: Subscription (may still be pending)
    API-->>PWA: Subscription status

    Note over Parent,Stripe: Webhook Flow (async)

    Stripe->>API: POST /api/webhooks/stripe<br/>(checkout.session.completed)
    API->>SubMgr: ValidateSignature(payload, sig)
    SubMgr-->>API: Signature valid

    API->>SubMgr: HandleCheckoutCompleted(session)
    SubMgr->>DB: Create Subscription<br/>{softUserId, planId, stripeSubId,<br/>status: active, periodStart/End}
    DB-->>SubMgr: Subscription created
    SubMgr-->>API: OK
    API-->>Stripe: 200 OK

    Note over Parent,Stripe: Recurring Payment

    Stripe->>API: POST /api/webhooks/stripe<br/>(invoice.paid)
    API->>SubMgr: HandleInvoicePaid(invoice)
    SubMgr->>DB: Update Subscription<br/>{currentPeriodStart, currentPeriodEnd}
    API-->>Stripe: 200 OK

    Note over Parent,Stripe: Payment Failure

    Stripe->>API: POST /api/webhooks/stripe<br/>(invoice.payment_failed)
    API->>SubMgr: HandlePaymentFailed(invoice)
    SubMgr->>DB: Update Subscription<br/>{status: past_due}
    API-->>Stripe: 200 OK

    PWA->>API: GET /api/subscriptions/status
    API-->>PWA: {status: past_due}
    PWA->>Parent: Show "Update payment" banner

    Note over Parent,Stripe: Cancellation

    Stripe->>API: POST /api/webhooks/stripe<br/>(customer.subscription.deleted)
    API->>SubMgr: HandleSubscriptionDeleted(sub)
    SubMgr->>DB: Update Subscription<br/>{status: canceled}
    API-->>Stripe: 200 OK
```

---

## 4 Offline Queue Flow

Service Worker capture, storage, replay, and notification when connectivity returns.

```mermaid
sequenceDiagram
    actor Parent
    participant PWA as PWA Client
    participant SW as Service Worker
    participant IDB as IndexedDB
    participant API as .NET Web API
    participant Pipeline as CoherencePipeline

    Note over Parent,Pipeline: Offline — Request Capture

    Parent->>PWA: Tap "Generate Story" (offline)
    PWA->>SW: POST /api/stories/generate<br/>(intercepted by SW)
    SW->>SW: Detect: navigator.onLine === false
    SW->>IDB: Store request payload<br/>{id, payload, status: "queued",<br/>timestamp, retryCount: 0}
    SW-->>PWA: Return synthetic response<br/>{status: "queued", queueId}

    PWA->>Parent: Show NotificationToast<br/>"Story queued — will generate<br/>when you're back online"
    PWA->>PWA: Show "Queued" badge<br/>on QuickGenerateCard

    Note over Parent,Pipeline: Connectivity Restored

    SW->>SW: Detect: online event +<br/>fetch probe succeeds
    SW->>IDB: Query queued requests<br/>(ORDER BY timestamp ASC)
    IDB-->>SW: Queued request list

    loop For each queued request (FIFO)
        SW->>API: POST /api/stories/generate<br/>(replay original payload)

        alt API Success
            API->>Pipeline: ExecuteFullPipeline
            Pipeline-->>API: GenerationResult
            API-->>SW: 200 OK {GenerationResult}

            SW->>IDB: Update request<br/>{status: "completed"}
            SW->>PWA: Store story in LocalStorage<br/>(via postMessage)
            SW->>Parent: Push notification<br/>"Your story is ready! 📖"
        else API Failure
            API-->>SW: Error response
            SW->>IDB: Update request<br/>{retryCount++}

            alt retryCount < 3
                SW->>SW: Schedule retry<br/>(exponential backoff)
            else retryCount >= 3
                SW->>IDB: Update request<br/>{status: "failed"}
                SW->>Parent: Push notification<br/>"Story generation failed.<br/>Please try again."
            end
        end
    end

    PWA->>PWA: Refresh StoryLibrary
    PWA->>PWA: Remove "Queued" badge
    PWA->>Parent: New stories visible in library
```

---

## 5 Onboarding Flow

First-launch experience from app open to trial activation.

```mermaid
sequenceDiagram
    actor Parent
    participant PWA as PWA Client
    participant LS as LocalStorage
    participant API as .NET Web API
    participant DB as PostgreSQL

    Parent->>PWA: Launch app (first time)
    PWA->>LS: Check for tw_softUserId
    LS-->>PWA: null (not found)

    PWA->>PWA: Generate UUID v4
    PWA->>LS: Store tw_softUserId

    PWA->>Parent: Show OnboardingWizard

    Note over Parent,PWA: Step 1 — Welcome
    Parent->>PWA: Tap "Get Started"

    Note over Parent,PWA: Step 2 — Child Profile
    Parent->>PWA: Enter name, age range,<br/>select interests & themes
    PWA->>LS: Store ChildProfile in<br/>tw_childrenProfiles[]

    Note over Parent,PWA: Step 3 — Voice Selection
    PWA->>API: GET /api/tts/voices
    API-->>PWA: Available TTS voices
    Parent->>PWA: Preview voices, select one
    PWA->>LS: Update ChildProfile.voiceId

    Note over Parent,PWA: Step 4 — Trial Activation
    Parent->>PWA: Tap "Start Free Trial"
    PWA->>API: POST /api/subscriptions/trial<br/>{softUserId}
    API->>DB: Create Subscription<br/>{status: trialing, trialEnd: +14 days}
    DB-->>API: Subscription created
    API-->>PWA: {subscription, trialEnd}

    PWA->>LS: Store trial status in<br/>tw_appSettings
    PWA->>Parent: Navigate to Home screen<br/>with QuickGenerateCard ready
```
