namespace StoryTime.Api.Data.Models;

/// <summary>
/// Represents a feature capability that can be associated with tiers.
/// </summary>
public class Capability : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    
    public string Label { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    // Navigation properties
    public ICollection<TierCapability> TierCapabilities { get; set; } = new List<TierCapability>();
}
