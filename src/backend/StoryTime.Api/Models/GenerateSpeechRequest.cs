namespace StoryTime.Api.Models;

public class GenerateSpeechRequest
{
    public string Text { get; set; } = string.Empty;
    public string? Voice { get; set; }
}
