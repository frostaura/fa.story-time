using System.Text;
using System.Text.Json;

namespace StoryTime.Api.Services;

public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfigService _configService;
    private readonly ILogger<OllamaService> _logger;

    public OllamaService(
        HttpClient httpClient,
        IConfigService configService,
        ILogger<OllamaService> logger)
    {
        _httpClient = httpClient;
        _configService = configService;
        _logger = logger;
    }

    public async Task<string> GenerateTextAsync(string model, string prompt, string? systemPrompt = null)
    {
        try
        {
            var ollamaUrl = await _configService.GetVariableAsync("ollama_url") ?? "http://ollama:11434";
            var endpoint = $"{ollamaUrl}/api/generate";

            var requestBody = new
            {
                model,
                prompt,
                system = systemPrompt ?? string.Empty,
                stream = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Calling Ollama API at {Url} with model {Model}", endpoint, model);

            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (responseObject.TryGetProperty("response", out var responseText))
            {
                return responseText.GetString() ?? string.Empty;
            }

            _logger.LogWarning("Ollama response did not contain 'response' field");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Ollama API with model {Model}", model);
            throw;
        }
    }
}
