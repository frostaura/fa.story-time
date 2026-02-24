<p align="center">
  <img src="README.icon.png" alt="TaleWeaver" width="300" />
</p>

# TaleWeaver 🌙

A mobile-first Progressive Web App that generates and plays personalized AI-powered bedtime stories.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

---

## Features

- **Quick Generate**: One-tap story generation with customizable duration
- **Series Mode**: Never-ending cohesive stories with a persistent Story Bible
- **One-shot Mode**: Standalone stories with full customization
- **AI Pipeline**: 5-pass coherence algorithm (outline → scene plan → scene batch → stitch → polish)
- **Parallax Posters**: 3-5 layer animated story covers with gyroscope/tilt support
- **Procedural Fallback**: SVG-based poster generation when AI images are unavailable
- **Privacy First**: No personal data stored server-side; all stories/profiles in browser LocalStorage
- **Parental Controls**: WebAuthn-gated settings, Kid Mode, approval workflows
- **Offline Support**: Service worker queues generation requests when offline
- **Subscription Tiers**: Trial (7-day), Plus, and Premium via Stripe

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React 18, TypeScript 5, Vite, Redux Toolkit, Framer Motion |
| Backend | .NET 10 Web API, Entity Framework Core |
| Database | PostgreSQL (config & subscriptions only) |
| AI | OpenRouter (Claude 3.5 Sonnet, Flux) |
| TTS | Coqui TTS (local) |
| Payments | Stripe |
| Observability | OpenTelemetry → SigNoz |

---

## Project Structure

```
docs/               # Architecture, database, use cases, design specs
src/
├── backend/        # .NET 10 Web API
│   ├── TaleWeaver.Api/
│   └── TaleWeaver.Api.Tests/
└── frontend/       # React + Vite PWA
    └── src/
        ├── components/   # UI components
        ├── pages/        # Route pages
        ├── services/     # API client, localStorage, notifications
        ├── store/        # Redux store & slices
        └── types/        # TypeScript interfaces
```

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js 18+
- PostgreSQL 15+
- (Optional) Coqui TTS server

### Backend

```bash
cd src/backend
dotnet restore
dotnet build
dotnet run --project TaleWeaver.Api
```

### Frontend

```bash
cd src/frontend
npm install
npm run dev
```

The frontend runs at http://localhost:5173

### Configuration

Copy `appsettings.Development.json` and configure:

- PostgreSQL connection string
- OpenRouter API key
- Stripe keys
- Coqui TTS URL

---

## Documentation

- [Architecture](docs/architecture.md)
- [Database Schema](docs/database.md)
- [Use Cases](docs/use-cases.md)
- [Frontend Design](docs/frontend.md)
- [Class Diagrams](docs/classes.md)
- [Sequence Diagrams](docs/sequence.md)

---

## License

MIT License - see [LICENSE](./LICENSE) for details.
