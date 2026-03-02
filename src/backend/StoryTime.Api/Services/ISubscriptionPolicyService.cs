namespace StoryTime.Api.Services;

public interface ISubscriptionPolicyService
{
    PolicyDecision TryStartGeneration(string softUserId, int durationMinutes, DateTimeOffset now);

    void CompleteGeneration(string softUserId, Guid reservationId, DateTimeOffset now);

    bool ApplyWebhook(string softUserId, string tier, bool resetCooldown);

    PaywallInfo GetPaywallInfo(string softUserId);

    CheckoutSession? CreateCheckoutSession(string softUserId, string? upgradeTier, DateTimeOffset now);

    CheckoutCompletion? CompleteCheckoutSession(string softUserId, string sessionId, DateTimeOffset now);
}

public sealed record PolicyDecision(bool Allowed, int StatusCode, string Message, Guid ReservationId);

public sealed record PaywallInfo(string CurrentTier, int MaxDurationMinutes, string UpgradeTier, string UpgradeUrl);

public sealed record CheckoutSession(
    string SessionId,
    string CurrentTier,
    string UpgradeTier,
    string CheckoutUrl,
    DateTimeOffset ExpiresAt);

public sealed record CheckoutCompletion(string CurrentTier, string UpgradeTier);
