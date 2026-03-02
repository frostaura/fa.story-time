namespace StoryTime.Api.Contracts;

public sealed record ParentGateChallengeResponse(
    string ChallengeId,
    string Challenge,
    string RpId,
    DateTimeOffset ExpiresAt);
