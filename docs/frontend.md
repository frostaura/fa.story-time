# Frontend Design Notes

## Stack

- React 19 + TypeScript
- Vite application shell and PWA packaging
- Fetch-based API client in `src/services/storyTimeApi.ts`
- Vitest + Testing Library for unit and integration-style UI tests

## Core UI Composition

- **App shell (`App.tsx`)**
  - owns page-level state and orchestrates async workflows
  - wires Quick Generate, Parent Controls, Paywall, Recent shelf, Favorites shelf
- **Extracted components (`src/components`)**
  - reusable sections that reduce monolithic JSX in `App`
  - typed props and event callbacks only (no direct side effects)
- **Styling**
  - `App.css` and `index.css` supply the calm-first visual system
  - poster parallax layers are rendered via CSS animation duration/delay values

## State Model

- **Local component state** in `App`:
  - current mode (`series` / `one-shot`)
  - duration and one-shot customization fields
  - loading/error/paywall view state
  - parent gate token and toggles
- **Derived state**:
  - `storiesById`, favorites projection, kid-shelf recent projection
- **Persistence**:
  - LocalStorage keys are runtime-configured (`runtimeConfig.storageKeys`)
  - persisted entities: soft user identity, child profile, story artifacts, parent credential metadata

## API Interaction Pattern

1. `resolveApiBaseUrl()` chooses runtime backend endpoint.
2. `createStoryTimeApi(baseUrl)` returns route-safe API methods.
3. `App` invokes API methods in event handlers (`onGenerate`, `approveStory`, `toggleFavorite`, etc.).
4. Responses are normalized into frontend state and persisted to LocalStorage.

## Accessibility and UX Guarantees

- Keyboard and label-based inputs for all primary controls.
- Reduced-motion mode is user-toggleable and also respects environment preference.
- Contrast checks are enforced via visual accessibility tests.

## Frontend Testing Taxonomy

- `src/tests/`:
  - behavior/unit tests for app interactions and state transitions
  - visual snapshot and contrast regression coverage
  - component-focused tests for extracted UI sections
- `tests/e2e/`:
  - Vitest live-backend end-to-end flow checks
- `tests/playwright/`:
  - Playwright browser-level rendering and interaction checks
  - responsive viewport browser checks across mobile/tablet/desktop
  - live-backend Playwright smoke coverage for critical browser flows
