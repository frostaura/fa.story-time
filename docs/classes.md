# Backend Class Map

- `StoryGenerationService`: builds coherent story output and poster layers.
- `SubscriptionPolicyService`: enforces tier duration, cooldown, and concurrency.
- `InMemoryStoryCatalog`: tracks non-PII library metadata in process memory.
- `FileSystemStoryCatalog`: persists metadata-only library entries to disk without narrative payloads.
- `Program`: API composition and route wiring.
