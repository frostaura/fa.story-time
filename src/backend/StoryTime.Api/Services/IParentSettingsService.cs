using StoryTime.Api.Contracts;

namespace StoryTime.Api.Services;

public interface IParentSettingsService
{
    bool RegisterCredential(string softUserId, string credentialId, string publicKey);

    ParentGateChallenge CreateChallenge(string softUserId, DateTimeOffset now);

    ParentGateSession? VerifyGate(string softUserId, string challengeId, ParentGateAssertion assertion, DateTimeOffset now);

    bool IsGateAuthorized(string softUserId, string gateToken, DateTimeOffset now);

    ParentSettingsSnapshot? GetSettings(string softUserId, string gateToken, DateTimeOffset now);

    ParentSettingsSnapshot? UpdateSettings(string softUserId, string gateToken, bool notificationsEnabled, bool analyticsEnabled, DateTimeOffset now);
}

public sealed record ParentGateChallenge(string ChallengeId, string Challenge, string RpId, DateTimeOffset ExpiresAt);

public sealed record ParentGateSession(string GateToken, DateTimeOffset ExpiresAt);

public sealed record ParentSettingsSnapshot(bool NotificationsEnabled, bool AnalyticsEnabled);
