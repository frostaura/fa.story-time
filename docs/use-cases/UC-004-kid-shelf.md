# UC-004 — Kid Shelf Recent and Favorites

## Goal
Allow child-safe browsing that only shows Recent and Favorites while in Kid Shelf mode.

## Actors
Parent, Child

## Preconditions
- A soft user identity exists.
- At least one story exists in the user library.

## Main Flow
1. User enables Kid Shelf from the Home header toggle.
2. App requests library data with `kidMode=true`.
3. Backend returns kid-safe `recent` and `favorites` slices.
4. App renders only those entries in Recent and Favorites shelves.

## Variants / Edge Cases
- If no items are present, the shelves show empty-state copy.
- If library request fails, the app surfaces an error message.

## Acceptance Criteria
- Kid Shelf toggle drives `kidMode=true` library calls.
- Recent shelf uses server-curated recent IDs in Kid Shelf mode.
- Favorites shelf uses server-curated favorite IDs in Kid Shelf mode.

## Notes
- Server limits are controlled via `StoryTime:Catalog:KidShelfRecentLimit` and `StoryTime:Catalog:KidShelfFavoritesLimit`.
