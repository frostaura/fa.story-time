namespace TaleWeaver.Api.Services;

/// <summary>
/// Coqui TTS client for speech synthesis.
/// </summary>
public class TtsService : ITtsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TtsService> _logger;

    public TtsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TtsService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var baseUrl = _configuration["Tts:BaseUrl"] ?? "http://localhost:5002";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<byte[]> SynthesizeAsync(string text, string? voiceId = null)
    {
        var selectedVoice = voiceId
            ?? _configuration["Tts:DefaultVoice"]
            ?? "p225";

        var queryParams = $"?text={Uri.EscapeDataString(text)}&speaker_id={Uri.EscapeDataString(selectedVoice)}";
        var requestUri = $"/api/tts{queryParams}";

        _logger.LogInformation("Synthesizing {Length} characters with voice {Voice}",
            text.Length, selectedVoice);

        var response = await _httpClient.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync();
    }
}
