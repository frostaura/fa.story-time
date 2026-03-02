namespace StoryTime.Api.Contracts;

public sealed record HomeStatusResponse(
    bool QuickGenerateVisible,
    bool DurationSliderVisible,
    int DurationMinMinutes,
    int DurationMaxMinutes,
    int DurationDefaultMinutes,
    string DefaultChildName,
    bool ParentControlsEnabled,
    OneShotDefaultsResponse OneShotDefaults);

public sealed record OneShotDefaultsResponse(
    string ArcName,
    string CompanionName,
    string Setting,
    string Mood,
    string ThemeTrackId,
    string NarrationStyle);
