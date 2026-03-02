# StoryTime Test Specification

## 1. Test Scope
This specification defines required unit, integration, and end-to-end coverage for StoryTime backend and frontend surfaces.

## 2. Test Levels

### 2.1 Unit Tests
Location: `src/backend/StoryTime.Api.Tests/Unit/`, `src/frontend/src/tests/`

Coverage goals:
- Story generation continuity behavior.
- Poster procedural fallback behavior and performance budget.
- Subscription policy enforcement.
- Parent-gate security variants (relying-party mismatch, signature-counter replay, and assertion validation).
- Log-safety redaction behavior for user identifiers (hashing-only, no raw ID leakage in emitted values).
- Quick Generate UI baseline rendering.
- Dedicated visual regression snapshot baselines for key UI states plus WCAG AA contrast assertions for core surfaces.

### 2.2 Integration Tests
Location: `src/backend/StoryTime.Api.Tests/Integration/`

Coverage goals:
- Home status API behavior.
- Generate/approve/favorite/library endpoint interactions.
- Trial duration/cooldown enforcement.
- Checkout authorization edge cases (missing gate token, cross-user gate token rejection, invalid/expired session completion).
- CORS policy behavior (allow configured origins/methods and deny unknown origins).

### 2.3 End-to-End Tests
Location: `src/backend/StoryTime.Api.Tests/E2E/`, `src/frontend/tests/e2e/`

Coverage goals:
- Full premium series flow including continuation and approval.
- Storage audit to ensure no narrative text persistence server-side.
- Frontend Quick Generate interaction through fetch boundary.

## 3. Required Gates
- `lint`: `make lint`
- `build`: `make build`
- `unit`: included in `make test`
- `integration`: included in `make test`
- `e2e`: included in `make test`
- `ci`: GitHub Actions workflow `.github/workflows/ci.yml`

## 4. Execution Commands
From repository root:

```bash
make lint
make build
make test
```

Direct commands:

```bash
dotnet test src/backend/StoryTime.slnx
cd src/frontend && npm run test
```

## 5. Definition of Pass
A run is passing when:
- all test projects execute without failure,
- no lint/build errors remain,
- CI job completes successfully on push/PR.
