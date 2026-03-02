# Sequence Summary

1. Client invokes `POST /api/stories/generate`.
2. Subscription policy validates tier limits.
3. Story generation service creates scenes and poster layers.
4. Metadata-only story entry is stored in catalog.
5. Client can approve full audio and mark favorites.
