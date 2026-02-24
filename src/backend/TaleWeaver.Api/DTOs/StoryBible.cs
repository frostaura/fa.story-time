namespace TaleWeaver.Api.DTOs;

/// <summary>
/// Persistent story bible for series-mode continuity across episodes.
/// </summary>
public class StoryBible
{
    /// <summary>Main character definitions and traits.</summary>
    public List<CharacterEntry> Characters { get; set; } = [];

    /// <summary>Recurring locations and settings.</summary>
    public List<string> Locations { get; set; } = [];

    /// <summary>Established facts that must remain consistent.</summary>
    public List<string> EstablishedFacts { get; set; } = [];

    /// <summary>Ongoing plot threads to continue.</summary>
    public List<string> PlotThreads { get; set; } = [];

    /// <summary>The overall series theme.</summary>
    public string? SeriesTheme { get; set; }

    /// <summary>Episode number in the series.</summary>
    public int EpisodeNumber { get; set; }
}

/// <summary>
/// A character in the Story Bible.
/// </summary>
public class CharacterEntry
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<string> Traits { get; set; } = [];
    public string? Description { get; set; }
}
