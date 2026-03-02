# Database Notes

StoryTime intentionally avoids storing story content and child-identifying data server-side.

## Current Runtime Persistence Model

- **Primary backend state store**: in-memory catalog and policy state.
- **Optional metadata persistence**: file-system metadata catalog (no narrative payloads).
- **Client persistence**: LocalStorage stores profile and story artifacts for playback continuity.

## Data Classes Stored Server-Side

Only non-sensitive operational metadata is stored or derived server-side:

- Soft-user subscription entitlement and cooldown/concurrency counters
- Metadata needed for library listing (`storyId`, flags, timestamps, mode, readiness state)
- Parent gate challenge/session lifecycle data

## Data Explicitly Not Stored Server-Side

- Story body text
- Child names and profiles
- Poster SVG/image payloads
- Narration audio payloads
- Raw prompt content

## Configuration Source

- All persistence and limits are configuration-driven through `StoryTimeOptions` (`appsettings.json` + env overrides).
- Catalog and policy behavior can be tuned without code changes.

## Production Extension Guidance

If a persistent database is introduced later:

1. Keep narrative payloads and child-identifying data out of server storage.
2. Persist only minimal metadata required for policy and library querying.
3. Preserve existing privacy contract and storage-audit test expectations.
