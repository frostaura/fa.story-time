# StoryTime System Specification (Canonical)

## 1. Product Goal
StoryTime is a calm-first, mobile-first PWA that lets families generate and play personalized bedtime stories with minimal navigation and safe defaults.

## 2. Core Product Principles
- Calm bedtime routine first, learning second, entertainment third.
- Visual delight comes from softly magical, motion-aware poster depth rather than loud or busy effects.
- Quick Generate is the default and visible on home with an always-on duration slider.
- Child-safe by default with parent-gated settings and checkout.
- Privacy first: no story content or child PII persisted server-side.
- Config-driven limits, entitlements, and feature flags.

## 3. Functional Features
### 3.1 Story Generation
- **Quick Generate**: one-tap generation from home.
- **Series mode**: the backend returns a Story Bible snapshot after each series episode and the client sends that snapshot back for continuity-safe continuations.
- **One-shot mode**: standalone stories with full customization.
- **Multi-pass AI flow**: outline -> scene plan -> scene batch -> stitch -> polish.
- **LLM provider**: When AI orchestration is enabled, remote stage requests must route through the OpenRouter chat-completions contract; the shipped default keeps AI orchestration disabled and falls back to deterministic non-LLM generation until provider credentials and approval are supplied.

### 3.2 Playback and Media
- Teaser narration before approval when approval mode is on.
- Full narration unlocked after approval.
- Poster output has 3-5 parallax layers with defined speed multipliers and a visibly layered composition in default motion mode.
- Motion-enabled poster surfaces use perceptible depth and ambient drift so generated stories do not read as flat static cards.
- Reduced-motion support uses a static/parallax-safe fallback that keeps posters readable without continuous motion.

### 3.3 Parent and Child Safety
- Parent settings are protected by strong gating, with strict backend WebAuthn assertion validation and localhost browser proof in supported Playwright/manual QA environments.
- Kid Shelf only exposes Recent and Favorites when the parent-managed Kid Shelf setting is enabled.
- Notification and analytics toggles are parent-controlled preference/consent flags. This build stores those flags but does not ship push delivery or an external analytics pipeline by default.

### 3.4 Subscription and Limits
- Tiering with concurrency and cooldown limits.
- Trial supports up to 10-minute stories with a single active generation and a 30-minute cooldown.
- Plus supports up to 12-minute stories with two active generations and a 10-minute cooldown.
- Premium unlocks 15-minute stories, three active generations, and a 5-minute cooldown.
- Duration-based paywalls recommend the lowest higher tier that can satisfy the requested story length, while default checkout progression follows `Trial -> Plus -> Premium`.
- Subscription webhook updates entitlement state when the shared secret header is present.
- Checkout supports the built-in local return flow and an external provider HTTP contract. The external contract is validated by automated contract tests; live provider certification remains an out-of-repo proof activity when credentials/endpoints are available.

## 4. Story Bible Contract
Series continuations use a client-owned Story Bible snapshot with:
- `seriesId`
- visual identity tokens
- recurring characters and continuity facts
- arc state and previous episode summary
- anchored audio/music metadata
- The client stores the latest snapshot locally and posts it back on continuation requests.

## 5. Poster and Parallax Contract
- 3-5 layers per poster (`BACKGROUND`, `MIDGROUND_*`, `FOREGROUND`, `PARTICLES`).
- Target role speed multipliers: 0.2x / 0.5x / 1.0x / 1.3x.
- Default motion-enabled rendering must expose clear depth separation between background, midground/foreground, and particle treatment so the poster feels softly alive rather than like a single flat image.
- Generated and continued stories should preserve recognizable visual identity tokens while still allowing per-story variation.
- If image model fails, procedural fallback generates layered SVG assets in <= 200ms without collapsing to a flat-looking card.

## 6. Privacy and Data Handling
- No server-side persistence of story text, child names, posters, or narration payloads.
- The shipped default catalog provider is `InMemory`; server-side persistence is limited to pseudonymous entitlement/parent-setting JSON plus WebAuthn public-key metadata, written via restricted state files rather than narrative payload storage.
- `FileSystemStoryCatalog` remains available as an opt-in metadata-only provider for local/deployment scenarios that intentionally persist library metadata without narrative payloads.
- Client stores profiles, story artifacts, Story Bible snapshots, and pending checkout state in LocalStorage.
- Backend logs must avoid PII and raw prompts.

## 7. Acceptance Checklist
1. Home shows Quick Generate and duration slider by default.
2. Series mode continues coherently over repeated continuations.
3. Poster surfaces feel visibly layered and softly alive in motion-enabled mode, with 3-5 layers and reduced-motion compatibility.
4. Procedural poster fallback completes quickly, preserves the layer model, and avoids a flat poster presentation.
5. Approval is enabled by default with teaser-first playback.
6. Kid Shelf limits browsing to Recent and Favorites.
7. Subscription webhooks and checkout flows enforce documented Trial/Plus/Premium duration, cooldown, and concurrency limits, with automated proof for the local return flow and contract proof for external-provider wiring.
8. Server-side storage/logging excludes story content and child PII while limiting persisted state to pseudonymous settings/entitlement metadata.
