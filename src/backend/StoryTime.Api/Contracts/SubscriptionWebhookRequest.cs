namespace StoryTime.Api.Contracts;

public sealed record SubscriptionWebhookRequest(string SoftUserId, string Tier, bool ResetCooldown);
