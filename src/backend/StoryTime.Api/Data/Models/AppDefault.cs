namespace StoryTime.Api.Data.Models;

/// <summary>
/// Represents application-wide default configuration values.
/// </summary>
public class AppDefault : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    
    public string Value { get; set; } = string.Empty;
}
