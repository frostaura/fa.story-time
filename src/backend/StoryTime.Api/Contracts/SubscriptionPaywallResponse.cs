namespace StoryTime.Api.Contracts;

public sealed record SubscriptionPaywallResponse(
    string CurrentTier,
    string UpgradeTier,
    int MaxDurationMinutes,
    string UpgradeUrl,
    string Message);
