namespace StoryTime.Api.Domain;

public sealed class StoryBible
{
    public required string SeriesId { get; init; }

    public required string VisualIdentity { get; init; }

    public required string RecurringCharacter { get; init; }

    public required AudioAnchorMetadata AudioAnchorMetadata { get; init; }

    public string ArcName { get; set; } = "";

    public int ArcEpisodeNumber { get; set; }

    public string ArcObjective { get; set; } = "";

    public List<string> ContinuityFacts { get; } = new();

    public string LastEpisodeSummary { get; set; } = "";
}

public sealed record AudioAnchorMetadata(string ThemeTrackId, string NarrationStyle);
