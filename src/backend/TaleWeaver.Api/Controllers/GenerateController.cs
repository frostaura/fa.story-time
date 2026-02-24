using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaleWeaver.Api.Data;
using TaleWeaver.Api.Data.Models;
using TaleWeaver.Api.DTOs;
using TaleWeaver.Api.Services;

namespace TaleWeaver.Api.Controllers;

/// <summary>
/// Story generation endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GenerateController : ControllerBase
{
    private readonly IStoryGenerationPipeline _pipeline;
    private readonly TaleWeaverDbContext _dbContext;
    private readonly ILogger<GenerateController> _logger;

    // In-memory tracking for concurrent generations (replace with Redis in production)
    private static readonly Dictionary<string, GenerationResponse> CompletedGenerations = new();
    private static readonly HashSet<string> ActiveGenerations = [];

    public GenerateController(
        IStoryGenerationPipeline pipeline,
        TaleWeaverDbContext dbContext,
        ILogger<GenerateController> logger)
    {
        _pipeline = pipeline;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Generate a new bedtime story.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<GenerationResponse>> Generate(
        [FromBody] GenerationRequest request,
        CancellationToken ct)
    {
        var softUserId = HttpContext.Items["SoftUserId"]?.ToString();
        if (string.IsNullOrEmpty(softUserId))
            return BadRequest(new { error = "SoftUserId is required" });

        request.SoftUserId = softUserId;

        // Check cooldown
        var cooldown = await _dbContext.CooldownStates
            .Include(cs => cs.Tier)
            .FirstOrDefaultAsync(cs => cs.SoftUserId == softUserId, ct);

        if (cooldown != null)
        {
            var cooldownEnd = cooldown.LastGenerationAt.AddMinutes(cooldown.Tier.CooldownMinutes);
            if (DateTime.UtcNow < cooldownEnd)
            {
                var remainingSeconds = (int)(cooldownEnd - DateTime.UtcNow).TotalSeconds;
                return StatusCode(StatusCodes.Status429TooManyRequests, new
                {
                    error = "Cooldown active",
                    retryAfterSeconds = remainingSeconds
                });
            }
        }

        // Check concurrency
        int activeCount;
        lock (ActiveGenerations)
        {
            activeCount = ActiveGenerations.Count(id => id.StartsWith(softUserId));
        }

        var subscription = await _dbContext.Subscriptions
            .Include(s => s.Plan)
            .ThenInclude(p => p.Tier)
            .FirstOrDefaultAsync(s => s.SoftUserId == softUserId, ct);

        var maxConcurrency = subscription?.Plan.Tier.Concurrency ?? 1;

        if (activeCount >= maxConcurrency)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                error = "Maximum concurrent generations reached",
                limit = maxConcurrency
            });
        }

        var generationKey = $"{softUserId}:{request.CorrelationId}";

        lock (ActiveGenerations)
        {
            ActiveGenerations.Add(generationKey);
        }

        try
        {
            var response = await _pipeline.GenerateAsync(request, ct);

            // Update cooldown
            if (cooldown == null)
            {
                var tierId = subscription?.Plan.TierId
                    ?? new Guid("00000000-0000-0000-0000-000000000001"); // Trial tier

                cooldown = new CooldownState
                {
                    SoftUserId = softUserId,
                    LastGenerationAt = DateTime.UtcNow,
                    TierId = tierId
                };
                _dbContext.CooldownStates.Add(cooldown);
            }
            else
            {
                cooldown.LastGenerationAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(ct);

            lock (CompletedGenerations)
            {
                CompletedGenerations[request.CorrelationId] = response;
            }

            return Ok(response);
        }
        finally
        {
            lock (ActiveGenerations)
            {
                ActiveGenerations.Remove(generationKey);
            }
        }
    }

    /// <summary>
    /// Check the status of a generation by correlation ID.
    /// </summary>
    [HttpGet("status/{correlationId}")]
    public ActionResult GetStatus(string correlationId)
    {
        lock (ActiveGenerations)
        {
            if (ActiveGenerations.Any(id => id.EndsWith($":{correlationId}")))
            {
                return Ok(new { status = "generating", correlationId });
            }
        }

        lock (CompletedGenerations)
        {
            if (CompletedGenerations.TryGetValue(correlationId, out var response))
            {
                return Ok(new { status = "completed", correlationId, response });
            }
        }

        return NotFound(new { status = "not_found", correlationId });
    }
}
