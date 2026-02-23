using Microsoft.EntityFrameworkCore;
using StoryTime.Api.Data;
using StoryTime.Api.Data.Models;

namespace StoryTime.Api.Services;

public class ConfigService : IConfigService
{
    private readonly StoryTimeDbContext _context;
    private readonly ILogger<ConfigService> _logger;

    public ConfigService(StoryTimeDbContext context, ILogger<ConfigService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> GetVariableAsync(string key, string? tierSlug = null)
    {
        try
        {
            // If tier is provided, try tier-specific value first
            if (!string.IsNullOrEmpty(tierSlug))
            {
                var tier = await _context.Tiers
                    .Include(t => t.TierVariables)
                    .ThenInclude(tv => tv.Variable)
                    .FirstOrDefaultAsync(t => t.Slug == tierSlug);

                if (tier != null)
                {
                    var tierVariable = tier.TierVariables
                        .FirstOrDefault(tv => tv.Variable.Key == key);
                    
                    if (tierVariable != null)
                    {
                        return tierVariable.Value;
                    }
                }
            }

            // Fall back to variable default value
            var variable = await _context.Variables
                .FirstOrDefaultAsync(v => v.Key == key);

            if (variable != null)
            {
                return variable.DefaultValue;
            }

            // Fall back to app default
            var appDefault = await _context.AppDefaults
                .FirstOrDefaultAsync(ad => ad.Key == key);

            return appDefault?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting variable {Key} for tier {TierSlug}", key, tierSlug);
            return null;
        }
    }

    public async Task<string?> GetCapabilityAsync(string tierSlug, string capabilityKey)
    {
        try
        {
            var tier = await _context.Tiers
                .Include(t => t.TierCapabilities)
                .ThenInclude(tc => tc.Capability)
                .FirstOrDefaultAsync(t => t.Slug == tierSlug);

            if (tier == null)
            {
                return null;
            }

            var tierCapability = tier.TierCapabilities
                .FirstOrDefault(tc => tc.Capability.Key == capabilityKey);

            return tierCapability?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting capability {Key} for tier {TierSlug}", capabilityKey, tierSlug);
            return null;
        }
    }

    public async Task<Tier?> GetTierAsync(string slug)
    {
        try
        {
            return await _context.Tiers
                .Include(t => t.TierCapabilities)
                .ThenInclude(tc => tc.Capability)
                .Include(t => t.TierVariables)
                .ThenInclude(tv => tv.Variable)
                .FirstOrDefaultAsync(t => t.Slug == slug);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tier {Slug}", slug);
            return null;
        }
    }

    public async Task<List<Tier>> GetAllActiveTiersAsync()
    {
        try
        {
            return await _context.Tiers
                .Where(t => t.IsActive)
                .Include(t => t.TierCapabilities)
                .ThenInclude(tc => tc.Capability)
                .Include(t => t.TierVariables)
                .ThenInclude(tv => tv.Variable)
                .OrderBy(t => t.PriceMonthlyCents)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all active tiers");
            return new List<Tier>();
        }
    }
}
