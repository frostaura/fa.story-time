using System.Text.Json.Serialization;

namespace TaleWeaver.Api.DTOs;

/// <summary>
/// Request payload for story generation.
/// </summary>
public class GenerationRequest
{
    /// <summary>Unique correlation ID for tracking this generation.</summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>The child's name to personalize the story.</summary>
    public string ChildName { get; set; } = string.Empty;

    /// <summary>Age of the child (affects vocabulary and themes).</summary>
    public int Age { get; set; }

    /// <summary>Story theme (e.g., "adventure", "friendship", "space").</summary>
    public string Theme { get; set; } = string.Empty;

    /// <summary>Target duration in minutes (affects scene count).</summary>
    public int DurationMinutes { get; set; } = 5;

    /// <summary>Story length category: "short", "medium", or "long".</summary>
    public string Length { get; set; } = "short";

    /// <summary>Optional voice ID for TTS.</summary>
    public string? VoiceId { get; set; }

    /// <summary>Optional existing Story Bible for series continuity.</summary>
    public StoryBible? StoryBible { get; set; }

    /// <summary>Soft user ID (set by middleware).</summary>
    [JsonIgnore]
    public string SoftUserId { get; set; } = string.Empty;
}
