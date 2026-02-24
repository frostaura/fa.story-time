namespace TaleWeaver.Api.Data.Models;

/// <summary>
/// Subscription tier defining rate limits and feature access.
/// </summary>
public class Tier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Concurrency { get; set; }
    public int CooldownMinutes { get; set; }
    public List<string> AllowedLengths { get; set; } = [];
    public bool HasLockScreenArt { get; set; }
    public bool HasLongStories { get; set; }
    public bool HasHighQualityBudget { get; set; }
}
