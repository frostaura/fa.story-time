# TaleWeaver — Architecture

> System context, component architecture, data-flow topology, and the 5-pass story coherence pipeline.

---

## 1 System Context Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        INTERNET / CLOUD                             │
│                                                                     │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────────────┐  │
│  │  OpenRouter   │    │    Stripe    │    │       SigNoz         │  │
│  │  (AI Gateway) │    │  (Payments)  │    │   (Observability)    │  │
│  └──────┬───────┘    └──────┬───────┘    └──────────┬───────────┘  │
│         │                   │                       │               │
└─────────┼───────────────────┼───────────────────────┼───────────────┘
          │                   │                       │
          │ HTTPS             │ HTTPS/Webhooks        │ OTLP/gRPC
          │                   │                       │
┌─────────┼───────────────────┼───────────────────────┼───────────────┐
│         ▼                   ▼                       ▲               │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                    .NET 10 Web API                           │   │
│  │                                                             │   │
│  │  ┌─────────────┐  ┌────────────┐  ┌───────────────────┐    │   │
│  │  │  Story API   │  │ Stripe API │  │  Config / Tiers   │    │   │
│  │  │  Controller  │  │ Controller │  │   Controller      │    │   │
│  │  └──────┬──────┘  └─────┬──────┘  └────────┬──────────┘    │   │
│  │         │               │                   │               │   │
│  │  ┌──────▼──────┐  ┌─────▼──────┐  ┌────────▼──────────┐    │   │
│  │  │  Generation  │  │  Webhook   │  │   EF Core +       │    │   │
│  │  │  Pipeline    │  │  Handler   │  │   PostgreSQL      │    │   │
│  │  └──────┬──────┘  └────────────┘  └───────────────────┘    │   │
│  │         │                                                   │   │
│  │  ┌──────▼──────┐                                            │   │
│  │  │  Coqui TTS  │ (local sidecar)                            │   │
│  │  └─────────────┘                                            │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                             ▲                                       │
│                             │ HTTPS (REST + JSON)                   │
│                             │                                       │
│  ┌──────────────────────────┴──────────────────────────────────┐   │
│  │               PWA Client  (React + Vite)                     │   │
│  │                                                             │   │
│  │  ┌──────────┐  ┌──────────┐  ┌────────────┐  ┌──────────┐  │   │
│  │  │  Redux    │  │  Service  │  │ LocalStorage│  │  UI      │  │   │
│  │  │  Store    │  │  Worker   │  │  (all user  │  │  Layer   │  │   │
│  │  │  + RTK Q  │  │  (offline)│  │   data)     │  │          │  │   │
│  │  └──────────┘  └──────────┘  └────────────┘  └──────────┘  │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│                         HOST / DEVICE                                │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 2 Component Diagram

### 2.1 PWA Client (React 18 + TypeScript 5 + Vite)

| Component | Responsibility |
|---|---|
| **UI Layer** | React component tree: pages, shells, modals, story playback |
| **Redux Store (RTK)** | Global state: current profile, generation status, UI flags |
| **RTK Query** | API data-fetching / caching layer for all .NET endpoints |
| **Service Worker** | Offline queue, asset pre-caching, background sync |
| **LocalStorage** | Persistent user data (see §2.2) |

### 2.2 LocalStorage Data Model

All personal and story data stays on the device. Nothing below ever leaves the browser.

| Key | Type | Description |
|---|---|---|
| `softUserId` | `string` | Anonymous device-scoped identifier (UUID v4) |
| `childrenProfiles` | `ChildProfile[]` | Name, age, interests, preferred themes, voice |
| `stories` | `Story[]` | Full story text, scene metadata, generation timestamps |
| `seriesBibles` | `StoryBible[]` | Character sheets, world rules, continuity facts per series |
| `posterLayers` | `PosterLayerSet[]` | Parallax layer images (base64 or blob URLs) |
| `audioCache` | `AudioEntry[]` | TTS audio blobs keyed by story + scene |
| `appSettings` | `AppSettings` | Theme, volume, reduced-motion, parental lock |
| `cooldownState` | `CooldownState` | Last generation timestamp (client-side mirror) |

### 2.3 .NET 10 Web API

Follows **iDesign architecture** (Managers → Engines → Data).

| Layer | Components |
|---|---|
| **Controllers** | `StoryController`, `SubscriptionController`, `ConfigController`, `WebhookController` |
| **Managers** | `StoryGenerationManager`, `SubscriptionManager`, `ConfigManager` |
| **Engines** | `CoherencePipelineEngine`, `OpenRouterEngine`, `TtsEngine`, `CooldownEngine` |
| **Data** | EF Core `DbContext`, Repositories for Tier, SubscriptionPlan, Subscription, FeatureFlag, AppConfig, CooldownState |

### 2.4 PostgreSQL (Server-Side Config Only)

The database stores **zero story content and zero PII**. It holds:

- Tier definitions and feature matrices
- Subscription plans and active subscriptions (keyed by anonymous `SoftUserId`)
- Feature flags and app configuration
- Cooldown state for rate-limiting generation

### 2.5 External Services

| Service | Protocol | Purpose |
|---|---|---|
| **OpenRouter** | HTTPS REST | AI text generation (Claude 3.5 Sonnet) and image generation (Flux) |
| **Coqui TTS** | HTTP (local) | Text-to-speech audio generation, runs as a local sidecar |
| **Stripe** | HTTPS + Webhooks | Subscription billing, checkout sessions, webhook events |
| **SigNoz** | OTLP/gRPC | Distributed tracing, metrics, and log aggregation |

---

## 3 The 5-Pass Coherence Pipeline

Story generation is **not** a single prompt. It follows a deterministic 5-pass pipeline to ensure narrative coherence, age-appropriateness, and illustration consistency.

```
┌─────────┐     ┌────────────┐     ┌─────────────┐     ┌─────────┐     ┌─────────┐
│  Pass 1  │────▶│   Pass 2    │────▶│   Pass 3     │────▶│  Pass 4  │────▶│  Pass 5  │
│ OUTLINE  │     │ SCENE PLAN  │     │ SCENE BATCH  │     │  STITCH  │     │  POLISH  │
└─────────┘     └────────────┘     └─────────────┘     └─────────┘     └─────────┘
```

### Pass 1 — Outline

- **Input**: Child profile (age, interests), optional Story Bible, duration preference.
- **Output**: `OutlineResponse` — title, theme, moral, character list, 3-act structure summary.
- **Model**: Claude 3.5 Sonnet via OpenRouter.
- **Purpose**: Establish the narrative arc before any prose is written.

### Pass 2 — Scene Plan

- **Input**: `OutlineResponse` + Story Bible (if series continuation).
- **Output**: `ScenePlan` — ordered list of `Scene` objects, each with: setting, characters present, key events, emotional beat, illustration prompt seed.
- **Model**: Claude 3.5 Sonnet via OpenRouter.
- **Purpose**: Break the arc into discrete, illustratable scenes with consistent pacing.

### Pass 3 — Scene Batch

- **Input**: `ScenePlan` + `OutlineResponse`.
- **Output**: Array of `Scene` objects with full prose text + final illustration prompts.
- **Model**: Claude 3.5 Sonnet (text), Flux (images) via OpenRouter. Scenes may be generated in parallel batches.
- **Purpose**: Generate the actual story content and illustration prompts per scene.

### Pass 4 — Stitch

- **Input**: All generated `Scene` objects.
- **Output**: Stitched full story text with transition smoothing between scenes.
- **Model**: Claude 3.5 Sonnet via OpenRouter.
- **Purpose**: Eliminate tonal/continuity seams between batch-generated scenes.

### Pass 5 — Polish

- **Input**: Stitched story text + child profile (age).
- **Output**: `GenerationResult` — final story text, reading-level adjusted vocabulary, updated Story Bible delta.
- **Model**: Claude 3.5 Sonnet via OpenRouter.
- **Purpose**: Final readability pass — vocabulary calibration, rhythm, and Story Bible update for series continuity.

### Pipeline Data Flow

```
ChildProfile + StoryBible? + Duration
        │
        ▼
   ┌──────────┐      OutlineResponse
   │  Pass 1   │─────────────┐
   │  Outline  │             │
   └──────────┘             ▼
                      ┌──────────┐      ScenePlan
                      │  Pass 2   │─────────────┐
                      │Scene Plan │             │
                      └──────────┘             ▼
                                         ┌──────────┐      Scene[] + Images
                                         │  Pass 3   │─────────────┐
                                         │Scene Batch│             │
                                         └──────────┘             ▼
                                                            ┌──────────┐      Stitched Text
                                                            │  Pass 4   │─────────────┐
                                                            │  Stitch  │             │
                                                            └──────────┘             ▼
                                                                               ┌──────────┐
                                                                               │  Pass 5   │
                                                                               │  Polish  │
                                                                               └────┬─────┘
                                                                                    │
                                                                                    ▼
                                                                            GenerationResult
                                                                          (story + bible delta
                                                                           + poster layers)
                                                                                    │
                                                                                    ▼
                                                                              ┌──────────┐
                                                                              │ Coqui TTS │
                                                                              └────┬─────┘
                                                                                   │
                                                                                   ▼
                                                                              Audio Blob
```

---

## 4 Deployment Topology

```
┌───────────────────────────────────────────────┐
│               Production Host                  │
│                                                │
│  ┌──────────────┐   ┌──────────────────────┐  │
│  │  Vite Static  │   │  .NET 10 Web API     │  │
│  │  (CDN / Nginx)│   │  (Kestrel, port 5000)│  │
│  │  port 5173    │   │                      │  │
│  └──────────────┘   │  ┌────────────────┐  │  │
│                      │  │ Coqui TTS      │  │  │
│                      │  │ (sidecar, 5002)│  │  │
│                      │  └────────────────┘  │  │
│                      └──────────────────────┘  │
│                                                │
│  ┌──────────────┐   ┌──────────────────────┐  │
│  │  PostgreSQL   │   │  SigNoz (OTEL)       │  │
│  │  port 5432    │   │  port 4317 (gRPC)    │  │
│  └──────────────┘   └──────────────────────┘  │
└───────────────────────────────────────────────┘
```

---

## 5 Cross-Cutting Concerns

### 5.1 Observability

- **OpenTelemetry SDK** integrated into the .NET API.
- Traces span the full pipeline (Pass 1 → TTS).
- Metrics: generation latency, token usage, TTS duration, Stripe webhook processing time.
- Logs: structured JSON, correlated by trace ID.
- All telemetry exported to **SigNoz** via OTLP/gRPC on port 4317.

### 5.2 Security

- No PII on server — the strongest privacy posture possible.
- `SoftUserId` is an opaque UUID; it cannot be reversed to a real identity.
- Parental controls gated by **WebAuthn** (device biometric / PIN).
- HTTPS everywhere; HSTS headers.
- Stripe webhook signature verification on every incoming event.
- CORS locked to the PWA origin.

### 5.3 Offline Support

- Service Worker pre-caches the app shell and critical assets.
- Generation requests made while offline are queued in IndexedDB.
- On reconnection, the Service Worker replays the queue in FIFO order.
- Previously generated stories are fully available offline from LocalStorage.

### 5.4 Rate Limiting / Cooldowns

- Server enforces per-`SoftUserId` cooldowns based on tier.
- `CooldownState` table tracks `LastGenerationAt` per user.
- Client mirrors cooldown state locally for optimistic UI.
