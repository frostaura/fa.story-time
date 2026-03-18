# UC-003 — Parent Approval Flow

## Goal
Require approval before full narration playback.

## Actors
Parent

## Preconditions
Story was generated with approval required.

## Main Flow
1. Story generation returns teaser state.
2. Parent first completes parent verification and receives a valid gate token.
3. Parent approves story with that verified gate token.
4. Full audio becomes available.

## Variants / Edge Cases
- If approval persistence needs a later retry, the current device still promotes the generated full narration into the single visible playback control after approval.
- Unsupported hosts still surface localhost recovery guidance before any parent-passkey prompt is attempted.
- Parent verification must fail fast when the browser ceremony or follow-up requests stall, and the recovery guidance must stay calm on mobile as well as desktop.

## Acceptance Criteria
- Approval required defaults to ON.
- Before approval, each story card exposes one clearly labeled teaser/preview player.
- Approval controls stay disabled until parent verification succeeds, and unsupported hosts explain the localhost recovery path instead of attempting approval.
- Approve endpoint requires a valid parent gate token bound to the same soft user before it marks full audio as ready.
- After approval, the full narration player replaces the teaser control only once the full narration surface reads as credibly playable, including a visible readiness or duration cue.
- Approval feedback stays anchored to the affected story card with calm recovery copy when a retry is needed.
- In local development, parent passkey verification is supported on `localhost`; unsupported hosts must show recovery guidance before attempting verification and offer a direct localhost handoff instead of raw troubleshooting copy.
