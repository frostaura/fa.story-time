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

## Quality commands
```bash
make lint
make build
make test
make test-coverage
cd src/frontend && npm run test:unit -- --coverage
```

`make test` now includes frontend browser E2E (`npm run test:browser-e2e`).

## Browser E2E (Playwright, direct)
```bash
cd src/frontend
npx playwright install chromium
npm run test:browser-e2e
```

`test:browser-e2e` builds the frontend, launches `vite preview`, boots a local backend with AI orchestration disabled for deterministic browser runs, and executes Playwright against the production bundle.

Browser coverage is intentionally split:
- `src/frontend/tests/playwright/quick-generate.browser.e2e.spec.ts` keeps deterministic mocked browser contract checks for UC-001 through UC-005.
- `src/frontend/tests/playwright/quick-generate.full-stack.browser.e2e.spec.ts` adds live-backend browser smoke coverage for the most critical paths.
- `src/frontend/tests/playwright/responsive.browser.e2e.spec.ts` exercises mobile, tablet, and desktop viewport behavior.

## Docker (API)
```bash
cp .env.example .env
docker compose up --build -d
curl http://localhost:8080/api/home/status
docker compose down
```
