namespace StoryTime.Api.Services;

public interface ITtsService
{
    Task<string> GenerateSpeechAsync(string text, string? voice = null);
}
