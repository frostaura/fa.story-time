# TEST-001 — Frontend E2E Critical Flows

## Scope
Define critical frontend E2E flows executed against the live backend process.

## Strategy (unit / integration / e2e)
- Extend frontend E2E (`src/frontend/tests/e2e/quick-generate.e2e.test.tsx`) for cross-boundary behavior.
- Keep unit and backend integration tests unchanged as complementary coverage.

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
- No E2E regression in existing paywall/cooldown expectations.
- Browser E2E coverage includes UC-001 through UC-005 Playwright checks.

## Regression Notes
- Tests intentionally run against a real backend process to validate route wiring and payload contracts end-to-end.

## Notes
- Parent gate cryptographic verification remains covered in backend integration/unit tests.
