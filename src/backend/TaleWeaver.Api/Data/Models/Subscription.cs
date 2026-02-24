namespace TaleWeaver.Api.Data.Models;

/// <summary>
/// Tracks a user's active subscription and its Stripe binding.
/// </summary>
public enum SubscriptionStatus
{
    Trialing,
    Active,
    PastDue,
    Canceled
}

public class Subscription : BaseEntity
{
    public string SoftUserId { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCustomerId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? TrialEnd { get; set; }
}
