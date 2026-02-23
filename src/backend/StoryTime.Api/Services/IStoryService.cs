using StoryTime.Api.Models;

namespace StoryTime.Api.Services;

public interface IStoryService
{
    Task<StoryResponse> GenerateStoryAsync(
        string childName, 
        int childAge, 
        string theme, 
        string tierSlug, 
        string? softUserId);
}
