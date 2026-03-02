namespace StoryTime.Api.Domain;

public sealed record StoryBibleSnapshot(
    string SeriesId,
    string VisualIdentity,
    string RecurringCharacter,
    string ArcName,
    int ArcEpisodeNumber,
    string ArcObjective,
    string PreviousEpisodeSummary,
    AudioAnchorMetadata AudioAnchorMetadata);
