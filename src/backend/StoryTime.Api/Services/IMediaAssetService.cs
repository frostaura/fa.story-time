using StoryTime.Api.Domain;

namespace StoryTime.Api.Services;

public interface IMediaAssetService
{
    IReadOnlyList<PosterLayer> BuildPosterLayers(string seed, bool reducedMotion);

    string BuildAudioDataUri(string storyId, int durationSeconds, double amplitudeScale);
}
