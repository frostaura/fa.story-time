namespace StoryTime.Api.Data.Models;

/// <summary>
/// Represents an AI provider usage log entry for cost tracking and monitoring.
/// </summary>
public class AiLog
{
    public Guid Id { get; set; }
    
    public string Provider { get; set; } = string.Empty;
    
    public string Model { get; set; } = string.Empty;
    
    public int PromptTokens { get; set; }
    
    public int CompletionTokens { get; set; }
    
    public long DurationMs { get; set; }
    
    public string? SoftUserId { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
