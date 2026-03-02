namespace StoryTime.Api.Contracts;

public sealed record SubscriptionCheckoutSessionRequest(string GateToken, string? UpgradeTier);

public sealed record SubscriptionCheckoutSessionResponse(
    string SessionId,
    string CurrentTier,
    string UpgradeTier,
    string CheckoutUrl,
    DateTimeOffset ExpiresAt);

public sealed record SubscriptionCheckoutCompleteRequest(string GateToken, string SessionId);

public sealed record SubscriptionCheckoutCompleteResponse(string CurrentTier, string UpgradeTier);
