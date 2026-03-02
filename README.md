<p align="center">
  <img src="README.icon.png" alt="StoryTime" width="300" />
</p>

# StoryTime 🌙

A calm-first, mobile-focused story generation platform with a .NET API and React PWA frontend.

## Features
- Quick Generate from Home with always-visible duration slider.
- Series continuity with Story Bible state.
- One-shot generation mode.
- Parent approval flow (teaser first, full audio on approval).
- Kid Shelf browsing limited to Recent + Favorites.
- Tier-based cooldown, concurrency, and duration enforcement.
- Procedural parallax poster fallback with 3-5 layers.
- Dedicated visual regression snapshot coverage for core UI states.
- Automated WCAG AA contrast checks for core UI surfaces.

## Project Structure
```
docs/
  specs/
  testing/
src/
  backend/
    StoryTime.Api/
    StoryTime.Api.Tests/
  frontend/
```

## Prerequisites
- .NET 10 SDK
- Node.js 18+
- npm
- make

## Getting Started
```bash
cd src/frontend
npm install
cd ../..
dotnet restore src/backend/StoryTime.slnx
```

Run backend:
```bash
dotnet run --project src/backend/StoryTime.Api
```

Run frontend:
```bash
cd src/frontend
npm run dev
```

## Quality Gates
```bash
make lint
make build
make test
```

## Docker API
```bash
cp .env.example .env
docker compose up --build -d
curl http://localhost:8080/api/home/status
docker compose down
```

For live-provider readiness, set `STORYTIME_POSTER_PROVIDER_ENDPOINT`, `STORYTIME_NARRATION_PROVIDER_ENDPOINT`,
`STORYTIME_AI_PROVIDER_ENDPOINT`, and `STORYTIME_CHECKOUT_PROVIDER_ENDPOINT` to real provider URLs. Story Bible
persistence defaults to on (`STORYTIME_PERSIST_SERIES_STORY_BIBLE=true`) and can be tuned with
`STORYTIME_PERSIST_CONTINUITY_FACTS` and `STORYTIME_STORY_BIBLE_FILE_PATH`.

## Documentation
- [System Spec](docs/specs/system_spec.md)
- [Test Spec](docs/specs/test_spec.md)
- [Traceability Matrix](docs/specs/traceability_matrix.md)
- [Testing How-To](docs/testing/how-to-run.md)
- [Use Cases](docs/use-cases.md)

## License
MIT License - see [LICENSE](./LICENSE) for details.
