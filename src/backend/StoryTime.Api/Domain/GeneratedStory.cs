namespace StoryTime.Api.Domain;

public sealed record GeneratedStory(
    string StoryId,
    string Title,
    string Mode,
    string? SeriesId,
    string Recap,
    IReadOnlyList<string> Scenes,
    IReadOnlyList<PosterLayer> PosterLayers,
    bool ApprovalRequired,
    string TeaserAudio,
    string? FullAudio,
    StoryBibleSnapshot? StoryBible,
    bool ReducedMotion,
    DateTimeOffset GeneratedAt);
