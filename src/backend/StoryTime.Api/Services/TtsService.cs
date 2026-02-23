using System.Text;
using System.Text.Json;

namespace StoryTime.Api.Services;

public class TtsService : ITtsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfigService _configService;
    private readonly ILogger<TtsService> _logger;

    public TtsService(
        HttpClient httpClient,
        IConfigService configService,
        ILogger<TtsService> logger)
    {
        _httpClient = httpClient;
        _configService = configService;
        _logger = logger;
    }

    public async Task<string> GenerateSpeechAsync(string text, string? voice = null)
    {
        try
        {
            var ttsEngineUrl = await _configService.GetVariableAsync("tts_engine_url") ?? "http://tts-engine:5500";
            var defaultVoice = await _configService.GetVariableAsync("tts_default_voice") ?? "en_US-lessac-medium";
            var endpoint = $"{ttsEngineUrl}/generate";

            var requestBody = new
            {
                text,
                voice = voice ?? defaultVoice
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Calling TTS Engine API at {Url} with voice {Voice}", endpoint, voice ?? defaultVoice);

            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (responseObject.TryGetProperty("audio", out var audioData))
            {
                return audioData.GetString() ?? string.Empty;
            }

            _logger.LogWarning("TTS Engine response did not contain 'audio' field");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling TTS Engine API with voice {Voice}", voice);
            throw;
        }
    }
}
