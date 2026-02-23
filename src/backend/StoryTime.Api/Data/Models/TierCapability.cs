namespace StoryTime.Api.Data.Models;

/// <summary>
/// Junction table linking tiers to capabilities with optional values.
/// </summary>
public class TierCapability : BaseEntity
{
    public Guid TierId { get; set; }
    
    public Guid CapabilityId { get; set; }
    
    public string? Value { get; set; }
    
    // Navigation properties
    public Tier Tier { get; set; } = null!;
    
    public Capability Capability { get; set; } = null!;
}
