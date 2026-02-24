# TaleWeaver — Documentation Index

> **TaleWeaver** is a mobile-first Progressive Web App (PWA) that generates AI-powered bedtime stories for children. Parents tap "Generate," and the app produces an illustrated, narrated story tailored to a child's age, interests, and optional series continuity — all while keeping every byte of personal data on-device.

## Tech Stack at a Glance

| Layer | Technology |
|---|---|
| Frontend | React 18 · TypeScript 5 · Vite PWA (port 5173) |
| State | Redux Toolkit + RTK Query |
| Backend | .NET 10 Web API · EF Core · PostgreSQL |
| AI — Text | OpenRouter → Claude 3.5 Sonnet |
| AI — Images | OpenRouter → Flux |
| AI — Voice | Local Coqui TTS |
| Payments | Stripe Subscriptions |
| Observability | OpenTelemetry → SigNoz |
| Privacy | Zero PII server-side; LocalStorage for all user data |

## Documents

| Document | Description |
|---|---|
| [Architecture](./architecture.md) | System context, component diagram, data-flow, and the 5-pass coherence pipeline |
| [Database](./database.md) | PostgreSQL schema — server-side config, tiers, subscriptions (no story content) |
| [Use Cases](./use-cases.md) | UC-1 through UC-8: actors, preconditions, flows, postconditions |
| [Frontend](./frontend.md) | Design language, component catalogue, LocalStorage keys, accessibility |
| [Classes](./classes.md) | Mermaid class diagrams for backend entities, services, and pipeline DTOs |
| [Sequences](./sequence.md) | Mermaid sequence diagrams for generation, subscription, and offline flows |

## Conventions

- **Spec-driven**: `docs/` is the single source of truth. Code must match specs and vice-versa.
- **PascalCase** for all database table and column names.
- **Soft-delete** (`IsDeleted` flag) for every entity.
- **SoftUserId** (anonymous, device-scoped) is the only user identifier on the server.
- All story content, child profiles, Story Bibles, poster layers, and audio live exclusively in browser **LocalStorage**.
