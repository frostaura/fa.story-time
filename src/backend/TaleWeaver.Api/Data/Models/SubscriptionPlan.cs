namespace TaleWeaver.Api.Data.Models;

/// <summary>
/// A purchasable subscription plan linked to a tier and Stripe price.
/// </summary>
public class SubscriptionPlan : BaseEntity
{
    public Guid TierId { get; set; }
    public Tier Tier { get; set; } = null!;
    public string StripePriceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MonthlyPriceCents { get; set; }
    public int TrialDays { get; set; }
}
