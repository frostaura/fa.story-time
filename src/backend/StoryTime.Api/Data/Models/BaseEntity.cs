namespace StoryTime.Api.Data.Models;

/// <summary>
/// Base entity class with common audit fields for soft-delete pattern.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    
    public bool IsDeleted { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}
