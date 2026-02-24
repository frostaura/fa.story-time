using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TaleWeaver.Api.Services;

/// <summary>
/// OpenRouter API client for text completions and image generation.
/// </summary>
public class OpenRouterService : IOpenRouterService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenRouterService> _logger;

    public OpenRouterService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenRouterService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var baseUrl = _configuration["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1";
        _httpClient.BaseAddress = new Uri(baseUrl);

        var apiKey = _configuration["OpenRouter:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    public async Task<string> CompleteTextAsync(string systemPrompt, string userPrompt, string? model = null)
    {
        var selectedModel = model
            ?? _configuration["OpenRouter:TextModel"]
            ?? "anthropic/claude-3.5-sonnet";

        var fallbackModel = _configuration["OpenRouter:TextFallbackModel"]
            ?? "openai/gpt-4.1-mini";

        try
        {
            return await CallChatCompletionAsync(systemPrompt, userPrompt, selectedModel);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary model {Model} failed, falling back to {Fallback}",
                selectedModel, fallbackModel);

            return await CallChatCompletionAsync(systemPrompt, userPrompt, fallbackModel);
        }
    }

    public async Task<List<byte[]>> GenerateImagesAsync(List<string> prompts, string? model = null)
    {
        var selectedModel = model
            ?? _configuration["OpenRouter:ImageModel"]
            ?? "black-forest-labs/flux-1-dev";

        var fallbackModel = _configuration["OpenRouter:ImageFallbackModel"]
            ?? "black-forest-labs/flux-1-schnell";

        var results = new List<byte[]>();

        foreach (var prompt in prompts)
        {
            try
            {
                var imageBytes = await CallImageGenerationAsync(prompt, selectedModel);
                results.Add(imageBytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Primary image model failed for prompt, trying fallback");

                try
                {
                    var imageBytes = await CallImageGenerationAsync(prompt, fallbackModel);
                    results.Add(imageBytes);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback image model also failed");
                    results.Add([]);
                }
            }
        }

        return results;
    }

    private async Task<string> CallChatCompletionAsync(string systemPrompt, string userPrompt, string model)
    {
        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/v1/chat/completions", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        var messageContent = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return messageContent ?? string.Empty;
    }

    private async Task<byte[]> CallImageGenerationAsync(string prompt, string model)
    {
        var requestBody = new
        {
            model,
            prompt,
            n = 1,
            response_format = "b64_json"
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/v1/images/generations", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        var b64 = doc.RootElement
            .GetProperty("data")[0]
            .GetProperty("b64_json")
            .GetString();

        return b64 != null ? Convert.FromBase64String(b64) : [];
    }
}
