# StoryTime System Specification (Canonical)

## 1. Product Goal
StoryTime is a calm-first, mobile-first PWA that lets families generate and play personalized bedtime stories with minimal navigation and safe defaults.

## 2. Core Product Principles
- Calm bedtime routine first, learning second, entertainment third.
- Quick Generate is the default and visible on home with an always-on duration slider.
- Child-safe by default with parent-gated settings and checkout.
- Privacy first: no story content or child PII persisted server-side.
- Config-driven limits, entitlements, and feature flags.

## 3. Functional Features
### 3.1 Story Generation
- **Quick Generate**: one-tap generation from home.
- **Series mode**: persistent Story Bible, continuity-safe continuations.
- **One-shot mode**: standalone stories with full customization.
- **Multi-pass AI flow**: outline -> scene plan -> scene batch -> stitch -> polish.
- **LLM provider**: All remote LLM requests must route through OpenRouter; deterministic non-LLM generation is allowed when AI orchestration is disabled.

### 3.2 Playback and Media
- Teaser narration before approval when approval mode is on.
- Full narration unlocked after approval.
- Poster output has 3-5 parallax layers with defined speed multipliers.
- Reduced-motion support via static/parallax-safe fallback.

### 3.3 Parent and Child Safety
- Parent settings are protected by strong gating (WebAuthn in full product scope).
- Kid Shelf only exposes Recent and Favorites.
- Notification toggles and analytics toggle are parent-controlled.

### 3.4 Subscription and Limits
- Tiering with concurrency and cooldown limits.
- Trial/Plus supports short-medium lengths.
- Premium unlocks long stories and higher limits.
- Subscription webhook updates entitlement state.

## 4. Story Bible Contract
Series continuations use a Story Bible with:
- `seriesId`
- visual identity tokens
- recurring characters and continuity facts
- arc state and previous episode summary
- anchored audio/music metadata

## 5. Poster and Parallax Contract
- 3-5 layers per poster (`BACKGROUND`, `MIDGROUND_*`, `FOREGROUND`, `PARTICLES`).
- Target role speed multipliers: 0.2x / 0.5x / 1.0x / 1.3x.
- If image model fails, procedural fallback generates layered SVG assets in <= 200ms.

## 6. Privacy and Data Handling
- No server-side persistence of story text, child names, posters, or narration payloads.
- Client stores profiles and story artifacts in LocalStorage.
- Backend logs must avoid PII and raw prompts.

## 7. Acceptance Checklist
1. Home shows Quick Generate and duration slider by default.
2. Series mode continues coherently over repeated continuations.
3. Poster parallax includes 3-5 layers and reduced-motion compatibility.
4. Procedural poster fallback completes quickly and preserves layer model.
5. Approval is enabled by default with teaser-first playback.
6. Kid Shelf limits browsing to Recent and Favorites.
7. Subscription webhooks enforce tier/cooldown/concurrency limits.
8. Server-side storage/logging excludes story content and PII.
