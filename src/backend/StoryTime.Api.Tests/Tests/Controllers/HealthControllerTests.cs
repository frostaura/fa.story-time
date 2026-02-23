using Microsoft.AspNetCore.Mvc;
using StoryTime.Api.Controllers;

namespace StoryTime.Api.Tests.Tests.Controllers;

public class HealthControllerTests
{
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _controller = new HealthController();
    }

    [Fact]
    public void Test_GetHealth_ReturnsHealthyStatus()
    {
        // Arrange
        // No arrangement needed for this simple test

        // Act
        var result = _controller.GetHealth();

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        // Use reflection to access anonymous type properties
        var valueType = okResult.Value.GetType();
        var statusProperty = valueType.GetProperty("status");
        var timestampProperty = valueType.GetProperty("timestamp");
        
        Assert.NotNull(statusProperty);
        Assert.NotNull(timestampProperty);
        
        var status = statusProperty.GetValue(okResult.Value);
        var timestamp = timestampProperty.GetValue(okResult.Value);
        
        Assert.Equal("healthy", status);
        Assert.NotNull(timestamp);
    }
}
