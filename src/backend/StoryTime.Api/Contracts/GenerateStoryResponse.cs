using StoryTime.Api.Domain;

namespace StoryTime.Api.Contracts;

public sealed record GenerateStoryResponse(
    string StoryId,
    string Title,
    string Mode,
    string? SeriesId,
    string Recap,
    IReadOnlyList<string> Scenes,
    int SceneCount,
    IReadOnlyList<PosterLayer> PosterLayers,
    bool ApprovalRequired,
    string TeaserAudio,
    string? FullAudio,
    bool FullAudioReady,
    StoryBibleSnapshot? StoryBible,
    bool ReducedMotion,
    DateTimeOffset GeneratedAt);
