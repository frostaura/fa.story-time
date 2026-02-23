using Microsoft.AspNetCore.Mvc;
using StoryTime.Api.Services;

namespace StoryTime.Api.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly IConfigService _configService;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(
        IConfigService configService,
        ILogger<ConfigController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    [HttpGet("tiers")]
    public async Task<IActionResult> GetTiers()
    {
        try
        {
            var tiers = await _configService.GetAllActiveTiersAsync();
            
            var response = tiers.Select(t => new
            {
                t.Id,
                t.Slug,
                t.DisplayName,
                t.Description,
                t.PriceMonthlyCents,
                t.PriceAnnualCents,
                t.Currency,
                t.BillingPeriod,
                Capabilities = t.TierCapabilities.Select(tc => new
                {
                    Key = tc.Capability.Key,
                    Label = tc.Capability.Label,
                    Value = tc.Value
                }).ToList()
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tiers");
            return StatusCode(500, new { error = "Failed to retrieve tiers" });
        }
    }

    [HttpGet("variables")]
    public async Task<IActionResult> GetVariables()
    {
        try
        {
            // For security, only return non-sensitive variables
            // This would need to be enhanced with a proper filtering mechanism
            return Ok(new { message = "Variables endpoint - implement filtering for sensitive data" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting variables");
            return StatusCode(500, new { error = "Failed to retrieve variables" });
        }
    }
}
