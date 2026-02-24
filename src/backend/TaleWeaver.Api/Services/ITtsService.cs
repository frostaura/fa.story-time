namespace TaleWeaver.Api.Services;

/// <summary>
/// Abstracts text-to-speech synthesis.
/// </summary>
public interface ITtsService
{
    /// <summary>
    /// Synthesize narration text to WAV audio bytes.
    /// </summary>
    Task<byte[]> SynthesizeAsync(string text, string? voiceId = null);
}
