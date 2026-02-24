using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TaleWeaver.Api.DTOs;
using TaleWeaver.Api.Services;

namespace TaleWeaver.Api.Tests.Services;

/// <summary>
/// Unit tests for the 5-pass StoryGenerationPipeline.
/// </summary>
public class StoryGenerationPipelineTests
{
    private readonly Mock<IOpenRouterService> _openRouterMock;
    private readonly Mock<ITtsService> _ttsMock;
    private readonly Mock<ILogger<StoryGenerationPipeline>> _loggerMock;
    private readonly StoryGenerationPipeline _pipeline;

    public StoryGenerationPipelineTests()
    {
        _openRouterMock = new Mock<IOpenRouterService>();
        _ttsMock = new Mock<ITtsService>();
        _loggerMock = new Mock<ILogger<StoryGenerationPipeline>>();

        _pipeline = new StoryGenerationPipeline(
            _openRouterMock.Object,
            _ttsMock.Object,
            _loggerMock.Object);
    }

    private static GenerationRequest CreateDefaultRequest() => new()
    {
        CorrelationId = "test-123",
        ChildName = "Luna",
        Age = 5,
        Theme = "adventure",
        DurationMinutes = 5,
        Length = "short",
        SoftUserId = "user-abc"
    };

    // ---------------------------------------------------------------
    // Pass 1: Outline
    // ---------------------------------------------------------------

    [Fact]
    public async Task GenerateOutlineAsync_ShouldCallOpenRouterWithCorrectPrompts()
    {
        // Arrange
        _openRouterMock
            .Setup(x => x.CompleteTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync("""{"beats":["intro","climax","resolution"],"theme":"adventure","ending":"happy"}""");

        var request = CreateDefaultRequest();

        // Act
        var result = await _pipeline.GenerateOutlineAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("beats");
        _openRouterMock.Verify(
            x => x.CompleteTextAsync(
                It.Is<string>(s => s.Contains("story architect")),
                It.Is<string>(s => s.Contains("Luna")),
                It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateOutlineAsync_WithStoryBible_ShouldIncludeBibleContext()
    {
        // Arrange
        _openRouterMock
            .Setup(x => x.CompleteTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync("{}");

        var request = CreateDefaultRequest();
        request.StoryBible = new StoryBible
        {
            Characters = [new CharacterEntry { Name = "Starry", Role = "sidekick" }],
            EpisodeNumber = 2
        };

        // Act
        await _pipeline.GenerateOutlineAsync(request, CancellationToken.None);

        // Assert
        _openRouterMock.Verify(
            x => x.CompleteTextAsync(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Story Bible")),
                It.IsAny<string?>()),
            Times.Once);
    }

    // ---------------------------------------------------------------
    // Pass 2: Scene Plan
    // ---------------------------------------------------------------

    [Fact]
    public async Task GenerateScenePlanAsync_ShouldParseValidSceneJson()
    {
        // Arrange
        var sceneJson = """
            [
                {
                    "sceneNumber": 1,
                    "setting": "forest",
                    "mood": "wonder",
                    "goal": "introduce character",
                    "conflictLevel": 2,
                    "continuityFacts": ["Luna has a red hat"],
                    "visualLayerPrompts": ["a forest glade"],
                    "musicTrackQuery": "gentle forest ambience",
                    "sfxKeywords": ["birds", "wind"]
                }
            ]
            """;

        _openRouterMock
            .Setup(x => x.CompleteTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(sceneJson);

        var request = CreateDefaultRequest();

        // Act
        var scenes = await _pipeline.GenerateScenePlanAsync(request, "{}", CancellationToken.None);

        // Assert
        scenes.Should().HaveCount(1);
        scenes[0].Setting.Should().Be("forest");
        scenes[0].Mood.Should().Be("wonder");
        scenes[0].VisualLayerPrompts.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GenerateScenePlanAsync_WithInvalidJson_ShouldReturnEmptyList()
    {
        // Arrange
        _openRouterMock
            .Setup(x => x.CompleteTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync("not valid json at all");

        var request = CreateDefaultRequest();

        // Act
        var scenes = await _pipeline.GenerateScenePlanAsync(request, "{}", CancellationToken.None);

        // Assert
        scenes.Should().BeEmpty();
    }

    // ---------------------------------------------------------------
    // Pass 3: Scene Batch
    // ---------------------------------------------------------------

    [Fact]
    public async Task GenerateSceneBatchAsync_ShouldGenerateNarrationForEachScene()
    {
        // Arrange
        var scenes = new List<ScenePlan>
        {
            new() { SceneNumber = 1, Setting = "forest", Mood = "wonder", Goal = "intro" },
            new() { SceneNumber = 2, Setting = "cave", Mood = "tension", Goal = "climax" },
            new() { SceneNumber = 3, Setting = "home", Mood = "calm", Goal = "resolution" }
        };

        _openRouterMock
            .Setup(x => x.CompleteTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync("Once upon a time...");

        var request = CreateDefaultRequest();

        // Act
        var result = await _pipeline.GenerateSceneBatchAsync(request, scenes, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(s => s.NarrationText.Should().NotBeNullOrEmpty());
        _openRouterMock.Verify(
            x => x.CompleteTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task GenerateSceneBatchAsync_FinalScene_ShouldRequestCalmWindDown()
    {
        // Arrange
        var scenes = new List<ScenePlan>
        {
            new() { SceneNumber = 1, Setting = "forest", Mood = "wonder", Goal = "intro" },
            new() { SceneNumber = 2, Setting = "home", Mood = "calm", Goal = "resolution" }
        };

        _openRouterMock
            .Setup(x => x.CompleteTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync("Narration text");

        var request = CreateDefaultRequest();

        // Act
        await _pipeline.GenerateSceneBatchAsync(request, scenes, CancellationToken.None);

        // Assert — the final scene prompt should include wind-down instructions
        _openRouterMock.Verify(
            x => x.CompleteTextAsync(
                It.Is<string>(s => s.Contains("FINAL scene")),
                It.IsAny<string>(),
                It.IsAny<string?>()),
            Times.Once);
    }

    // ---------------------------------------------------------------
    // Pass 4: Stitch
    // ---------------------------------------------------------------

    [Fact]
    public async Task StitchScenesAsync_ShouldJoinAllSceneNarrations()
    {
        // Arrange
        var scenes = new List<ScenePlan>
        {
            new() { SceneNumber = 1, NarrationText = "Scene one text." },
            new() { SceneNumber = 2, NarrationText = "Scene two text." }
        };

        _openRouterMock
            .Setup(x => x.CompleteTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync("Stitched story text.");

        var request = CreateDefaultRequest();

        // Act
        var result = await _pipeline.StitchScenesAsync(request, scenes, CancellationToken.None);

        // Assert
        result.Should().NotBeNullOrEmpty();
        _openRouterMock.Verify(
            x => x.CompleteTextAsync(
                It.Is<string>(s => s.Contains("story editor")),
                It.Is<string>(s => s.Contains("Scene one") && s.Contains("Scene two")),
                It.IsAny<string?>()),
            Times.Once);
    }

    // ---------------------------------------------------------------
    // Pass 5: Polish
    // ---------------------------------------------------------------

    [Fact]
    public async Task PolishTextAsync_ShouldCallOpenRouterWithQualityCheckPrompt()
    {
        // Arrange
        _openRouterMock
            .Setup(x => x.CompleteTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync("Polished story text.");

        var request = CreateDefaultRequest();

        // Act
        var result = await _pipeline.PolishTextAsync(request, "Raw story text.", CancellationToken.None);

        // Assert
        result.Should().Be("Polished story text.");
        _openRouterMock.Verify(
            x => x.CompleteTextAsync(
                It.Is<string>(s => s.Contains("quality checker")),
                It.IsAny<string>(),
                It.IsAny<string?>()),
            Times.Once);
    }

    // ---------------------------------------------------------------
    // Full Pipeline
    // ---------------------------------------------------------------

    [Fact]
    public async Task GenerateAsync_ShouldExecuteAllFivePasses()
    {
        // Arrange
        var callCount = 0;
        _openRouterMock
            .Setup(x => x.CompleteTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount switch
                {
                    1 => """{"beats":["intro"],"theme":"adventure","ending":"happy"}""",
                    2 => """[{"sceneNumber":1,"setting":"forest","mood":"wonder","goal":"intro","conflictLevel":2,"continuityFacts":[],"visualLayerPrompts":[],"musicTrackQuery":"","sfxKeywords":[]}]""",
                    _ => "Generated text content."
                };
            });

        _openRouterMock
            .Setup(x => x.GenerateImagesAsync(It.IsAny<List<string>>(), It.IsAny<string?>()))
            .ReturnsAsync([]);

        _ttsMock
            .Setup(x => x.SynthesizeAsync(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        var request = CreateDefaultRequest();

        // Act
        var response = await _pipeline.GenerateAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.CorrelationId.Should().Be("test-123");
        response.NarrationText.Should().NotBeNullOrEmpty();
        response.AudioData.Should().NotBeNull();
        response.GenerationTimeMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GenerateAsync_WhenTtsFails_ShouldReturnTextOnly()
    {
        // Arrange
        _openRouterMock
            .Setup(x => x.CompleteTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync("""[{"sceneNumber":1,"setting":"f","mood":"w","goal":"i","conflictLevel":0,"continuityFacts":[],"visualLayerPrompts":[],"musicTrackQuery":"","sfxKeywords":[]}]""");

        _openRouterMock
            .Setup(x => x.GenerateImagesAsync(It.IsAny<List<string>>(), It.IsAny<string?>()))
            .ReturnsAsync([]);

        _ttsMock
            .Setup(x => x.SynthesizeAsync(It.IsAny<string>(), It.IsAny<string?>()))
            .ThrowsAsync(new HttpRequestException("TTS service unavailable"));

        var request = CreateDefaultRequest();

        // Act
        var response = await _pipeline.GenerateAsync(request);

        // Assert
        response.NarrationText.Should().NotBeNullOrEmpty();
        response.AudioData.Should().BeNull();
    }
}
