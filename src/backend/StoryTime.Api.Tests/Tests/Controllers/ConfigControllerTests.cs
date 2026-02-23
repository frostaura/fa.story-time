using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StoryTime.Api.Controllers;
using StoryTime.Api.Data.Models;
using StoryTime.Api.Services;

namespace StoryTime.Api.Tests.Tests.Controllers;

public class ConfigControllerTests
{
    private readonly Mock<IConfigService> _configServiceMock;
    private readonly Mock<ILogger<ConfigController>> _loggerMock;
    private readonly ConfigController _controller;

    public ConfigControllerTests()
    {
        _configServiceMock = new Mock<IConfigService>();
        _loggerMock = new Mock<ILogger<ConfigController>>();
        _controller = new ConfigController(_configServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Test_GetTiers_ReturnsOk()
    {
        // Arrange
        var tier1 = new Tier
        {
            Id = Guid.NewGuid(),
            Slug = "trial",
            DisplayName = "Trial",
            Description = "Trial tier",
            PriceMonthlyCents = 0,
            PriceAnnualCents = 0,
            Currency = "USD",
            BillingPeriod = "monthly",
            IsActive = true,
            TierCapabilities = new List<TierCapability>
            {
                new TierCapability
                {
                    Id = Guid.NewGuid(),
                    Capability = new Capability
                    {
                        Id = Guid.NewGuid(),
                        Key = "stories_per_day",
                        Label = "Stories Per Day",
                        Description = "Number of stories per day"
                    },
                    Value = "3"
                }
            }
        };

        var tier2 = new Tier
        {
            Id = Guid.NewGuid(),
            Slug = "plus",
            DisplayName = "Plus",
            Description = "Plus tier",
            PriceMonthlyCents = 999,
            PriceAnnualCents = 9999,
            Currency = "USD",
            BillingPeriod = "monthly",
            IsActive = true,
            TierCapabilities = new List<TierCapability>
            {
                new TierCapability
                {
                    Id = Guid.NewGuid(),
                    Capability = new Capability
                    {
                        Id = Guid.NewGuid(),
                        Key = "stories_per_day",
                        Label = "Stories Per Day",
                        Description = "Number of stories per day"
                    },
                    Value = "10"
                }
            }
        };

        _configServiceMock.Setup(x => x.GetAllActiveTiersAsync())
            .ReturnsAsync(new List<Tier> { tier1, tier2 });

        // Act
        var result = await _controller.GetTiers();

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Test_GetVariables_ReturnsOk()
    {
        // Arrange
        // No specific setup needed for this endpoint

        // Act
        var result = await _controller.GetVariables();

        // Assert
        Assert.NotNull(result);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}
