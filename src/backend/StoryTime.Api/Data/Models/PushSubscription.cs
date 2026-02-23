namespace StoryTime.Api.Data.Models;

/// <summary>
/// Represents a user's web push notification subscription.
/// </summary>
public class PushSubscription
{
    public Guid Id { get; set; }
    
    public string SoftUserId { get; set; } = string.Empty;
    
    public string Endpoint { get; set; } = string.Empty;
    
    public string PublicKey { get; set; } = string.Empty;
    
    public string AuthSecret { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public bool IsDeleted { get; set; }
}
