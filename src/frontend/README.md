# StoryTime Frontend

React + TypeScript + Vite frontend for StoryTime.

## Commands

```bash
npm install
npm run dev
npm run lint
npm run build
npm run test
```

## Runtime configuration

The app reads these environment variables:

- `VITE_API_BASE_URL` (default current browser origin)
- `VITE_DEFAULT_DURATION_MINUTES`
- `VITE_DEFAULT_DURATION_MAX_MINUTES`
- `VITE_DEFAULT_DURATION_SELECTION`
- `VITE_DEFAULT_CHILD_NAME`
- `VITE_LIBRARY_RECENT_LIMIT`
- `VITE_STORAGE_KEY_STORY_ARTIFACTS`, `VITE_STORAGE_KEY_SOFT_USER_ID`, `VITE_STORAGE_KEY_CHILD_PROFILE`, `VITE_STORAGE_KEY_PARENT_CREDENTIAL`
- `VITE_PARENT_GATE_WEBAUTHN_RP_DISPLAY_NAME`, `VITE_PARENT_GATE_WEBAUTHN_USER_DISPLAY_NAME`
- `VITE_PARENT_GATE_WEBAUTHN_TIMEOUT_MS`, `VITE_PARENT_GATE_WEBAUTHN_USER_VERIFICATION` (`required` | `preferred` | `discouraged`)
- `STORYTIME_E2E_BACKEND_PORT`, `STORYTIME_E2E_BACKEND_HOST`, `STORYTIME_E2E_BACKEND_BASE_URL` (frontend e2e backend target override)

For local development, run the backend first:

```bash
dotnet run --project ../backend/StoryTime.Api
```

## Test notes

- Unit tests live in `src/tests`.
- Frontend E2E tests live in `tests/e2e` and execute against a live backend API.
