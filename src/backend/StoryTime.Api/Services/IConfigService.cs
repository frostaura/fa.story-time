using StoryTime.Api.Data.Models;

namespace StoryTime.Api.Services;

public interface IConfigService
{
    Task<string?> GetVariableAsync(string key, string? tierSlug = null);
    Task<string?> GetCapabilityAsync(string tierSlug, string capabilityKey);
    Task<Tier?> GetTierAsync(string slug);
    Task<List<Tier>> GetAllActiveTiersAsync();
}
