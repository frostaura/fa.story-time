# Sequence Summary

## UC-001 Quick Generate

```mermaid
sequenceDiagram
  participant UI as Frontend App
  participant API as StoryTime API
  participant Policy as SubscriptionPolicyService
  participant Gen as StoryGenerationService
  participant Catalog as StoryCatalog

  UI->>API: POST /api/stories/generate
  API->>Policy: Validate duration/cooldown/concurrency
  Policy-->>API: Allowed
  API->>Gen: Generate story + media artifacts
  Gen-->>API: Story payload
  API->>Catalog: Upsert metadata-only entry
  API-->>UI: 200 story response
```

## UC-002 Series Continuation

```mermaid
sequenceDiagram
  participant UI as Frontend App
  participant API as StoryTime API
  participant Gen as StoryGenerationService
  participant Bible as StoryBible State

  UI->>API: POST /api/stories/generate (mode=series, seriesId)
  API->>Gen: Continue series flow
  Gen->>Bible: Load and merge continuity facts
  Bible-->>Gen: Prior arc context
  Gen-->>API: Continuation with recap + updated bible
  API-->>UI: Story with stable seriesId
```

## UC-003 Parent Approval

```mermaid
sequenceDiagram
  participant UI as Frontend App
  participant API as StoryTime API
  participant Gate as Parent Gate

  UI->>API: POST /api/parent/gate/challenge
  API-->>UI: challenge + rpId
  UI->>Gate: Create assertion
  UI->>API: POST /api/parent/gate/verify
  API-->>UI: gateToken
  UI->>API: POST /api/stories/{id}/approve
  API-->>UI: fullAudioReady + fullAudio
```

## UC-004 Kid Shelf

```mermaid
sequenceDiagram
  participant UI as Frontend App
  participant API as StoryTime API
  participant Catalog as StoryCatalog

  UI->>API: GET /api/library/{softUserId}?kidMode=true
  API->>Catalog: Read metadata by soft user
  Catalog-->>API: recent + favorites metadata
  API-->>UI: Kid-safe shelves only
```

## UC-005 Tier and Cooldown

```mermaid
sequenceDiagram
  participant UI as Frontend App
  participant API as StoryTime API
  participant Policy as SubscriptionPolicyService
  participant Checkout as Checkout Provider

  UI->>API: POST /api/stories/generate (long duration)
  API->>Policy: Validate tier duration
  Policy-->>API: Rejected with upgrade path
  API-->>UI: 402 + paywall metadata
  UI->>API: POST /api/subscription/checkout/session
  API->>Checkout: Create checkout session
  Checkout-->>API: sessionId
  UI->>API: POST /api/subscription/checkout/complete
  API-->>UI: upgraded tier
```
