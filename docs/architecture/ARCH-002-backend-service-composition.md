# ARCH-002 — Backend Service Composition in Program.cs

## Summary
Keep backend wiring centralized in `Program.cs` using explicit DI registration and strongly typed options validation.

## Context & Problem
StoryTime has multiple cross-cutting backend concerns (tier policy, generation orchestration, parent gate, and catalog persistence) and each request path depends on consistent runtime configuration. Splitting composition across multiple startup files would increase drift risk and make endpoint/service contracts harder to audit.

## Decision
- Bind `StoryTimeOptions` once at startup and validate on boot.
- Register the core services as singletons:
  - `IStoryGenerationService` → `StoryGenerationService`
  - `ISubscriptionPolicyService` → `SubscriptionPolicyService`
  - `IParentSettingsService` → `ParentSettingsService`
  - `IMediaAssetService` → `ProceduralMediaAssetService`
- Select catalog implementation from config (`InMemory` or `FileSystem`) via provider switch.
- Keep API endpoint mapping in the same composition root to make route-to-service traceability explicit.

## Alternatives Considered
- Split startup concerns into extension classes per domain.
- Move catalog-provider branching into factory classes only.

## Consequences
- Startup behavior is explicit and easy to audit against `docs/specs/system_spec.md`.
- Route drift is easier to detect because endpoint contracts remain near DI composition.
- Startup validation failures happen early instead of surfacing as runtime partial failures.

## Affected Components
- `src/backend/StoryTime.Api/Program.cs`
- `src/backend/StoryTime.Api/StoryTimeOptions.cs`
- `src/backend/StoryTime.Api/StoryTimeOptionsValidator.cs`

## Notes
- This decision prioritizes traceability and configuration safety over startup-file modularization.
