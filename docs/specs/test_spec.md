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
- Motion-enabled versus reduced-motion story-card rendering states, including layered poster presentation that does not collapse into a flat single-plane card.
- Component-level snapshot baselines for key UI states plus WCAG AA contrast assertions for core surfaces.

### 2.2 Integration Tests
Location: `src/backend/StoryTime.Api.Tests/Integration/`

Coverage goals:
- Home status API behavior.
- Generate/approve/favorite/library endpoint interactions.
- Trial duration/cooldown enforcement.
- Checkout authorization edge cases (missing gate token, cross-user gate token rejection, invalid/expired session completion).
- CORS policy behavior (allow configured origins/methods/headers and deny unknown origins or unsupported methods).

### 2.3 End-to-End Tests
Location: `src/backend/StoryTime.Api.Tests/E2E/`, `src/frontend/tests/e2e/`, `src/frontend/tests/playwright/`

Coverage goals:
- Full premium series flow including continuation and approval.
- Storage audit to ensure no narrative text persistence server-side.
- Frontend Quick Generate interaction through fetch boundary.
- Frontend browser-rendered flow validation through Playwright, including responsive viewport coverage, motion-enabled layered-poster rendering checks, strict localhost WebAuthn verification with a virtual authenticator, and live-backend generation/continuation/paywall/checkout paths alongside mocked contract journeys.

### 2.4 Non-Functional Validation Matrix
Location: backend integration/e2e suites, frontend Playwright suites, and `make test-coverage`.

Coverage goals:
- **Performance budget checks**: procedural poster fallback must complete within configured budget (`<= 200ms` by default) and retain 3-5 layer contract.
- **Resilience/fault injection checks**: external provider failures must produce explicit stage/media errors when local fallback is disallowed.
- **Load/readiness checks**: generate + approve + library retrieval sequence must remain stable across repeated runs in CI (`make test` on clean agents).
- **Security/privacy checks**: storage audit and identifier hashing assertions must confirm no narrative text or raw identifiers leak to persistence/logging surfaces.
- **Visual-motion checks**: motion-enabled story cards must expose a perceptibly layered poster treatment (multiple layers plus motion-ready presentation), while frontend visual regression coverage keeps reduced-motion rendering readable without continuous motion.

## 3. Required Gates
- `traceability`: `make traceability`
- `lint`: `make lint`
- `build`: `make build`
- `unit`: included in `make test`
- `integration`: included in `make test`
- `e2e`: included in `make test`
- `ci`: GitHub Actions workflow `.github/workflows/ci.yml`

## 4. Execution Commands
From repository root:

```bash
make verify
```

Equivalent expanded local gate set:

```bash
make lint
make build
make traceability
make test
make test-governance
make frontend-coverage
make validate-env
make test-coverage
make backend-coverage
```

Direct commands:

```bash
dotnet test src/backend/StoryTime.slnx
cd src/frontend && npm run test
cd src/frontend && npx playwright install chromium
cd src/frontend && npm run test:browser-e2e
cd src/frontend && npm run test:unit -- --coverage
python3 scripts/validate-env-examples.py
make test-coverage
```

## 5. Definition of Pass
A run is passing when:
- all test projects execute without failure,
- no lint/build errors remain,
- frontend coverage thresholds pass,
- backend coverage artifact is produced,
- backend coverage threshold passes,
- `make verify` succeeds locally,
- CI job completes successfully on push/PR.
- Traceability and completion-evidence artifacts resolve to real paths.

## 6. Coverage Threshold Policy
- Backend line coverage floor: **80%** minimum for `StoryTime.Api` assemblies (hard-failed in CI by parsing Cobertura output from `make test-coverage`; generated build artifacts such as `obj/**` are excluded from the denominator).
- New/changed backend code should include unit or integration assertions for success and failure paths.
- Frontend line coverage floor: **70%** minimum for the unit-tested runtime surface (`src/frontend/src/App.tsx`, `src/frontend/src/components/**`, `src/frontend/src/services/**`, `src/frontend/src/pwa.ts`), hard-failed in CI via `cd src/frontend && npm run test:unit -- --coverage`.
- New/changed frontend code should include targeted assertions for both success and failure paths, with an expectation of **>=80% line coverage** for touched modules (or Playwright/browser e2e evidence when module-level coverage is not practical).

## 7. Test Data Lifecycle and Isolation
- Backend integration/e2e tests must create deterministic test data within each test scope and must not depend on persisted data from prior runs.
- Any filesystem artifacts created by tests (for example temporary story bible/catalog files) must be written to per-test temp paths and cleaned up in `finally` blocks.
- Frontend tests must reset shared state (localStorage, mocks, timers) between tests to prevent cross-test leakage.

## 8. Ownership and Escalation Matrix
- Backend unit/integration/e2e regressions: **backend maintainers** own triage and fix.
- Frontend unit/visual/vitest-e2e regressions: **frontend maintainers** own triage and fix.
- Playwright/browser e2e regressions: **frontend + platform owners** jointly own triage when failures involve CI/browser infrastructure.
- Blocking CI failures must be acknowledged within one business day and either fixed or explicitly quarantined with an owner and follow-up task (PR ownership routing is enforced through `.github/CODEOWNERS`, and remediation issues use `.github/ISSUE_TEMPLATE/flaky-test.yml`).

## 9. Flaky Test Policy
- A test is considered flaky if it fails non-deterministically across reruns without code/config changes.
- Flaky tests are not silently ignored: open a remediation task, tag the owning domain, and either stabilize immediately or quarantine with an expiry task. CI enforces this by failing on committed `.skip(...)`/`.only(...)` markers, and the remediation record uses `.github/ISSUE_TEMPLATE/flaky-test.yml`.
- Quarantined tests must be reviewed at least weekly and restored to blocking status once stabilized (see `docs/testing/TEST-002-flaky-test-governance.md`).
