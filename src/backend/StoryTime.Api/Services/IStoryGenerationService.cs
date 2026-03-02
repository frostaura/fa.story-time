using StoryTime.Api.Contracts;
using StoryTime.Api.Domain;

namespace StoryTime.Api.Services;

public interface IStoryGenerationService
{
    Task<GeneratedStory> GenerateAsync(GenerateStoryRequest request, CancellationToken cancellationToken);
}
