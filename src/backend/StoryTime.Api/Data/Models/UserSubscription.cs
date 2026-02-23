namespace StoryTime.Api.Data.Models;

/// <summary>
/// Represents a user's subscription to a tier.
/// </summary>
public class UserSubscription : BaseEntity
{
    public string SoftUserId { get; set; } = string.Empty;
    
    public string? ExternalSubId { get; set; }
    
    public Guid TierId { get; set; }
    
    public DateTime? TrialEndsAt { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    public bool IsActive { get; set; }
    
    // Navigation properties
    public Tier Tier { get; set; } = null!;
}
