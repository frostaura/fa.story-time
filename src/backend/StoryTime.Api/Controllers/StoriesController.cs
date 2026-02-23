using Microsoft.AspNetCore.Mvc;
using StoryTime.Api.Models;
using StoryTime.Api.Services;

namespace StoryTime.Api.Controllers;

[ApiController]
[Route("api/stories")]
public class StoriesController : ControllerBase
{
    private readonly IStoryService _storyService;
    private readonly ILogger<StoriesController> _logger;

    public StoriesController(
        IStoryService storyService,
        ILogger<StoriesController> logger)
    {
        _storyService = storyService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<StoryResponse>> GenerateStory([FromBody] GenerateStoryRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ChildName))
            {
                return BadRequest(new { error = "Child name is required" });
            }

            if (request.ChildAge < 1 || request.ChildAge > 18)
            {
                return BadRequest(new { error = "Child age must be between 1 and 18" });
            }

            _logger.LogInformation(
                "Generating story for child {ChildName}, age {ChildAge}, theme {Theme}",
                request.ChildName, request.ChildAge, request.Theme);

            var story = await _storyService.GenerateStoryAsync(
                request.ChildName,
                request.ChildAge,
                request.Theme,
                request.TierSlug,
                request.SoftUserId);

            return Ok(story);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating story");
            return StatusCode(500, new { error = "Failed to generate story" });
        }
    }
}
