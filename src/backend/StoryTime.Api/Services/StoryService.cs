using System.Text.Json;
using System.Text.RegularExpressions;
using StoryTime.Api.Models;

namespace StoryTime.Api.Services;

public class StoryService : IStoryService
{
    private const int MaxMetadataTextLength = 2000;
    
    private readonly IOllamaService _ollamaService;
    private readonly IConfigService _configService;
    private readonly ILogger<StoryService> _logger;

    public StoryService(
        IOllamaService ollamaService,
        IConfigService configService,
        ILogger<StoryService> logger)
    {
        _ollamaService = ollamaService;
        _configService = configService;
        _logger = logger;
    }

    public async Task<StoryResponse> GenerateStoryAsync(
        string childName,
        int childAge,
        string theme,
        string tierSlug,
        string? softUserId)
    {
        try
        {
            _logger.LogInformation(
                "Generating story for {ChildName}, age {ChildAge}, theme {Theme}, tier {TierSlug}",
                childName, childAge, theme, tierSlug);

            // Get model configurations
            var storyModel = await _configService.GetVariableAsync("text_model_story", tierSlug) ?? "llama3:8b-instruct";
            var outlineModel = await _configService.GetVariableAsync("text_model_outline", tierSlug) ?? "phi3:mini-instruct";
            var metadataModel = await _configService.GetVariableAsync("text_model_metadata", tierSlug) ?? "phi3:mini-instruct";

            // Step 1: Create story bible
            _logger.LogInformation("Step 1: Creating story bible");
            var bible = await CreateStoryBibleAsync(childName, childAge, theme, storyModel);

            // Step 2: Create scene outline
            _logger.LogInformation("Step 2: Creating scene outline");
            var scenes = await CreateSceneOutlineAsync(childName, childAge, theme, bible, outlineModel);

            // Step 3: Generate scene text
            _logger.LogInformation("Step 3: Generating scene text for {Count} scenes", scenes.Count);
            await GenerateSceneTextsAsync(scenes, childName, bible, storyModel);

            // Step 4: Generate summary and title
            _logger.LogInformation("Step 4: Generating title and summary");
            var fullText = string.Join("\n\n", scenes.Select(s => s.Text));
            var (title, summary) = await GenerateMetadataAsync(fullText, childName, metadataModel);

            var response = new StoryResponse
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Summary = summary,
                Text = fullText,
                Scenes = scenes,
                CreatedAt = DateTime.UtcNow.ToString("o")
            };

            _logger.LogInformation("Story generation completed successfully: {Title}", title);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating story for {ChildName}", childName);
            throw;
        }
    }

    private async Task<string> CreateStoryBibleAsync(string childName, int childAge, string theme, string model)
    {
        var prompt = $@"Create a story bible for a children's story with the following parameters:
- Child name: {childName}
- Child age: {childAge}
- Theme: {theme}

The story bible should include:
1. Main character traits and personality
2. Setting description
3. Key supporting characters
4. Moral or lesson
5. Tone and style guidelines

Keep it concise but informative.";

        var systemPrompt = "You are a creative children's story writer. Create engaging, age-appropriate content.";

        return await _ollamaService.GenerateTextAsync(model, prompt, systemPrompt);
    }

    private async Task<List<SceneResponse>> CreateSceneOutlineAsync(
        string childName,
        int childAge,
        string theme,
        string bible,
        string model)
    {
        var prompt = $@"Based on this story bible, create an outline of 3-5 scenes for a children's story:

STORY BIBLE:
{bible}

For each scene, provide:
- Scene number
- Brief summary (one sentence)

Format your response as:
Scene 1: [summary]
Scene 2: [summary]
etc.";

        var systemPrompt = "You are a children's story writer creating scene outlines.";
        var outlineText = await _ollamaService.GenerateTextAsync(model, prompt, systemPrompt);

        // Parse the outline
        var scenes = new List<SceneResponse>();
        var scenePattern = new Regex(@"Scene\s+(\d+):\s*(.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        var matches = scenePattern.Matches(outlineText);

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            scenes.Add(new SceneResponse
            {
                Id = Guid.NewGuid().ToString(),
                Order = i + 1,
                Summary = match.Groups[2].Value.Trim(),
                Text = string.Empty
            });
        }

        // If parsing failed, create default scenes
        if (scenes.Count == 0)
        {
            _logger.LogWarning("Failed to parse scene outline, using defaults");
            scenes = new List<SceneResponse>
            {
                new SceneResponse { Id = Guid.NewGuid().ToString(), Order = 1, Summary = "Introduction", Text = string.Empty },
                new SceneResponse { Id = Guid.NewGuid().ToString(), Order = 2, Summary = "Adventure begins", Text = string.Empty },
                new SceneResponse { Id = Guid.NewGuid().ToString(), Order = 3, Summary = "Challenge faced", Text = string.Empty },
                new SceneResponse { Id = Guid.NewGuid().ToString(), Order = 4, Summary = "Resolution", Text = string.Empty }
            };
        }

        return scenes;
    }

    private async Task GenerateSceneTextsAsync(
        List<SceneResponse> scenes,
        string childName,
        string bible,
        string model)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            var prompt = $@"Write scene {scene.Order} of a children's story based on this bible and summary:

STORY BIBLE:
{bible}

SCENE SUMMARY:
{scene.Summary}

Write 3-5 paragraphs of engaging, age-appropriate narrative. Make it vivid and exciting.
This is scene {scene.Order} of {scenes.Count}.";

            var systemPrompt = "You are a children's story writer. Write engaging, vivid narrative suitable for young readers.";
            scene.Text = await _ollamaService.GenerateTextAsync(model, prompt, systemPrompt);
        }
    }

    private async Task<(string title, string summary)> GenerateMetadataAsync(
        string fullText,
        string childName,
        string model)
    {
        var prompt = $@"Based on this complete children's story, generate:
1. A catchy, short title (under 60 characters)
2. A one-paragraph summary

STORY:
{fullText.Substring(0, Math.Min(MaxMetadataTextLength, fullText.Length))}...

Format your response as:
TITLE: [title here]
SUMMARY: [summary here]";

        var systemPrompt = "You are a children's story editor creating titles and summaries.";
        var response = await _ollamaService.GenerateTextAsync(model, prompt, systemPrompt);

        // Parse title and summary
        var titleMatch = Regex.Match(response, @"TITLE:\s*(.+?)(?:\n|$)", RegexOptions.IgnoreCase);
        var summaryMatch = Regex.Match(response, @"SUMMARY:\s*(.+?)(?:\n\n|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        var title = titleMatch.Success 
            ? titleMatch.Groups[1].Value.Trim() 
            : $"{childName}'s Adventure";

        var summary = summaryMatch.Success 
            ? summaryMatch.Groups[1].Value.Trim() 
            : "A wonderful adventure story.";

        return (title, summary);
    }
}
