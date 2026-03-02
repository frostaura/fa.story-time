# ARCH-001 — Frontend API Service Layer

## Summary
Introduce a dedicated frontend API service layer to remove inline HTTP orchestration from `App.tsx`.

## Context & Problem
`App.tsx` previously contained direct `fetch` calls for all backend interactions, increasing coupling between view logic and transport concerns and making maintenance harder.

## Decision
Add `src/frontend/src/services/storyTimeApi.ts` with route-aware methods for:
- home status
- library
- generation/favorite/approval
- parent gate register/challenge/verify/settings
- subscription checkout session/complete

`App.tsx` now consumes this service instead of building request URLs and request payload plumbing inline.

## Alternatives Considered
- Keep inline `fetch` calls and only extract utility helpers.
- Introduce a third-party query/state library immediately.

## Consequences
- API interaction behavior is centralized and easier to test/extend.
- Component code stays focused on state transitions and rendering.
- Future migration to a richer data layer can build on this boundary.

## Affected Components
- `src/frontend/src/services/storyTimeApi.ts`
- `src/frontend/src/App.tsx`

## Notes
- This keeps behavior unchanged while improving separation of concerns.
