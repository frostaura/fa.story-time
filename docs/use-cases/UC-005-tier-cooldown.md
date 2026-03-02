# UC-005 — Tier and Cooldown Enforcement

## Goal
Enforce subscription duration, concurrency, and cooldown limits, and allow upgrades through checkout/webhook flows.

## Actors
Parent, Billing Provider, Backend

## Preconditions
- A soft user identity exists.
- Subscription policy service has configured tier limits.

## Main Flow
1. User requests story generation with a specific duration.
2. Backend policy evaluates tier duration/concurrency/cooldown rules.
3. If allowed, generation starts and reservation is tracked.
4. When generation completes, cooldown is applied.
5. If blocked by tier duration, backend returns paywall details.
6. Parent-verified checkout upgrades tier, then cooldown resets.

## Variants / Edge Cases
- Invalid or expired checkout sessions are rejected.
- Cross-user or missing gate tokens are rejected.
- Subscription webhook updates tier and can optionally reset cooldown.

## Acceptance Criteria
- Trial/Plus/Premium limits are enforced from `StoryTime:TierLimits`.
- 402 responses include paywall metadata for upgrade UX.
- Webhook updates entitlement and optional cooldown reset.

## Notes
- Checkout defaults and provider behavior are configured under `StoryTime:Checkout`.
