namespace StoryTime.Api.Data.Models;

/// <summary>
/// Represents a configurable variable that can have tier-specific values.
/// </summary>
public class Variable : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    
    public string Label { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string DefaultValue { get; set; } = string.Empty;
    
    // Navigation properties
    public ICollection<TierVariable> TierVariables { get; set; } = new List<TierVariable>();
}
