# How to Run and Test StoryTime

## Prerequisites
- .NET 10 SDK
- Node.js 18+
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
```

## Docker (API)
```bash
cp .env.example .env
docker compose up --build -d
curl http://localhost:8080/api/home/status
docker compose down
```
