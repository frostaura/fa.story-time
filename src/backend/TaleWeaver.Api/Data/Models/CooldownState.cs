namespace TaleWeaver.Api.Data.Models;

/// <summary>
/// Tracks generation cooldown per user to enforce rate limits.
/// </summary>
public class CooldownState : BaseEntity
{
    public string SoftUserId { get; set; } = string.Empty;
    public DateTime LastGenerationAt { get; set; }
    public Guid TierId { get; set; }
    public Tier Tier { get; set; } = null!;
}
