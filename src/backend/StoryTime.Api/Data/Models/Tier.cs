namespace StoryTime.Api.Data.Models;

/// <summary>
/// Represents a subscription tier (e.g., trial, plus, premium).
/// </summary>
public class Tier : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    
    public string DisplayName { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public int PriceMonthlyCents { get; set; }
    
    public int PriceAnnualCents { get; set; }
    
    public string Currency { get; set; } = string.Empty;
    
    public string BillingPeriod { get; set; } = string.Empty;
    
    public bool IsActive { get; set; }
    
    // Navigation properties
    public ICollection<TierCapability> TierCapabilities { get; set; } = new List<TierCapability>();
    
    public ICollection<TierVariable> TierVariables { get; set; } = new List<TierVariable>();
    
    public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}
