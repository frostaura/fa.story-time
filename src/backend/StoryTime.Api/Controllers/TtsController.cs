using Microsoft.AspNetCore.Mvc;
using StoryTime.Api.Models;
using StoryTime.Api.Services;

namespace StoryTime.Api.Controllers;

[ApiController]
[Route("api/tts")]
public class TtsController : ControllerBase
{
    private readonly ITtsService _ttsService;
    private readonly ILogger<TtsController> _logger;

    public TtsController(
        ITtsService ttsService,
        ILogger<TtsController> logger)
    {
        _ttsService = ttsService;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateSpeech([FromBody] GenerateSpeechRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest(new { error = "Text is required" });
            }

            _logger.LogInformation("Generating speech with voice {Voice}", request.Voice ?? "default");

            var audioBase64 = await _ttsService.GenerateSpeechAsync(request.Text, request.Voice);

            return Ok(new
            {
                audio = audioBase64,
                text = request.Text,
                voice = request.Voice
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating speech");
            return StatusCode(500, new { error = "Failed to generate speech" });
        }
    }
}
