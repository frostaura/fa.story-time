namespace StoryTime.Api.Contracts;

public sealed record StoryApprovalResponse(bool FullAudioReady, string? FullAudio);
