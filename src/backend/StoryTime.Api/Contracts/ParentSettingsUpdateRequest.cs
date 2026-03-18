namespace StoryTime.Api.Contracts;

public sealed record ParentSettingsUpdateRequest(
    string GateToken,
    bool NotificationsEnabled,
    bool AnalyticsEnabled,
    bool KidShelfEnabled = false);
