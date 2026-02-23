namespace StoryTime.Api.Models;

public class StoryResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<SceneResponse> Scenes { get; set; } = new();
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
}

public class SceneResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int Order { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
