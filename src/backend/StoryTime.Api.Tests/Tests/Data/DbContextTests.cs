using Microsoft.EntityFrameworkCore;
using StoryTime.Api.Data;
using StoryTime.Api.Data.Models;

namespace StoryTime.Api.Tests.Tests.Data;

public class DbContextTests : IDisposable
{
    private readonly StoryTimeDbContext _context;

    public DbContextTests()
    {
        var options = new DbContextOptionsBuilder<StoryTimeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new StoryTimeDbContext(options);
    }

    [Fact]
    public async Task Test_CanCreateTier()
    {
        // Arrange
        var tier = new Tier
        {
            Slug = "test-tier",
            DisplayName = "Test Tier",
            Description = "A test tier for unit testing",
            PriceMonthlyCents = 999,
            PriceAnnualCents = 9999,
            Currency = "USD",
            BillingPeriod = "monthly",
            IsActive = true
        };

        // Act
        _context.Tiers.Add(tier);
        await _context.SaveChangesAsync();

        // Assert
        var savedTier = await _context.Tiers.FirstOrDefaultAsync(t => t.Slug == "test-tier");
        Assert.NotNull(savedTier);
        Assert.Equal("Test Tier", savedTier.DisplayName);
        Assert.Equal(999, savedTier.PriceMonthlyCents);
    }

    [Fact]
    public async Task Test_SoftDelete_FilterWorks()
    {
        // Arrange
        var tier1 = new Tier
        {
            Slug = "active-tier",
            DisplayName = "Active Tier",
            Description = "Active tier",
            PriceMonthlyCents = 999,
            PriceAnnualCents = 9999,
            Currency = "USD",
            BillingPeriod = "monthly",
            IsActive = true,
            IsDeleted = false
        };

        var tier2 = new Tier
        {
            Slug = "deleted-tier",
            DisplayName = "Deleted Tier",
            Description = "Deleted tier",
            PriceMonthlyCents = 999,
            PriceAnnualCents = 9999,
            Currency = "USD",
            BillingPeriod = "monthly",
            IsActive = true,
            IsDeleted = true
        };

        _context.Tiers.Add(tier1);
        _context.Tiers.Add(tier2);
        await _context.SaveChangesAsync();

        // Act
        var tiers = await _context.Tiers.ToListAsync();

        // Assert
        Assert.Single(tiers);
        Assert.Equal("active-tier", tiers[0].Slug);
        Assert.DoesNotContain(tiers, t => t.Slug == "deleted-tier");
    }

    [Fact]
    public async Task Test_AutoTimestamps_AreSet()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow.AddSeconds(-1);
        
        var tier = new Tier
        {
            Slug = "timestamp-tier",
            DisplayName = "Timestamp Tier",
            Description = "Testing timestamps",
            PriceMonthlyCents = 999,
            PriceAnnualCents = 9999,
            Currency = "USD",
            BillingPeriod = "monthly",
            IsActive = true
        };

        // Act
        _context.Tiers.Add(tier);
        await _context.SaveChangesAsync();
        var afterCreate = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.NotEqual(Guid.Empty, tier.Id);
        Assert.True(tier.CreatedAt >= beforeCreate);
        Assert.True(tier.CreatedAt <= afterCreate);
        Assert.Equal(tier.CreatedAt, tier.UpdatedAt);

        // Test update timestamp
        var beforeUpdate = DateTime.UtcNow.AddSeconds(-1);
        tier.DisplayName = "Updated Timestamp Tier";
        await _context.SaveChangesAsync();
        var afterUpdate = DateTime.UtcNow.AddSeconds(1);

        Assert.True(tier.UpdatedAt >= beforeUpdate);
        Assert.True(tier.UpdatedAt <= afterUpdate);
        Assert.True(tier.UpdatedAt > tier.CreatedAt);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
