using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaleWeaver.Api.Data;

namespace TaleWeaver.Api.Controllers;

/// <summary>
/// Configuration endpoints (publicly accessible, no SoftUserId required).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly TaleWeaverDbContext _dbContext;

    public ConfigController(TaleWeaverDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get all subscription tier definitions.
    /// </summary>
    [HttpGet("tiers")]
    public async Task<IActionResult> GetTiers()
    {
        var tiers = await _dbContext.Tiers
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Concurrency,
                t.CooldownMinutes,
                t.AllowedLengths,
                t.HasLockScreenArt,
                t.HasLongStories,
                t.HasHighQualityBudget
            })
            .ToListAsync();

        return Ok(tiers);
    }

    /// <summary>
    /// Get all feature flags.
    /// </summary>
    [HttpGet("flags")]
    public async Task<IActionResult> GetFlags()
    {
        var flags = await _dbContext.FeatureFlags
            .Select(f => new
            {
                f.Key,
                f.Value,
                f.Description
            })
            .ToListAsync();

        return Ok(flags);
    }
}
