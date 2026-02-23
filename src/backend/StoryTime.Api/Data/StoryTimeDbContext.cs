using Microsoft.EntityFrameworkCore;
using StoryTime.Api.Data.Models;

namespace StoryTime.Api.Data;

/// <summary>
/// Database context for the StoryTime application.
/// </summary>
public class StoryTimeDbContext : DbContext
{
    public StoryTimeDbContext(DbContextOptions<StoryTimeDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Tier> Tiers => Set<Tier>();
    public DbSet<Capability> Capabilities => Set<Capability>();
    public DbSet<Variable> Variables => Set<Variable>();
    public DbSet<TierCapability> TierCapabilities => Set<TierCapability>();
    public DbSet<TierVariable> TierVariables => Set<TierVariable>();
    public DbSet<AppDefault> AppDefaults => Set<AppDefault>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<ApiLog> ApiLogs => Set<ApiLog>();
    public DbSet<AiLog> AiLogs => Set<AiLog>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        foreach (var entry in ChangeTracker.Entries())
        {
            // Handle BaseEntity timestamp updates
            if (entry.Entity is BaseEntity baseEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        baseEntity.CreatedAt = now;
                        baseEntity.UpdatedAt = now;
                        if (baseEntity.Id == Guid.Empty)
                        {
                            baseEntity.Id = Guid.NewGuid();
                        }
                        break;
                    case EntityState.Modified:
                        baseEntity.UpdatedAt = now;
                        break;
                }
            }
            // Handle PushSubscription (has CreatedAt but not UpdatedAt)
            else if (entry.Entity is PushSubscription pushSub)
            {
                if (entry.State == EntityState.Added)
                {
                    pushSub.CreatedAt = now;
                    if (pushSub.Id == Guid.Empty)
                    {
                        pushSub.Id = Guid.NewGuid();
                    }
                }
            }
            // Handle ApiLog (append-only)
            else if (entry.Entity is ApiLog apiLog)
            {
                if (entry.State == EntityState.Added)
                {
                    apiLog.CreatedAt = now;
                    if (apiLog.Id == Guid.Empty)
                    {
                        apiLog.Id = Guid.NewGuid();
                    }
                }
            }
            // Handle AiLog (append-only)
            else if (entry.Entity is AiLog aiLog)
            {
                if (entry.State == EntityState.Added)
                {
                    aiLog.CreatedAt = now;
                    if (aiLog.Id == Guid.Empty)
                    {
                        aiLog.Id = Guid.NewGuid();
                    }
                }
            }
        }
        
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table names with PascalCase
        modelBuilder.Entity<Tier>().ToTable("Tiers");
        modelBuilder.Entity<Capability>().ToTable("Capabilities");
        modelBuilder.Entity<Variable>().ToTable("Variables");
        modelBuilder.Entity<TierCapability>().ToTable("TierCapabilities");
        modelBuilder.Entity<TierVariable>().ToTable("TierVariables");
        modelBuilder.Entity<AppDefault>().ToTable("AppDefaults");
        modelBuilder.Entity<UserSubscription>().ToTable("UserSubscriptions");
        modelBuilder.Entity<PushSubscription>().ToTable("PushSubscriptions");
        modelBuilder.Entity<ApiLog>().ToTable("ApiLogs");
        modelBuilder.Entity<AiLog>().ToTable("AiLogs");

        // Apply global query filter for soft-delete
        modelBuilder.Entity<Tier>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Capability>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Variable>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TierCapability>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TierVariable>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AppDefault>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<UserSubscription>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PushSubscription>().HasQueryFilter(e => !e.IsDeleted);

        // Configure Tier entity
        modelBuilder.Entity<Tier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.BillingPeriod).IsRequired().HasMaxLength(50);
        });

        // Configure Capability entity
        modelBuilder.Entity<Capability>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Label).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired();
        });

        // Configure Variable entity
        modelBuilder.Entity<Variable>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Label).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.DefaultValue).IsRequired();
        });

        // Configure TierCapability entity
        modelBuilder.Entity<TierCapability>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Tier)
                .WithMany(t => t.TierCapabilities)
                .HasForeignKey(e => e.TierId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Capability)
                .WithMany(c => c.TierCapabilities)
                .HasForeignKey(e => e.CapabilityId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.TierId, e.CapabilityId }).IsUnique();
        });

        // Configure TierVariable entity
        modelBuilder.Entity<TierVariable>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Tier)
                .WithMany(t => t.TierVariables)
                .HasForeignKey(e => e.TierId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Variable)
                .WithMany(v => v.TierVariables)
                .HasForeignKey(e => e.VariableId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.TierId, e.VariableId }).IsUnique();
            entity.Property(e => e.Value).IsRequired();
        });

        // Configure AppDefault entity
        modelBuilder.Entity<AppDefault>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired();
        });

        // Configure UserSubscription entity
        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Tier)
                .WithMany(t => t.UserSubscriptions)
                .HasForeignKey(e => e.TierId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => e.SoftUserId);
            entity.HasIndex(e => e.ExternalSubId);
            entity.Property(e => e.SoftUserId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ExternalSubId).HasMaxLength(255);
        });

        // Configure PushSubscription entity
        modelBuilder.Entity<PushSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SoftUserId);
            entity.HasIndex(e => e.Endpoint).IsUnique();
            entity.Property(e => e.SoftUserId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Endpoint).IsRequired();
            entity.Property(e => e.PublicKey).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AuthSecret).IsRequired().HasMaxLength(500);
        });

        // Configure ApiLog entity
        modelBuilder.Entity<ApiLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.SoftUserId);
            entity.Property(e => e.Method).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(500);
            entity.Property(e => e.SoftUserId).HasMaxLength(255);
        });

        // Configure AiLog entity
        modelBuilder.Entity<AiLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.SoftUserId);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SoftUserId).HasMaxLength(255);
        });
    }
}
