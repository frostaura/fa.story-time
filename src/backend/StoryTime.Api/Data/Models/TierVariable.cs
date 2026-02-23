namespace StoryTime.Api.Data.Models;

/// <summary>
/// Junction table linking tiers to variables with specific values.
/// </summary>
public class TierVariable : BaseEntity
{
    public Guid TierId { get; set; }
    
    public Guid VariableId { get; set; }
    
    public string Value { get; set; } = string.Empty;
    
    // Navigation properties
    public Tier Tier { get; set; } = null!;
    
    public Variable Variable { get; set; } = null!;
}
