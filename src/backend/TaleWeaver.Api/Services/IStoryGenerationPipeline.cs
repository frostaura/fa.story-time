using TaleWeaver.Api.DTOs;

namespace TaleWeaver.Api.Services;

/// <summary>
/// 5-pass story generation pipeline.
/// </summary>
public interface IStoryGenerationPipeline
{
    /// <summary>
    /// Execute the full generation pipeline and return a complete story response.
    /// </summary>
    Task<GenerationResponse> GenerateAsync(GenerationRequest request, CancellationToken ct = default);
}
