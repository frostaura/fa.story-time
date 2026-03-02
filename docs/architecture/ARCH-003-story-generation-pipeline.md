# ARCH-003 — Five-Stage Story Generation Pipeline

## Summary
Generate stories using a deterministic five-stage pipeline: `outline -> scene plan -> scene batch -> stitch -> polish`.

## Context & Problem
Story generation must preserve calm tone, continuity, and predictable output contracts for both one-shot and series mode. A single-pass generator produced unstable structure and made continuity constraints harder to enforce consistently.

## Decision
- Use a staged pipeline in `StoryGenerationService`:
  1. **Outline** for high-level arc framing
  2. **Scene plan** for ordered scene objectives
  3. **Scene batch** for per-scene prose
  4. **Stitch** to produce a coherent narrative sequence
  5. **Polish** to normalize bedtime tone and final output quality
- Keep AI provider access behind configurable `Generation:AiOrchestration` options.
- Preserve deterministic local fallback behavior when external providers are unavailable.
- Keep Story Bible continuity updates and poster/audio generation aligned with the same generation transaction.

## Alternatives Considered
- Single-pass generation using one prompt.
- Two-pass generation (outline + final story only).

## Consequences
- Better continuity quality for UC-002 series flows.
- Clear stage boundaries improve testability and failure diagnostics.
- Configuration surface is larger but explicitly validated at startup.

## Affected Components
- `src/backend/StoryTime.Api/Services/StoryGenerationService.cs`
- `src/backend/StoryTime.Api/StoryTimeOptions.cs` (`Generation` and `NarrativeTemplates`)
- `src/backend/StoryTime.Api/appsettings.json` (`StoryTime:Generation`)

## Notes
- OpenRouter endpoint enforcement remains mandatory when AI orchestration is enabled.
