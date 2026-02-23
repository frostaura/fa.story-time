using Microsoft.AspNetCore.Mvc;
using StoryTime.Api.Data;
using StoryTime.Api.Data.Models;

namespace StoryTime.Api.Controllers;

[ApiController]
[Route("api/push")]
public class PushController : ControllerBase
{
    private readonly StoryTimeDbContext _context;
    private readonly ILogger<PushController> _logger;

    public PushController(
        StoryTimeDbContext context,
        ILogger<PushController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscribeRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SoftUserId))
            {
                return BadRequest(new { error = "SoftUserId is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Endpoint))
            {
                return BadRequest(new { error = "Endpoint is required" });
            }

            var subscription = new PushSubscription
            {
                SoftUserId = request.SoftUserId,
                Endpoint = request.Endpoint,
                PublicKey = request.PublicKey ?? string.Empty,
                AuthSecret = request.AuthSecret ?? string.Empty
            };

            _context.PushSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Push subscription created for user {SoftUserId}", request.SoftUserId);

            return Ok(new { message = "Subscription created successfully", id = subscription.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating push subscription");
            return StatusCode(500, new { error = "Failed to create subscription" });
        }
    }
}

public class PushSubscribeRequest
{
    public string SoftUserId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string? PublicKey { get; set; }
    public string? AuthSecret { get; set; }
}
