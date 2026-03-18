# TEST-001 — Frontend E2E Critical Flows

## Scope
Define critical frontend E2E flows across the live backend process and browser-rendered Playwright coverage.

## Strategy (unit / integration / e2e)
- Extend frontend E2E (`src/frontend/tests/e2e/quick-generate.e2e.test.tsx`) for cross-boundary behavior, with mocked WebAuthn and relaxed backend assertion requirements called out explicitly.
- Keep unit and backend integration tests unchanged as complementary coverage.
- Use Playwright in two layers:
  - deterministic mocked browser contract checks for UC-001 through UC-005
  - live-backend browser flows for the highest-risk paths, including localhost parent verification with a virtual authenticator
- Keep responsive/mobile confidence explicit with a mobile/tablet/desktop viewport matrix.
- Treat poster richness as part of the visual contract: browser checks should confirm motion-enabled stories look visibly layered and alive, while frontend visual regression coverage keeps reduced-motion states calm and static.

## Prerequisites
- .NET 10 SDK and Node.js 22+ available locally.
- Backend can be launched via `dotnet run --project src/backend/StoryTime.Api`.

## How to Run
From repository root:

```bash
make test
```

Or frontend-only:

```bash
cd src/frontend
npm run test:e2e
```

## Expected Results
- Quick Generate produces story, approval flow unlocks full audio, and favorite toggling persists.
- Premium continuation flow (after entitlement reset via webhook in test) keeps `seriesId` stable across episodes.
- No E2E regression in existing paywall/cooldown expectations, including Trial -> Plus -> Premium progression.
- Browser E2E coverage includes UC-001 through UC-005 Playwright checks, live-backend coverage for generation/paywall/continuation/parent settings/checkout completion, responsive viewport assertions, motion-enabled poster rendering checks, and explicit verification that one-shot/continuation submits surface loading plus success/error feedback instead of silent no-op interactions.

## Regression Notes
- `src/frontend/tests/e2e/quick-generate.e2e.test.tsx` runs against a real backend process to validate route wiring and payload contracts end-to-end, but it is not the strict passkey proof lane.
- Playwright now combines mocked browser contract journeys with live-backend browser paths so browser rendering, layered-poster richness, network wiring, and strict localhost parent-gate proof are all exercised.

## Notes
- Parent gate cryptographic verification remains covered in backend integration/unit tests, while Playwright proves the supported localhost browser ceremony end to end.
- The Vitest live-backend suite should be read as fetch-boundary coverage with mocked WebAuthn, not as a replacement for strict browser passkey validation.
