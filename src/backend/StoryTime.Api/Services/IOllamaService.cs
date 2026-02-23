namespace StoryTime.Api.Services;

public interface IOllamaService
{
    Task<string> GenerateTextAsync(string model, string prompt, string? systemPrompt = null);
}
