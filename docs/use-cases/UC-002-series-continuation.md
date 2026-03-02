# UC-002 — Series Continuation

## Goal
Continue an existing series while preserving continuity.

## Actors
Parent, Child

## Preconditions
A prior series episode exists.

## Main Flow
1. User generates first series story.
2. User generates continuation with `seriesId`.
3. System returns recap and coherent continuation.

## Acceptance Criteria
- `seriesId` remains stable across continuations.
- Recap begins with prior-context language.
