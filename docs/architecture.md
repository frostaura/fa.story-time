# Architecture Overview

StoryTime is a split web system optimized for calm bedtime flows, privacy-first data handling, and configuration-driven behavior.

## System Context

- **Frontend (`src/frontend`)**: React + TypeScript + Vite PWA that renders Home, Quick Generate, shelves, parent controls, and upgrade UX.
- **Backend (`src/backend/StoryTime.Api`)**: ASP.NET Core minimal API that orchestrates story generation, policy checks, parent gate, subscriptions, and metadata-only library state.
- **Test harness (`src/backend/StoryTime.Api.Tests`, `src/frontend/src/tests`, `src/frontend/tests/e2e`)**: unit/integration/e2e coverage validating behavior from contracts in `docs/specs/`.

## High-Level Architecture

1. Frontend resolves runtime config and calls backend routes through `createStoryTimeApi`.
2. Backend validates request constraints (tier policy, parent gate where needed, options validation).
3. Story generation pipeline produces recap/scenes and media artifacts (teaser + optional full audio + poster layers).
4. Backend returns story payload to client and stores only metadata required for library operations.
5. Frontend persists story artifacts and profile state in LocalStorage using configured `tw_` keys.

## Backend Structure

- **Composition root**: `Program.cs` wires DI services, config binding/validation, CORS, and minimal API endpoints.
- **Services**:
  - `StoryGenerationService`: multi-pass narrative generation and media assembly.
  - `SubscriptionPolicyService`: tier duration, cooldown, and concurrency enforcement.
  - `ParentSettingsService`: parent-gated settings read/update.
  - `ProceduralMediaAssetService`: procedural poster/audio fallback and rendering parameters.
- **Catalog implementations**:
  - `InMemoryStoryCatalog`: runtime metadata storage.
  - `FileSystemStoryCatalog`: metadata-only persistence without narrative payloads.
- **Config model**: strongly-typed `StoryTimeOptions` bound from `appsettings.json` and environment variables.

## Frontend Structure

- `App.tsx` orchestrates page-level state and asynchronous flows.
- `config/` defines typed runtime defaults, API routes, and mode/message contracts.
- `services/storyTimeApi.ts` centralizes HTTP requests.
- `components/` contains extracted presentational sections from `App`.
- State persistence uses LocalStorage for soft user ID, story artifacts, child profile, and parent credential metadata.

## Security and Privacy Boundaries

- No server-side persistence of story narrative text, poster data URIs, audio payloads, or child PII.
- Parent controls and upgrade actions require parent-gate verification flows.
- Logged identifiers are anonymized/hardened in backend code paths.

## Runtime and Delivery

- Canonical local gates: `make lint`, `make build`, `make test`.
- CI (`.github/workflows/ci.yml`) enforces the same gate order.
- Docker workflow (`docker-compose.yml`) provisions backend API with `.env`-driven configuration.

## Architecture Decision Records

- `docs/architecture/ARCH-001-frontend-api-service-layer.md`
- `docs/architecture/ARCH-002-backend-service-composition.md`
- `docs/architecture/ARCH-003-story-generation-pipeline.md`
- `docs/architecture/ARCH-004-parent-gate-webauthn-security-model.md`
