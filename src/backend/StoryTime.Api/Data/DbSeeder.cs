using Microsoft.EntityFrameworkCore;
using StoryTime.Api.Data.Models;

namespace StoryTime.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(StoryTimeDbContext context, ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting database seeding...");

            // Seed Capabilities
            await SeedCapabilitiesAsync(context, logger);

            // Seed Variables
            await SeedVariablesAsync(context, logger);

            // Seed Tiers
            await SeedTiersAsync(context, logger);

            // Seed TierCapabilities
            await SeedTierCapabilitiesAsync(context, logger);

            // Seed AppDefaults
            await SeedAppDefaultsAsync(context, logger);

            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }

    private static async Task SeedCapabilitiesAsync(StoryTimeDbContext context, ILogger logger)
    {
        var capabilities = new[]
        {
            new { Key = "concurrent_generations", Label = "Concurrent Generations", Description = "Number of stories that can be generated simultaneously" },
            new { Key = "priority_queue", Label = "Priority Queue", Description = "Access to priority generation queue" },
            new { Key = "max_child_profiles", Label = "Max Child Profiles", Description = "Maximum number of child profiles" },
            new { Key = "lockscreen_poster", Label = "Lockscreen Poster", Description = "Generate lockscreen poster images" },
            new { Key = "long_story_enabled", Label = "Long Story Enabled", Description = "Enable generation of longer stories" }
        };

        foreach (var cap in capabilities)
        {
            if (!await context.Capabilities.AnyAsync(c => c.Key == cap.Key))
            {
                context.Capabilities.Add(new Capability
                {
                    Key = cap.Key,
                    Label = cap.Label,
                    Description = cap.Description
                });
                logger.LogInformation("Seeded capability: {Key}", cap.Key);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedVariablesAsync(StoryTimeDbContext context, ILogger logger)
    {
        var variables = new[]
        {
            new { Key = "trial_days", Label = "Trial Days", Description = "Number of trial days", DefaultValue = "7" },
            new { Key = "trial_data_retention_days", Label = "Trial Data Retention Days", Description = "Days to retain trial data", DefaultValue = "14" },
            new { Key = "plus_cooldown_minutes", Label = "Plus Cooldown Minutes", Description = "Cooldown between generations for plus tier", DefaultValue = "30" },
            new { Key = "premium_cooldown_minutes", Label = "Premium Cooldown Minutes", Description = "Cooldown between generations for premium tier", DefaultValue = "15" },
            new { Key = "max_cached_stories", Label = "Max Cached Stories", Description = "Maximum number of cached stories", DefaultValue = "20" },
            new { Key = "story_cache_days", Label = "Story Cache Days", Description = "Days to cache stories", DefaultValue = "14" },
            new { Key = "favorites_never_expire", Label = "Favorites Never Expire", Description = "Whether favorited stories never expire", DefaultValue = "true" },
            new { Key = "text_model_story", Label = "Text Model Story", Description = "Model for story generation", DefaultValue = "llama3:8b-instruct" },
            new { Key = "text_model_outline", Label = "Text Model Outline", Description = "Model for outline generation", DefaultValue = "phi3:mini-instruct" },
            new { Key = "text_model_metadata", Label = "Text Model Metadata", Description = "Model for metadata generation", DefaultValue = "phi3:mini-instruct" },
            new { Key = "image_engine_url", Label = "Image Engine URL", Description = "URL for image generation service", DefaultValue = "http://image-engine:7860" },
            new { Key = "image_model_lowpoly", Label = "Image Model Lowpoly", Description = "Model for lowpoly image generation", DefaultValue = "dreamshaper" },
            new { Key = "tts_engine_url", Label = "TTS Engine URL", Description = "URL for TTS service", DefaultValue = "http://tts-engine:5500" },
            new { Key = "tts_enabled", Label = "TTS Enabled", Description = "Whether TTS is enabled", DefaultValue = "true" },
            new { Key = "tts_default_voice", Label = "TTS Default Voice", Description = "Default voice for TTS", DefaultValue = "en_US-lessac-medium" },
            new { Key = "ollama_url", Label = "Ollama URL", Description = "URL for Ollama service", DefaultValue = "http://ollama:11434" },
            new { Key = "logging_level", Label = "Logging Level", Description = "Application logging level", DefaultValue = "info" }
        };

        foreach (var variable in variables)
        {
            if (!await context.Variables.AnyAsync(v => v.Key == variable.Key))
            {
                context.Variables.Add(new Variable
                {
                    Key = variable.Key,
                    Label = variable.Label,
                    Description = variable.Description,
                    DefaultValue = variable.DefaultValue
                });
                logger.LogInformation("Seeded variable: {Key}", variable.Key);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedTiersAsync(StoryTimeDbContext context, ILogger logger)
    {
        var tiers = new[]
        {
            new { Slug = "trial", DisplayName = "Trial", Description = "Free trial tier", PriceMonthlyCents = 0, PriceAnnualCents = 0, Currency = "USD", BillingPeriod = "trial", IsActive = true },
            new { Slug = "plus", DisplayName = "Plus", Description = "Plus subscription tier", PriceMonthlyCents = 499, PriceAnnualCents = 4990, Currency = "USD", BillingPeriod = "monthly", IsActive = true },
            new { Slug = "premium", DisplayName = "Premium", Description = "Premium subscription tier", PriceMonthlyCents = 999, PriceAnnualCents = 9990, Currency = "USD", BillingPeriod = "monthly", IsActive = true }
        };

        foreach (var tier in tiers)
        {
            if (!await context.Tiers.AnyAsync(t => t.Slug == tier.Slug))
            {
                context.Tiers.Add(new Tier
                {
                    Slug = tier.Slug,
                    DisplayName = tier.DisplayName,
                    Description = tier.Description,
                    PriceMonthlyCents = tier.PriceMonthlyCents,
                    PriceAnnualCents = tier.PriceAnnualCents,
                    Currency = tier.Currency,
                    BillingPeriod = tier.BillingPeriod,
                    IsActive = tier.IsActive
                });
                logger.LogInformation("Seeded tier: {Slug}", tier.Slug);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedTierCapabilitiesAsync(StoryTimeDbContext context, ILogger logger)
    {
        var tierCapabilities = new[]
        {
            // Trial tier
            new { TierSlug = "trial", CapabilityKey = "concurrent_generations", Value = "1" },
            new { TierSlug = "trial", CapabilityKey = "priority_queue", Value = "false" },
            new { TierSlug = "trial", CapabilityKey = "max_child_profiles", Value = "1" },
            new { TierSlug = "trial", CapabilityKey = "lockscreen_poster", Value = "false" },
            new { TierSlug = "trial", CapabilityKey = "long_story_enabled", Value = "false" },
            
            // Plus tier
            new { TierSlug = "plus", CapabilityKey = "concurrent_generations", Value = "2" },
            new { TierSlug = "plus", CapabilityKey = "priority_queue", Value = "false" },
            new { TierSlug = "plus", CapabilityKey = "max_child_profiles", Value = "3" },
            new { TierSlug = "plus", CapabilityKey = "lockscreen_poster", Value = "true" },
            new { TierSlug = "plus", CapabilityKey = "long_story_enabled", Value = "true" },
            
            // Premium tier
            new { TierSlug = "premium", CapabilityKey = "concurrent_generations", Value = "5" },
            new { TierSlug = "premium", CapabilityKey = "priority_queue", Value = "true" },
            new { TierSlug = "premium", CapabilityKey = "max_child_profiles", Value = "unlimited" },
            new { TierSlug = "premium", CapabilityKey = "lockscreen_poster", Value = "true" },
            new { TierSlug = "premium", CapabilityKey = "long_story_enabled", Value = "true" }
        };

        foreach (var tc in tierCapabilities)
        {
            var tier = await context.Tiers.FirstOrDefaultAsync(t => t.Slug == tc.TierSlug);
            var capability = await context.Capabilities.FirstOrDefaultAsync(c => c.Key == tc.CapabilityKey);

            if (tier != null && capability != null)
            {
                if (!await context.TierCapabilities.AnyAsync(x => x.TierId == tier.Id && x.CapabilityId == capability.Id))
                {
                    context.TierCapabilities.Add(new TierCapability
                    {
                        TierId = tier.Id,
                        CapabilityId = capability.Id,
                        Value = tc.Value
                    });
                    logger.LogInformation("Seeded tier capability: {Tier}.{Capability}", tc.TierSlug, tc.CapabilityKey);
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedAppDefaultsAsync(StoryTimeDbContext context, ILogger logger)
    {
        var appDefaults = new[]
        {
            new { Key = "ollama_url", Value = "http://ollama:11434" },
            new { Key = "image_engine_url", Value = "http://image-engine:7860" },
            new { Key = "tts_engine_url", Value = "http://tts-engine:5500" },
            new { Key = "logging_level", Value = "info" }
        };

        foreach (var appDefault in appDefaults)
        {
            if (!await context.AppDefaults.AnyAsync(ad => ad.Key == appDefault.Key))
            {
                context.AppDefaults.Add(new AppDefault
                {
                    Key = appDefault.Key,
                    Value = appDefault.Value
                });
                logger.LogInformation("Seeded app default: {Key}", appDefault.Key);
            }
        }

        await context.SaveChangesAsync();
    }
}
