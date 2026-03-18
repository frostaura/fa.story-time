# How to Run and Test StoryTime

## Prerequisites
- .NET 10 SDK
- Node.js 22+
- npm
- GNU Make

## Install
```bash
cd src/frontend
npm install
cd ../..
dotnet restore src/backend/StoryTime.slnx
```

## Run backend
```bash
dotnet run --project src/backend/StoryTime.Api
```

## Run frontend
```bash
cd src/frontend
npm run dev
```

For parent-passkey QA, open the frontend on `http://localhost:5173` (or the matching localhost preview URL). The local WebAuthn relying party ID defaults to `localhost`, so `127.0.0.1` now shows setup guidance instead of attempting a broken verification flow.

## Quality commands
```bash
make traceability
make lint
make build
make test
make verify
make test-coverage
cd src/frontend && npm run test:unit -- --coverage
```

`make test` now includes frontend browser E2E (`npm run test:browser-e2e`).

For a direct fetch-boundary run against the live backend process, use:

```bash
cd src/frontend
npm run test:e2e
```

This Vitest lane keeps the backend HTTP boundary live, but it uses mocked WebAuthn plus relaxed backend assertion requirements because JSDOM cannot complete the browser-native passkey ceremony. Treat strict localhost passkey proof as Playwright + backend integration coverage, not as a Vitest responsibility.

`make verify` is the canonical local CI-equivalent gate. It runs the same logical bar as `.github/workflows/ci.yml`: lint, build, traceability, tests, focused/skipped-test governance, frontend coverage, env-example validation, backend coverage collection, and backend coverage threshold enforcement.

## Browser E2E (Playwright, direct)
```bash
cd src/frontend
npx playwright install chromium
npm run test:browser-e2e
```

`test:browser-e2e` builds the frontend, launches `vite preview`, boots a local backend with AI orchestration disabled for deterministic browser runs, and executes Playwright against the production bundle with strict parent-gate requirements still enabled.

By default the browser suite now uses isolated ports (`4184` for the preview app, `19082` for the backend) and does **not** reuse arbitrary servers already listening on compose/debug ports. Override `PLAYWRIGHT_WEB_PORT`, `PLAYWRIGHT_API_PORT`, or set `PLAYWRIGHT_REUSE_EXISTING_SERVER=true` only when you intentionally want to attach Playwright to an already-running stack.

Browser coverage is intentionally split:
- `src/frontend/tests/playwright/quick-generate.browser.e2e.spec.ts` keeps deterministic mocked browser contract checks for UC-001 through UC-005.
- `src/frontend/tests/playwright/quick-generate.full-stack.browser.e2e.spec.ts` adds live-backend browser coverage for generation, approval, favorites, paywall, and localhost parent verification using a Chromium virtual authenticator while reading the same runtime-configured storage keys that the shipped app uses.
- `src/frontend/tests/playwright/responsive.browser.e2e.spec.ts` exercises mobile, tablet, and desktop viewport behavior.

The mocked browser suite runs on `127.0.0.1` by design, so it asserts the unsupported-host recovery copy for parent verification rather than attempting a passkey ceremony there.

## Docker (full stack)
```bash
cp .env.example .env
docker compose up --build -d
open http://localhost:4173
curl http://localhost:8080/api/home/status
docker compose down
```

`docker compose` now starts both the API and the frontend preview server. Override `FRONTEND_API_BASE_URL` in `.env` if the browser-facing API origin should differ from `http://localhost:8080`.

For a repeatable compose-backed regression pass against the shipped stack, run:

```bash
python3 scripts/compose-regression.py --webhook-secret "$STORYTIME_CHECKOUT_WEBHOOK_SHARED_SECRET"
```

The regression script validates frontend reachability plus API generate/approve/favorite/library/storage-audit flows, paywall and unauthorized checkout boundaries, webhook-secret rejection, and a repeated premium flow against the compose runtime.
