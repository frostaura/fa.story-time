namespace TaleWeaver.Api.DTOs;

/// <summary>
/// A single scene in the story generation pipeline.
/// </summary>
public class ScenePlan
{
    /// <summary>Scene number (1-based).</summary>
    public int SceneNumber { get; set; }

    /// <summary>Setting description for this scene.</summary>
    public string Setting { get; set; } = string.Empty;

    /// <summary>Emotional mood/tone (e.g., "wonder", "tension", "calm").</summary>
    public string Mood { get; set; } = string.Empty;

    /// <summary>Narrative goal for this scene.</summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>Conflict intensity level (0-10).</summary>
    public int ConflictLevel { get; set; }

    /// <summary>Facts that must stay consistent in this scene.</summary>
    public List<string> ContinuityFacts { get; set; } = [];

    /// <summary>Visual layer prompts for image generation (3-5 per scene).</summary>
    public List<string> VisualLayerPrompts { get; set; } = [];

    /// <summary>Search query for background music.</summary>
    public string MusicTrackQuery { get; set; } = string.Empty;

    /// <summary>Keywords for sound effects.</summary>
    public List<string> SfxKeywords { get; set; } = [];

    /// <summary>Generated narration text for this scene (filled in pass 3).</summary>
    public string NarrationText { get; set; } = string.Empty;
}
