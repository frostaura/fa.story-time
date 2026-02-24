namespace TaleWeaver.Api.Services;

/// <summary>
/// Abstracts OpenRouter API for text completion and image generation.
/// </summary>
public interface IOpenRouterService
{
    /// <summary>
    /// Complete a text prompt using the configured LLM.
    /// </summary>
    Task<string> CompleteTextAsync(string systemPrompt, string userPrompt, string? model = null);

    /// <summary>
    /// Generate images from a list of prompts.
    /// </summary>
    Task<List<byte[]>> GenerateImagesAsync(List<string> prompts, string? model = null);
}
