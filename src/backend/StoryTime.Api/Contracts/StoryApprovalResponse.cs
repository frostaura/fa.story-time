namespace StoryTime.Api.Contracts;

public sealed record StoryApprovalRequest(string SoftUserId, string GateToken);

public sealed record StoryApprovalResponse(bool FullAudioReady, string? FullAudio);
