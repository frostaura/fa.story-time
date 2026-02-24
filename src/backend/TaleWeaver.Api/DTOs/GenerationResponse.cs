namespace TaleWeaver.Api.DTOs;

/// <summary>
/// Response payload from story generation pipeline.
/// </summary>
public class GenerationResponse
{
    /// <summary>Correlation ID matching the request.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>The fully stitched and polished narration text.</summary>
    public string NarrationText { get; set; } = string.Empty;

    /// <summary>Audio bytes from TTS synthesis (WAV).</summary>
    public byte[]? AudioData { get; set; }

    /// <summary>Scene plans used during generation.</summary>
    public List<ScenePlan> Scenes { get; set; } = [];

    /// <summary>Updated Story Bible for series continuity.</summary>
    public StoryBible? UpdatedStoryBible { get; set; }

    /// <summary>Image data for generated scene illustrations.</summary>
    public List<byte[]> Images { get; set; } = [];

    /// <summary>Total generation time in milliseconds.</summary>
    public long GenerationTimeMs { get; set; }
}
