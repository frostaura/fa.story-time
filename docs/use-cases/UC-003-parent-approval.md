# UC-003 — Parent Approval Flow

## Goal
Require approval before full narration playback.

## Actors
Parent

## Preconditions
Story was generated with approval required.

## Main Flow
1. Story generation returns teaser state.
2. Parent approves story.
3. Full audio becomes available.

## Acceptance Criteria
- Approval required defaults to ON.
- Approve endpoint marks full audio as ready.
