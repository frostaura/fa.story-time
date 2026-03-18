using StoryTime.Api.Domain;

namespace StoryTime.Api.Contracts;

public sealed record GenerateStoryRequest(
    string SoftUserId,
    string? ChildName,
    string Mode,
    int DurationMinutes,
    string? SeriesId,
    bool? ApprovalRequired,
    bool Favorite,
    bool ReducedMotion = false,
    OneShotCustomizationRequest? Customization = null,
    StoryBibleSnapshot? StoryBible = null);

public sealed record OneShotCustomizationRequest(
    string? ArcName,
    string? CompanionName,
    string? Setting,
    string? Mood,
    string? ThemeTrackId,
    string? NarrationStyle);
