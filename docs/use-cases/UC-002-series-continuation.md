# UC-002 — Series Continuation

## Goal
Continue an existing series while preserving continuity.

## Actors
Parent, Child

## Preconditions
A prior series episode exists.

## Main Flow
1. User generates first series story.
2. Client stores the returned Story Bible snapshot locally.
3. User selects the prior series explicitly and generates a continuation with `seriesId` plus the latest Story Bible snapshot.
4. System returns recap and coherent continuation plus an updated Story Bible snapshot.

## Acceptance Criteria
- `seriesId` remains stable across continuations.
- Recap begins with prior-context language.
- The continuation request includes the latest Story Bible snapshot from the prior episode.
- The response returns an updated Story Bible snapshot for the next continuation.
- The continuation result preserves recognizable series visual identity so the new poster reads as part of the same series while still introducing fresh layered motion.
- Selecting a continuation path updates the home form with an explicit continuation summary before submit.
- Submitting a continuation shows visible pending feedback and then promotes the newly generated episode to the top of Recent on success.
