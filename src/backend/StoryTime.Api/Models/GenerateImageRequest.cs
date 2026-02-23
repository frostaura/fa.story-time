namespace StoryTime.Api.Models;

public class GenerateImageRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string Style { get; set; } = "storybook-lowpoly";
}
