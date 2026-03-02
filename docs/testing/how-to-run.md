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

`test:browser-e2e` builds the frontend and runs Playwright against `vite preview` (production bundle), not the dev server.

## Docker (API)
```bash
cp .env.example .env
docker compose up --build -d
curl http://localhost:8080/api/home/status
docker compose down
```
