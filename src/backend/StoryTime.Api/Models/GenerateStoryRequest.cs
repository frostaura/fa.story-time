namespace StoryTime.Api.Models;

public class GenerateStoryRequest
{
    public string ChildName { get; set; } = string.Empty;
    public int ChildAge { get; set; } = 6;
    public string Theme { get; set; } = "adventure";
    public string TierSlug { get; set; } = "trial";
    public string? SoftUserId { get; set; }
}
