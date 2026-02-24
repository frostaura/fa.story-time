namespace TaleWeaver.Api.Data.Models;

/// <summary>
/// Runtime feature flag for toggling capabilities.
/// </summary>
public class FeatureFlag : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
