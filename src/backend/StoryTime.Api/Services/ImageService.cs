using System.Text;
using System.Text.Json;

namespace StoryTime.Api.Services;

public class ImageService : IImageService
{
    private readonly HttpClient _httpClient;
    private readonly IConfigService _configService;
    private readonly ILogger<ImageService> _logger;

    public ImageService(
        HttpClient httpClient,
        IConfigService configService,
        ILogger<ImageService> logger)
    {
        _httpClient = httpClient;
        _configService = configService;
        _logger = logger;
    }

    public async Task<string> GenerateImageAsync(string prompt, string style)
    {
        try
        {
            var imageEngineUrl = await _configService.GetVariableAsync("image_engine_url") ?? "http://image-engine:7860";
            var endpoint = $"{imageEngineUrl}/generate";

            var requestBody = new
            {
                prompt,
                style
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Calling Image Engine API at {Url} with style {Style}", endpoint, style);

            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (responseObject.TryGetProperty("image", out var imageData))
            {
                return imageData.GetString() ?? string.Empty;
            }

            _logger.LogWarning("Image Engine response did not contain 'image' field");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Image Engine API with style {Style}", style);
            throw;
        }
    }
}
