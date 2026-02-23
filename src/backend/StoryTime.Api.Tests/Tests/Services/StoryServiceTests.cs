using Microsoft.Extensions.Logging;
using Moq;
using StoryTime.Api.Services;

namespace StoryTime.Api.Tests.Tests.Services;

public class StoryServiceTests
{
    private readonly Mock<IOllamaService> _ollamaServiceMock;
    private readonly Mock<IConfigService> _configServiceMock;
    private readonly Mock<ILogger<StoryService>> _loggerMock;
    private readonly StoryService _service;

    public StoryServiceTests()
    {
        _ollamaServiceMock = new Mock<IOllamaService>();
        _configServiceMock = new Mock<IConfigService>();
        _loggerMock = new Mock<ILogger<StoryService>>();
        _service = new StoryService(
            _ollamaServiceMock.Object,
            _configServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Test_GenerateStory_ReturnsStoryResponse()
    {
        // Arrange
        var childName = "Alice";
        var childAge = 5;
        var theme = "adventure";
        var tierSlug = "plus";
        string? softUserId = "user123";

        // Setup config service mocks
        _configServiceMock.Setup(x => x.GetVariableAsync("text_model_story", tierSlug))
            .ReturnsAsync("llama3:8b-instruct");
        _configServiceMock.Setup(x => x.GetVariableAsync("text_model_outline", tierSlug))
            .ReturnsAsync("phi3:mini-instruct");
        _configServiceMock.Setup(x => x.GetVariableAsync("text_model_metadata", tierSlug))
            .ReturnsAsync("phi3:mini-instruct");

        // Setup ollama service mocks
        _ollamaServiceMock.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.Is<string>(p => p.Contains("story bible")),
                It.IsAny<string>()))
            .ReturnsAsync("Main character: Alice, a brave 5-year-old adventurer. Setting: Magical forest.");

        _ollamaServiceMock.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.Is<string>(p => p.Contains("scene outline")),
                It.IsAny<string>()))
            .ReturnsAsync(@"Scene 1: Alice enters the magical forest
Scene 2: She meets a talking rabbit
Scene 3: They solve a puzzle together
Scene 4: Alice returns home with new confidence");

        _ollamaServiceMock.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.Is<string>(p => p.Contains("Write scene")),
                It.IsAny<string>()))
            .ReturnsAsync("This is the scene text with exciting adventure content.");

        _ollamaServiceMock.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.Is<string>(p => p.Contains("complete children's story")),
                It.IsAny<string>()))
            .ReturnsAsync(@"TITLE: Alice's Magical Adventure
SUMMARY: Alice goes on an exciting journey through a magical forest where she meets new friends and learns to be brave.");

        // Act
        var result = await _service.GenerateStoryAsync(childName, childAge, theme, tierSlug, softUserId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Id);
        Assert.NotEmpty(result.Id);
    }

    [Fact]
    public async Task Test_GenerateStory_ContainsTitle()
    {
        // Arrange
        var childName = "Bob";
        var childAge = 7;
        var theme = "space";
        var tierSlug = "premium";
        string? softUserId = "user456";

        // Setup config service mocks
        _configServiceMock.Setup(x => x.GetVariableAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("test-model");

        // Setup ollama service mocks
        _ollamaServiceMock.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.Is<string>(p => p.Contains("story bible")),
                It.IsAny<string>()))
            .ReturnsAsync("Story bible content");

        _ollamaServiceMock.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.Is<string>(p => p.Contains("scene outline")),
                It.IsAny<string>()))
            .ReturnsAsync(@"Scene 1: Start
Scene 2: Middle
Scene 3: End");

        _ollamaServiceMock.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.Is<string>(p => p.Contains("Write scene")),
                It.IsAny<string>()))
            .ReturnsAsync("Scene content");

        _ollamaServiceMock.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.Is<string>(p => p.Contains("complete children's story")),
                It.IsAny<string>()))
            .ReturnsAsync(@"TITLE: Bob's Space Odyssey
SUMMARY: Bob explores the cosmos.");

        // Act
        var result = await _service.GenerateStoryAsync(childName, childAge, theme, tierSlug, softUserId);

        // Assert
        Assert.NotNull(result.Title);
        Assert.NotEmpty(result.Title);
        Assert.Equal("Bob's Space Odyssey", result.Title);
    }

    [Fact]
    public async Task Test_GenerateStory_ContainsScenes()
    {
        // Arrange
        var childName = "Charlie";
        var childAge = 6;
        var theme = "dinosaurs";
        var tierSlug = "trial";
        string? softUserId = null;

        // Setup config service mocks
        _configServiceMock.Setup(x => x.GetVariableAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("test-model");

        // Setup ollama service mocks
        _ollamaServiceMock.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.Is<string>(p => p.Contains("story bible")),
                It.IsAny<string>()))
            .ReturnsAsync("Story bible content");

        _ollamaServiceMock.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.Is<string>(p => p.Contains("scene outline")),
                It.IsAny<string>()))
            .ReturnsAsync(@"Scene 1: Charlie discovers dinosaur eggs
Scene 2: The eggs hatch
Scene 3: Charlie befriends baby dinosaurs");

        _ollamaServiceMock.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.Is<string>(p => p.Contains("Write scene")),
                It.IsAny<string>()))
            .ReturnsAsync("Exciting scene text about dinosaurs.");

        _ollamaServiceMock.Setup(x => x.GenerateTextAsync(
                It.IsAny<string>(),
                It.Is<string>(p => p.Contains("complete children's story")),
                It.IsAny<string>()))
            .ReturnsAsync(@"TITLE: Charlie and the Dinosaurs
SUMMARY: Charlie discovers dinosaur eggs.");

        // Act
        var result = await _service.GenerateStoryAsync(childName, childAge, theme, tierSlug, softUserId);

        // Assert
        Assert.NotNull(result.Scenes);
        Assert.NotEmpty(result.Scenes);
        Assert.All(result.Scenes, scene =>
        {
            Assert.NotEmpty(scene.Id);
            Assert.NotEmpty(scene.Text);
        });
    }
}
