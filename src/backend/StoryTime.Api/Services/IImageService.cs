namespace StoryTime.Api.Services;

public interface IImageService
{
    Task<string> GenerateImageAsync(string prompt, string style);
}
