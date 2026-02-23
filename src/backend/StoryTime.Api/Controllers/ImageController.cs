using Microsoft.AspNetCore.Mvc;
using StoryTime.Api.Models;
using StoryTime.Api.Services;

namespace StoryTime.Api.Controllers;

[ApiController]
[Route("api/images")]
public class ImageController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly ILogger<ImageController> _logger;

    public ImageController(
        IImageService imageService,
        ILogger<ImageController> logger)
    {
        _imageService = imageService;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateImage([FromBody] GenerateImageRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { error = "Prompt is required" });
            }

            _logger.LogInformation("Generating image with style {Style}", request.Style);

            var imageBase64 = await _imageService.GenerateImageAsync(request.Prompt, request.Style);

            return Ok(new
            {
                image = imageBase64,
                prompt = request.Prompt,
                style = request.Style
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image");
            return StatusCode(500, new { error = "Failed to generate image" });
        }
    }
}
