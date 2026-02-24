# TaleWeaver — Use Cases

> Actors, preconditions, flows, and postconditions for all primary use cases.

---

## Actors

| Actor | Description |
|---|---|
| **Parent** | Adult user who manages profiles, settings, and subscriptions |
| **Child** | Young user in Kid Mode with restricted access |
| **System** | The TaleWeaver backend (API + pipeline + TTS) |
| **Stripe** | External payment processor |
| **Service Worker** | Client-side offline/background sync agent |

---

## UC-1: Quick Generate

**One-tap story generation from the home screen.**

### Summary

The parent taps a single button to generate a new bedtime story using the active child profile's preferences. The system runs the 5-pass coherence pipeline, generates illustrations and audio, then presents the completed story.

### Actors

- Primary: Parent
- Supporting: System

### Preconditions

1. At least one child profile exists in LocalStorage.
2. An active child profile is selected.
3. The user's cooldown period has elapsed.
4. The device is online (or offline queue is available — see UC-8).

### Main Flow

1. Parent opens the app; home screen shows the **QuickGenerateCard**.
2. Parent optionally adjusts the **DurationSlider** (short / medium / long, per tier).
3. Parent taps **"Generate Story"**.
4. Client sends `POST /api/stories/generate` with:
   - `softUserId`
   - `childProfileSnapshot` (age, interests, themes — sent as ephemeral payload, not stored server-side)
   - `duration`
   - `storyBibleId` (null for standalone)
5. System validates cooldown and tier entitlements.
6. System executes the 5-pass coherence pipeline:
   - Pass 1: Outline
   - Pass 2: Scene Plan
   - Pass 3: Scene Batch (text + image prompts → OpenRouter)
   - Pass 4: Stitch
   - Pass 5: Polish
7. System generates TTS audio via Coqui for each scene.
8. System returns `GenerationResult` (story text, scene images, audio URLs, Story Bible delta).
9. Client stores the story, poster layers, and audio in LocalStorage.
10. Client navigates to the **StoryModal** for playback.

### Postconditions

- A new story is saved in LocalStorage under `stories[]`.
- Cooldown timer is reset (server + client).
- If `storyBibleId` was provided, the Story Bible is updated with the delta.

### Alternative Flows

- **AF-1 (Cooldown active)**: Step 5 returns `429 Too Many Requests` with `retryAfter` seconds. Client shows a countdown on the QuickGenerateCard.
- **AF-2 (Tier limit)**: Step 5 returns `403 Forbidden` if duration exceeds tier's `AllowedLengths`. Client prompts upgrade.
- **AF-3 (Offline)**: Step 4 fails; request is queued by Service Worker (see UC-8).

---

## UC-2: Series Continuation

**Continue an existing story series with Story Bible context.**

### Summary

The parent selects an existing series and generates a continuation episode. The system uses the stored Story Bible to maintain character, setting, and plot continuity.

### Actors

- Primary: Parent
- Supporting: System

### Preconditions

1. At least one story with an associated Story Bible exists in LocalStorage.
2. The user's cooldown period has elapsed.
3. The device is online.

### Main Flow

1. Parent opens the **StoryLibrary** and selects a story tagged as part of a series.
2. Parent taps **"Continue Series"** on the StoryCard.
3. Client loads the associated `StoryBible` from LocalStorage.
4. Client presents the **QuickGenerateCard** pre-populated with series context.
5. Parent optionally adjusts duration and taps **"Generate Next Episode"**.
6. Client sends `POST /api/stories/generate` with:
   - `softUserId`
   - `childProfileSnapshot`
   - `duration`
   - `storyBibleSnapshot` (full Story Bible sent as ephemeral payload)
7. System runs the 5-pass pipeline with Story Bible context threaded through Passes 1, 2, and 5.
8. System returns `GenerationResult` including a Story Bible delta.
9. Client merges the delta into the existing Story Bible in LocalStorage.
10. Client stores the new episode and navigates to the StoryModal.

### Postconditions

- A new episode is appended to the series in `stories[]`.
- The Story Bible in `seriesBibles[]` is updated with new characters, events, and continuity facts.
- Cooldown timer is reset.

### Alternative Flows

- **AF-1 (Story Bible too large)**: If the serialized Story Bible exceeds the context window limit, the client truncates older entries and warns the parent.

---

## UC-3: Onboarding

**First-run experience: profile creation, voice selection, and trial activation.**

### Summary

On first launch, the app walks the parent through creating a child profile, selecting a TTS voice, and activating their free trial subscription.

### Actors

- Primary: Parent
- Supporting: System, Stripe

### Preconditions

1. The app has never been launched on this device (no `softUserId` in LocalStorage).

### Main Flow

1. App detects no `softUserId`; generates a new UUID v4 and stores it.
2. App presents the **OnboardingWizard** (step-based modal):
   - **Step 1 — Welcome**: Brief intro and value proposition.
   - **Step 2 — Child Profile**: Name, age range, interests (multi-select), preferred themes.
   - **Step 3 — Voice Selection**: Audio preview of available Coqui TTS voices; parent selects one.
   - **Step 4 — Trial Activation**: Explain the Trial tier; parent taps "Start Free Trial."
3. Client sends `POST /api/subscriptions/trial` with `softUserId`.
4. System creates a `Subscription` record with `Status = trialing`, linked to the Trial tier's `SubscriptionPlan`.
5. System returns subscription confirmation with `TrialEnd` date.
6. Client stores the child profile, voice preference, and trial status in LocalStorage.
7. App navigates to the home screen with the QuickGenerateCard ready.

### Postconditions

- `softUserId` exists in LocalStorage.
- One child profile exists in `childrenProfiles[]`.
- A Trial subscription is active server-side.
- `appSettings` contains the selected voice.

### Alternative Flows

- **AF-1 (Skip trial)**: Parent can skip Step 4. The app operates in a limited free mode until trial is activated.
- **AF-2 (Add more profiles)**: After onboarding, parent can add additional child profiles from Parental Settings (UC-5).

---

## UC-4: Story Playback

**Full story presentation with text, audio, and parallax poster art.**

### Summary

The user opens a completed story and experiences synchronized text display, TTS narration, and parallax-animated poster illustrations.

### Actors

- Primary: Parent or Child
- Supporting: (none — fully client-side)

### Preconditions

1. A generated story exists in LocalStorage with at least text content.
2. Audio and poster layers are available (or gracefully degraded).

### Main Flow

1. User taps a **StoryCard** in the StoryLibrary or is auto-navigated after generation.
2. Client opens the **StoryModal** full-screen.
3. Client loads story text, audio blobs, and poster layers from LocalStorage.
4. Playback begins:
   - Text is displayed scene-by-scene with smooth scroll transitions.
   - TTS audio plays synchronized to the current scene.
   - **ParallaxPoster** renders layered illustrations with depth effect on device tilt / scroll.
5. User can:
   - Pause / resume narration.
   - Skip forward / back between scenes.
   - Adjust volume.
   - Toggle text visibility (audio-only mode).
6. On story completion, client shows:
   - "Continue Series" button (if Story Bible exists).
   - "Generate New Story" button.
   - Star rating (stored locally for personal use).

### Postconditions

- Story is marked as "read" in LocalStorage.
- Optional star rating is persisted locally.

### Alternative Flows

- **AF-1 (No audio)**: If TTS audio is unavailable, text-only mode is used with a visual indicator.
- **AF-2 (No poster)**: If poster layers are missing, a default gradient background is shown.
- **AF-3 (Reduced motion)**: If `appSettings.reducedMotion` is true, parallax effects are disabled; static poster is shown.

---

## UC-5: Parental Controls

**WebAuthn-gated access to app settings and administrative features.**

### Summary

Settings that affect content, subscriptions, or child profiles are locked behind device biometric or PIN authentication via WebAuthn.

### Actors

- Primary: Parent
- Supporting: (none — client-side WebAuthn)

### Preconditions

1. The device supports WebAuthn (biometric, PIN, or security key).
2. At least one child profile exists.

### Main Flow

1. Parent taps the **Settings** icon (gear) in the app header.
2. Client triggers a **WebAuthn challenge** (fingerprint, Face ID, or device PIN).
3. Upon successful authentication, client opens **ParentalSettings**:
   - **Profiles**: Add / edit / delete child profiles.
   - **Subscription**: View current tier, manage billing (links to Stripe portal).
   - **Content**: Set age filters, blocked themes, max story length.
   - **Voices**: Change default TTS voice per profile.
   - **Kid Mode**: Enable / disable Kid Mode (UC-6).
   - **Data**: Export stories (JSON), clear all data.
4. Parent makes desired changes.
5. Changes are saved to LocalStorage (profiles, settings) or propagated to the API (subscription changes).

### Postconditions

- Settings changes are persisted.
- WebAuthn session expires after navigation away or after a configurable timeout (default: 5 minutes).

### Alternative Flows

- **AF-1 (WebAuthn unavailable)**: If the device lacks WebAuthn support, a fallback 4-digit PIN is used (stored hashed in LocalStorage).
- **AF-2 (Auth failure)**: Three consecutive failures show a cooldown timer (30 seconds).

---

## UC-6: Kid Mode

**Restricted browsing mode for unsupervised child use.**

### Summary

When Kid Mode is enabled, the app restricts the interface to story browsing and playback only. Generation, settings, and external links are hidden or disabled.

### Actors

- Primary: Child
- Supporting: Parent (enables/disables via UC-5)

### Preconditions

1. Kid Mode has been enabled by the parent via Parental Settings.

### Main Flow

1. App launches in Kid Mode (or parent toggles it on).
2. The interface changes:
   - **KidShelf** replaces the standard StoryLibrary — large, colorful tiles with character art.
   - Navigation is limited to: KidShelf → StoryModal (playback).
   - No settings icon, no generation controls, no external links.
   - The "Exit Kid Mode" button is hidden behind a **WebAuthn challenge**.
3. Child browses the KidShelf and taps a story to play.
4. Playback proceeds as UC-4 but with simplified controls (play/pause only).

### Postconditions

- The child can only access previously generated stories.
- No new stories are generated.
- No settings are accessible.

### Alternative Flows

- **AF-1 (Exit Kid Mode)**: Parent taps a hidden gesture area (e.g., long-press on a corner) → WebAuthn challenge → Kid Mode disabled.
- **AF-2 (Empty shelf)**: If no stories exist, KidShelf shows a friendly message: "Ask a grown-up to make you a story!"

---

## UC-7: Subscription Management

**Stripe-powered checkout, billing management, and webhook processing.**

### Summary

Parents can upgrade, downgrade, or cancel their subscription via Stripe Checkout. The server processes Stripe webhooks to keep subscription state in sync.

### Actors

- Primary: Parent
- Supporting: System, Stripe

### Preconditions

1. The parent has a `softUserId` and is authenticated to Parental Settings (UC-5).

### Main Flow — Checkout

1. Parent navigates to Subscription settings within Parental Settings.
2. Client displays available plans with pricing (fetched from `GET /api/subscriptions/plans`).
3. Parent selects a plan and taps **"Subscribe"**.
4. Client sends `POST /api/subscriptions/checkout` with `softUserId` and `planId`.
5. System creates a Stripe Checkout Session and returns the session URL.
6. Client redirects to the Stripe Checkout page.
7. Parent completes payment on Stripe.
8. Stripe redirects back to the app with a success token.
9. Client confirms subscription via `GET /api/subscriptions/status?softUserId=...`.

### Main Flow — Webhook Processing

1. Stripe sends a webhook event to `POST /api/webhooks/stripe`.
2. System verifies the webhook signature.
3. System processes the event:
   - `checkout.session.completed` → Create/update `Subscription` record.
   - `invoice.paid` → Update `CurrentPeriodStart/End`.
   - `invoice.payment_failed` → Set `Status = past_due`.
   - `customer.subscription.deleted` → Set `Status = canceled`.
   - `customer.subscription.updated` → Update plan, status, period dates.
4. System returns `200 OK` to Stripe.

### Postconditions

- `Subscription` record reflects the current Stripe state.
- Client's next API call picks up the new tier entitlements.

### Alternative Flows

- **AF-1 (Payment failure)**: Webhook sets status to `past_due`. Client shows a banner prompting payment update.
- **AF-2 (Cancellation)**: Parent cancels via Stripe portal. Webhook sets status to `canceled`. Access reverts to Trial tier at period end.
- **AF-3 (Plan change)**: Upgrading mid-cycle is prorated by Stripe. Downgrading takes effect at next period.

---

## UC-8: Offline Queue

**Service Worker queuing for generation requests made while offline.**

### Summary

When the device loses connectivity, generation requests are captured by the Service Worker and replayed automatically when connectivity is restored.

### Actors

- Primary: Parent
- Supporting: Service Worker, System

### Preconditions

1. The PWA is installed with an active Service Worker.
2. The device is offline or the API is unreachable.

### Main Flow

1. Parent taps "Generate Story" while offline.
2. The Service Worker intercepts the failed `POST /api/stories/generate` request.
3. Service Worker stores the request payload in **IndexedDB** with a `queued` status and timestamp.
4. Client displays a **NotificationToast**: "You're offline. Your story will be generated when you're back online."
5. Client shows a "Queued" badge on the QuickGenerateCard.
6. Device regains connectivity (detected via `navigator.onLine` + periodic fetch probe).
7. Service Worker replays queued requests in **FIFO order**.
8. For each successful response:
   - Service Worker stores the story in LocalStorage.
   - Service Worker triggers a push notification (if permitted): "Your story is ready!"
   - Queue entry is marked `completed`.
9. Client refreshes the StoryLibrary to show the new story.

### Postconditions

- All queued requests are processed.
- Stories are available in LocalStorage.
- Queue is cleared.

### Alternative Flows

- **AF-1 (Partial connectivity)**: If replay fails (e.g., API error), the request remains queued with a retry counter. After 3 failures, it is marked `failed` and the user is notified.
- **AF-2 (Multiple queued)**: Multiple generation requests are replayed sequentially to respect cooldown timers.
- **AF-3 (App closed)**: The Service Worker continues processing the queue even if the app tab is closed (background sync API).

---

## Use Case Dependency Map

```
UC-3 (Onboarding)
  └──▶ UC-1 (Quick Generate)
         ├──▶ UC-4 (Story Playback)
         └──▶ UC-2 (Series Continuation)
                └──▶ UC-4 (Story Playback)

UC-5 (Parental Controls)
  ├──▶ UC-6 (Kid Mode)
  └──▶ UC-7 (Subscription Management)

UC-8 (Offline Queue) ──supports──▶ UC-1, UC-2
```
