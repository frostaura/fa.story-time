using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StoryTime.Api.Data;
using StoryTime.Api.Data.Models;
using StoryTime.Api.Services;

namespace StoryTime.Api.Tests.Tests.Services;

public class ConfigServiceTests : IDisposable
{
    private readonly StoryTimeDbContext _context;
    private readonly ConfigService _service;
    private readonly Mock<ILogger<ConfigService>> _loggerMock;

    public ConfigServiceTests()
    {
        // Setup InMemory database
        var options = new DbContextOptionsBuilder<StoryTimeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new StoryTimeDbContext(options);
        _loggerMock = new Mock<ILogger<ConfigService>>();
        _service = new ConfigService(_context, _loggerMock.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create a test variable
        var variable = new Variable
        {
            Id = Guid.NewGuid(),
            Key = "test_variable",
            Label = "Test Variable",
            Description = "A test variable",
            DefaultValue = "default_value",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Variables.Add(variable);

        // Create a test tier
        var tier = new Tier
        {
            Id = Guid.NewGuid(),
            Slug = "test-tier",
            DisplayName = "Test Tier",
            Description = "A test tier",
            PriceMonthlyCents = 999,
            PriceAnnualCents = 9999,
            Currency = "USD",
            BillingPeriod = "monthly",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Tiers.Add(tier);

        // Create a tier variable override
        var tierVariable = new TierVariable
        {
            Id = Guid.NewGuid(),
            TierId = tier.Id,
            VariableId = variable.Id,
            Value = "tier_override_value",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tier = tier,
            Variable = variable
        };
        _context.TierVariables.Add(tierVariable);

        // Create a test capability
        var capability = new Capability
        {
            Id = Guid.NewGuid(),
            Key = "test_capability",
            Label = "Test Capability",
            Description = "A test capability",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Capabilities.Add(capability);

        // Create a tier capability
        var tierCapability = new TierCapability
        {
            Id = Guid.NewGuid(),
            TierId = tier.Id,
            CapabilityId = capability.Id,
            Value = "enabled",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tier = tier,
            Capability = capability
        };
        _context.TierCapabilities.Add(tierCapability);

        _context.SaveChanges();
    }

    [Fact]
    public async Task Test_GetVariable_ReturnsDefaultValue_WhenNoOverride()
    {
        // Arrange
        var key = "test_variable";

        // Act
        var result = await _service.GetVariableAsync(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("default_value", result);
    }

    [Fact]
    public async Task Test_GetVariable_ReturnsTierOverride_WhenExists()
    {
        // Arrange
        var key = "test_variable";
        var tierSlug = "test-tier";

        // Act
        var result = await _service.GetVariableAsync(key, tierSlug);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("tier_override_value", result);
    }

    [Fact]
    public async Task Test_GetTier_ReturnsTier_WhenSlugExists()
    {
        // Arrange
        var slug = "test-tier";

        // Act
        var result = await _service.GetTierAsync(slug);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-tier", result.Slug);
        Assert.Equal("Test Tier", result.DisplayName);
    }

    [Fact]
    public async Task Test_GetTier_ReturnsNull_WhenSlugNotFound()
    {
        // Arrange
        var slug = "non-existent-tier";

        // Act
        var result = await _service.GetTierAsync(slug);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Test_GetCapability_ReturnsValue_WhenExists()
    {
        // Arrange
        var tierSlug = "test-tier";
        var capabilityKey = "test_capability";

        // Act
        var result = await _service.GetCapabilityAsync(tierSlug, capabilityKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("enabled", result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
