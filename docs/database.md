# Database Notes

The current baseline does not persist story payloads server-side.
Only subscription/config state would be persisted in full production deployment.
Current implementation uses in-memory state to preserve privacy constraints while enabling integration testing.
