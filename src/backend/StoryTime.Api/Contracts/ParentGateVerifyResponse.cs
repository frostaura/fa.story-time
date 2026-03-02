namespace StoryTime.Api.Contracts;

public sealed record ParentGateVerifyResponse(string GateToken, DateTimeOffset ExpiresAt);
