# UC-004 — Kid Shelf Recent and Favorites

## Goal
Allow child-safe browsing that only shows Recent and Favorites while in Kid Shelf mode.

## Actors
Parent, Child

## Preconditions
- A soft user identity exists.
- At least one story exists in the user library.

## Main Flow
1. Parent unlocks Parent Controls with the parent gate.
2. Parent enables Kid Shelf from the parent-controlled Kid Shelf setting.
3. App requests library data and the backend returns kid-safe `recent` and `favorites` slices with `kidShelfEnabled=true`.
4. App renders only those entries in Recent and Favorites shelves and shows Kid Shelf as active.

## Variants / Edge Cases
- If no items are present, the shelves show empty-state copy.
- If library request fails, the app keeps Quick Generate and the shelves visually calm, preserves compact empty states, and offers a retry action with plain-language guidance.
- Story-card favorite feedback stays anchored to the card that triggered it so Quick Generate is not used as a fallback error surface.

## Acceptance Criteria
- Kid Shelf can only be changed after parent-gate verification.
- Recent shelf uses server-curated recent IDs when `kidShelfEnabled=true`.
- Favorites shelf uses server-curated favorite IDs when `kidShelfEnabled=true`.
- Library responses remain kid-safe because the backend ignores client-side attempts to opt out of Kid Shelf once the parent-managed setting is enabled.
- Library retry guidance avoids raw transport-status copy.
- Favorite actions expose local progress and local recovery copy on the affected story card.
- Recent and Favorites remain visually distinct; the Recent shelf keeps the richer layered poster treatment, while the Favorites shelf stays more compact so saved content does not repeat the same full-card treatment twice.

## Notes
- Server limits are controlled via `StoryTime:Catalog:KidShelfRecentLimit` and `StoryTime:Catalog:KidShelfFavoritesLimit`.
