# TEST-002 — Flaky Test Governance

## Scope
Define the required triage, quarantine, ownership, and review workflow for flaky tests across backend, frontend, and browser suites.

## Strategy (unit / integration / e2e)
- Keep flaky-test prevention automatic in CI by rejecting committed `.skip(...)` and `.only(...)` markers.
- Route regressions through `.github/CODEOWNERS` and the flaky-test issue template in `.github/ISSUE_TEMPLATE/flaky-test.yml`.
- Require every quarantine decision to carry an owner, an expiry date, and a restoration plan.

## Prerequisites
- CI remains green before merge unless an explicit flaky-test remediation issue documents the quarantine.
- The failing test path and owning area are known.

## How to Run
From repository root:

```bash
make test
```

If a browser regression needs focused reproduction:

```bash
cd src/frontend
npm run test:browser-e2e
```

## Expected Results
- Blocking failures are acknowledged within one business day.
- The owning domain is explicit (`backend`, `frontend`, or `frontend+platform` for browser/CI infrastructure issues).
- Any quarantine uses a remediation issue with an expiry date and weekly review checkpoint.

## Regression Notes
- CI rejects committed focused or skipped tests in `.github/workflows/ci.yml`.
- Browser failures involving CI, Chromium, preview hosting, or backend bootstrapping are jointly owned by frontend and platform.

## Notes
- Use `.github/ISSUE_TEMPLATE/flaky-test.yml` for every flaky-test remediation or quarantine request.
