namespace TaleWeaver.Api.Data.Models;

/// <summary>
/// Base entity providing common audit fields for all domain objects.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
