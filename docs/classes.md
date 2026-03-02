# Backend Class Map

## Core Domain Contracts

- **Story contracts**
  - `StoryBible`: continuity state for series generation (`seriesId`, arc progress, audio anchors)
  - `PosterLayer`: layered visual contract (`role`, `speedMultiplier`, data URI payload)
  - generation DTOs in `DTOs/` coordinate request/response boundaries
- **Policy contracts**
  - tier and cooldown/concurrency rule models under options/config sections
  - checkout and paywall DTOs for upgrade path responses

## Service Layer

- `StoryGenerationService`
  - orchestrates outline -> scene plan -> batch -> stitch -> polish flow
  - creates teaser/full audio metadata and poster layers
  - emits responses aligned with system spec contracts
- `SubscriptionPolicyService`
  - validates tier duration constraints
  - applies cooldown and concurrency reservation logic
  - drives 402 paywall behavior
- `ParentSettingsService`
  - validates gate token/session and updates parent-controlled settings
- `ProceduralMediaAssetService`
  - builds deterministic fallback poster/audio assets when provider paths are unavailable
  - uses `ProceduralPosterGeometryOptions.DriftCenterOffset` (default `80`) to center procedural drift around zero when `DriftVariance` is `160`

## Catalog and Persistence Adapters

- `InMemoryStoryCatalog`
  - default catalog for metadata-only runtime storage
- `FileSystemStoryCatalog`
  - file-backed metadata catalog for durability across restarts
  - preserves privacy by omitting narrative/audio/poster payloads

## Application Composition

- `Program`
  - binds options and validates configuration
  - registers services and catalog implementation
  - maps API routes for home, stories, library, parent gate, settings, and subscription flows

## Validation and Utilities

- options validators enforce required provider and route constraints
- identifier hashing utilities protect logs from raw user identifiers
