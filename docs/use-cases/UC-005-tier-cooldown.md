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
6. Parent unlocks checkout, creates a checkout session for the requested higher tier, and is redirected to the checkout URL with a return callback.
7. App resumes from the return callback, completes the checkout session with the stored parent gate token, and then the upgraded tier resets cooldown.

## Variants / Edge Cases
- Invalid or expired checkout sessions are rejected.
- Cross-user or missing gate tokens are rejected.
- Parent settings retrieval uses the `X-StoryTime-Gate-Token` header instead of query-string transport.
- Subscription webhook updates tier and can optionally reset cooldown when `X-StoryTime-Webhook-Secret` matches the configured shared secret.
- Upgrade guidance stays inside the paywall surface, including in-place parent verification before checkout and the post-checkout confirmation state.

## Acceptance Criteria
- Trial/Plus/Premium limits are enforced from `StoryTime:TierLimits`, with checkout progression ordered by `StoryTime:Checkout:TierOrder`.
- 402 responses include paywall metadata for upgrade UX.
- Checkout creation requires a valid parent gate token and return URL.
- Webhook updates entitlement and optional cooldown reset only when authenticated.
- The paywall presents the current tier, upgrade path, and local recovery/progress messaging without falling back to unrelated page sections.

## Notes
- Checkout defaults and provider behavior are configured under `StoryTime:Checkout`.
- The shipped default tier ladder is Trial (10 min / 1 concurrency / 30 min cooldown), Plus (12 min / 2 concurrency / 10 min cooldown), and Premium (15 min / 3 concurrency / 5 min cooldown).
