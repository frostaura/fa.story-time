# TEST-003 — Completion Evidence Index

## Purpose
This index defines the minimum evidence set required before StoryTime can claim roadmap or release completion.

## Canonical evidence set
- `make verify`
- `STORYTIME_CHECKOUT_WEBHOOK_SHARED_SECRET=<secret> make up && python3 scripts/compose-regression.py --webhook-secret <secret> && make down`
- CI pass on `.github/workflows/ci.yml`
- Manual regression labels:
  - `curl` for API-impacting changes
  - `playwright-mcp` for web-impacting changes

`make verify` expands to the same local gates CI expects: `make lint`, `make build`, `make traceability`, `make test`, `make test-governance`, `make frontend-coverage`, `make validate-env`, `make test-coverage`, and `make backend-coverage`.

## Evidence capture rules
- Keep evidence low-context: record file paths, commands, and labels instead of pasting long logs.
- If a roadmap item changes docs, tests, or CI, update the corresponding proof paths in the same change set.
- Completion claims are invalid when `ROADMAP.md` or `COMPLETION_REPORT.md` drift from the current repo state.
- Local gate passes are preflight only until the exact ref is pushed and `.github/workflows/ci.yml` is green for that same ref.

## Required artifact references
- Flaky-test remediation issues use `.github/ISSUE_TEMPLATE/flaky-test.yml`.
- Traceability enforcement reads `docs/specs/traceability_matrix.md`.
- Testing policy is defined in `docs/specs/test_spec.md`.
- Manual execution guidance lives in `docs/testing/how-to-run.md`.
- Current implementation handoff lives in `IMPLEMENTATION_SUMMARY.md`.
- Current audit state lives in `COMPLETION_REPORT.md`.
