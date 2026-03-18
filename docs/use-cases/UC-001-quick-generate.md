# UC-001 — Quick Generate

## Goal
Generate a bedtime story from Home without navigation.

## Actors
Parent, Child

## Preconditions
A soft user identity exists in LocalStorage.

## Main Flow
1. User lands on Home.
2. Quick Generate and duration slider are visible.
3. User taps Generate story.
4. Story appears in Recent shelf with a visibly layered poster treatment.

## Variants / Edge Cases
- If Recent/Favorites cannot sync immediately, Home still renders Quick Generate plus compact empty shelves and offers calm retry guidance without exposing raw transport errors.
- If generation is temporarily rate-limited, the app keeps the user on Home and explains how to retry in plain language.
- Mode-specific controls must acknowledge user intent immediately: one-shot optional details expand in place, continuation selections summarize the active target, and every generate attempt resolves into visible loading, success, or error feedback inside the Quick Generate card.

## Acceptance Criteria
- Quick Generate and duration controls are immediately visible.
- Story generation success updates Recent shelf.
- Generated story cards surface a visibly layered poster by default; when reduced motion is off, the result feels softly alive rather than like a flat placeholder.
- The primary CTA changes label with context (`Generate story`, `Continue series`, `Generate one-shot`) and never behaves like a silent click.
- One-shot optional details remain progressive by default, then visibly expand and focus the first revealed field when requested.
- Home stays usable and visually calm even when library bootstrap needs a retry.
