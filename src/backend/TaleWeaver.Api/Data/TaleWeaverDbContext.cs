using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TaleWeaver.Api.Data.Models;

namespace TaleWeaver.Api.Data;

/// <summary>
/// EF Core database context for TaleWeaver with PostgreSQL.
/// </summary>
public class TaleWeaverDbContext : DbContext
{
    public TaleWeaverDbContext(DbContextOptions<TaleWeaverDbContext> options)
        : base(options) { }

    public DbSet<Tier> Tiers => Set<Tier>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<AppConfig> AppConfigs => Set<AppConfig>();
    public DbSet<CooldownState> CooldownStates => Set<CooldownState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global query filter for soft deletes
        modelBuilder.Entity<Tier>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SubscriptionPlan>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Subscription>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FeatureFlag>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AppConfig>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CooldownState>().HasQueryFilter(e => !e.IsDeleted);

        // PascalCase table names
        modelBuilder.Entity<Tier>().ToTable("Tiers");
        modelBuilder.Entity<SubscriptionPlan>().ToTable("SubscriptionPlans");
        modelBuilder.Entity<Subscription>().ToTable("Subscriptions");
        modelBuilder.Entity<FeatureFlag>().ToTable("FeatureFlags");
        modelBuilder.Entity<AppConfig>().ToTable("AppConfigs");
        modelBuilder.Entity<CooldownState>().ToTable("CooldownStates");

        // Tier -> AllowedLengths stored as JSON
        modelBuilder.Entity<Tier>()
            .Property(t => t.AllowedLengths)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(
                new ValueComparer<List<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        // SubscriptionPlan -> Tier relationship
        modelBuilder.Entity<SubscriptionPlan>()
            .HasOne(sp => sp.Tier)
            .WithMany()
            .HasForeignKey(sp => sp.TierId)
            .OnDelete(DeleteBehavior.Restrict);

        // Subscription -> Plan relationship
        modelBuilder.Entity<Subscription>()
            .HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // CooldownState -> Tier relationship
        modelBuilder.Entity<CooldownState>()
            .HasOne(cs => cs.Tier)
            .WithMany()
            .HasForeignKey(cs => cs.TierId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes on SoftUserId
        modelBuilder.Entity<Subscription>()
            .HasIndex(s => s.SoftUserId)
            .HasDatabaseName("IX_Subscriptions_SoftUserId");

        modelBuilder.Entity<CooldownState>()
            .HasIndex(cs => cs.SoftUserId)
            .HasDatabaseName("IX_CooldownStates_SoftUserId");

        // Unique index on FeatureFlag.Key
        modelBuilder.Entity<FeatureFlag>()
            .HasIndex(f => f.Key)
            .IsUnique()
            .HasDatabaseName("IX_FeatureFlags_Key");

        // Unique index on AppConfig.Key
        modelBuilder.Entity<AppConfig>()
            .HasIndex(a => a.Key)
            .IsUnique()
            .HasDatabaseName("IX_AppConfigs_Key");

        // Convert SubscriptionStatus enum to string
        modelBuilder.Entity<Subscription>()
            .Property(s => s.Status)
            .HasConversion<string>();

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Tier IDs (deterministic for seed data)
        var trialTierId = new Guid("00000000-0000-0000-0000-000000000001");
        var plusTierId = new Guid("00000000-0000-0000-0000-000000000002");
        var premiumTierId = new Guid("00000000-0000-0000-0000-000000000003");

        var now = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Tier>().HasData(
            new Tier
            {
                Id = trialTierId,
                Name = "Trial",
                Concurrency = 1,
                CooldownMinutes = 30,
                AllowedLengths = ["short"],
                HasLockScreenArt = false,
                HasLongStories = false,
                HasHighQualityBudget = false,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            },
            new Tier
            {
                Id = plusTierId,
                Name = "Plus",
                Concurrency = 2,
                CooldownMinutes = 10,
                AllowedLengths = ["short", "medium"],
                HasLockScreenArt = true,
                HasLongStories = false,
                HasHighQualityBudget = false,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            },
            new Tier
            {
                Id = premiumTierId,
                Name = "Premium",
                Concurrency = 3,
                CooldownMinutes = 5,
                AllowedLengths = ["short", "medium", "long"],
                HasLockScreenArt = true,
                HasLongStories = true,
                HasHighQualityBudget = true,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            });

        modelBuilder.Entity<SubscriptionPlan>().HasData(
            new SubscriptionPlan
            {
                Id = new Guid("00000000-0000-0000-0000-000000000011"),
                TierId = trialTierId,
                StripePriceId = "",
                Name = "Trial",
                MonthlyPriceCents = 0,
                TrialDays = 7,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            },
            new SubscriptionPlan
            {
                Id = new Guid("00000000-0000-0000-0000-000000000012"),
                TierId = plusTierId,
                StripePriceId = "price_plus_monthly",
                Name = "Plus Monthly",
                MonthlyPriceCents = 999,
                TrialDays = 7,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            },
            new SubscriptionPlan
            {
                Id = new Guid("00000000-0000-0000-0000-000000000013"),
                TierId = premiumTierId,
                StripePriceId = "price_premium_monthly",
                Name = "Premium Monthly",
                MonthlyPriceCents = 1999,
                TrialDays = 7,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            });
    }

    public override int SaveChanges()
    {
        SetAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditFields()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;

                if (entry.Entity.Id == Guid.Empty)
                    entry.Entity.Id = Guid.NewGuid();
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
