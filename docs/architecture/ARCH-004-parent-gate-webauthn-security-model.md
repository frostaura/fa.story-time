# ARCH-004 — Parent Gate WebAuthn Security Model

## Summary
Use a challenge/verify gate flow backed by WebAuthn assertions before allowing parent-only actions.

## Context & Problem
Parent settings and subscription upgrades require strong parental control. A weak token-only model would not provide replay resistance or origin-bound verification required for reliable parent gating.

## Decision
- Parent gate flow is implemented as:
  1. Register parent credential (`/api/parent/{softUserId}/gate/register`)
  2. Create challenge (`/api/parent/{softUserId}/gate/challenge`)
  3. Verify assertion (`/api/parent/{softUserId}/gate/verify`)
  4. Use gate token for protected settings and upgrade endpoints
- Enforce configurable validation controls in `StoryTime:ParentGate`:
  - challenge/session TTL
  - relying-party and origin controls
  - credential registration requirements
  - assertion and challenge-binding requirements
- Keep protected updates server-authorized by gate token checks in `ParentSettingsService`.

## Alternatives Considered
- PIN-only parent check.
- Time-based token checks without WebAuthn challenge binding.

## Consequences
- Parent-only actions gain replay-resistant and origin-aware verification.
- Integration and unit tests can directly validate mismatch/replay rejection cases.
- Frontend must implement passkey UX and assertion transport handling.

## Affected Components
- `src/backend/StoryTime.Api/Services/ParentSettingsService.cs`
- `src/backend/StoryTime.Api/Program.cs`
- `src/frontend/src/App.tsx`
- `src/frontend/tests/e2e/quick-generate.e2e.test.tsx`

## Notes
- This design aligns with `docs/use-cases/UC-003-parent-approval.md` and keeps gate checks explicit at backend boundaries.
