namespace StoryTime.Api.Data.Models;

/// <summary>
/// Represents an API request log entry for monitoring and analytics.
/// </summary>
public class ApiLog
{
    public Guid Id { get; set; }
    
    public string Method { get; set; } = string.Empty;
    
    public string Path { get; set; } = string.Empty;
    
    public int StatusCode { get; set; }
    
    public long DurationMs { get; set; }
    
    public string? SoftUserId { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
