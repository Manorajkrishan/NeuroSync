using Microsoft.EntityFrameworkCore;
using NeuroSync.Core.Models;

namespace NeuroSync.Api.Data;

public class NeuroSyncDbContext : DbContext
{
    public NeuroSyncDbContext(DbContextOptions<NeuroSyncDbContext> options)
        : base(options)
    {
    }

    // Human OS v2.0 Tables
    public DbSet<DailyEmotionalSummary> DailyEmotionalSummaries { get; set; }
    public DbSet<LifeDomain> LifeDomains { get; set; }
    public DbSet<Decision> Decisions { get; set; }
    public DbSet<DecisionOption> DecisionOptions { get; set; }
    public DbSet<CollapseRiskAssessment> CollapseRiskAssessments { get; set; }
    public DbSet<IdentityProfile> IdentityProfiles { get; set; }
    public DbSet<LifeEvent> LifeEvents { get; set; }
    public DbSet<EmotionalGrowthMetrics> EmotionalGrowthMetrics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // DailyEmotionalSummary configuration
        modelBuilder.Entity<DailyEmotionalSummary>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique();
            entity.Property(e => e.CurrentEmotion).HasMaxLength(50);
            entity.Property(e => e.EmotionalTrend).HasMaxLength(20);
            entity.Property(e => e.BurnoutRiskLevel).HasMaxLength(20);
        });

        // LifeDomain configuration
        modelBuilder.Entity<LifeDomain>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Domain }).IsUnique();
            entity.Property(e => e.RiskLevel).HasMaxLength(20);
        });

        // Decision configuration
        modelBuilder.Entity<Decision>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.DecisionText).HasMaxLength(1000);
            entity.HasMany(e => e.Options)
                .WithOne(o => o.Decision)
                .HasForeignKey(o => o.DecisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DecisionOption configuration
        modelBuilder.Entity<DecisionOption>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OptionText).HasMaxLength(500);
            entity.Property(e => e.RiskLevel).HasMaxLength(20);
        });

        // CollapseRiskAssessment configuration
        modelBuilder.Entity<CollapseRiskAssessment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.DepressionSeverity).HasMaxLength(20);
            entity.Property(e => e.AnxietyImpact).HasMaxLength(20);
            entity.Property(e => e.InterventionUrgency).HasMaxLength(20);
        });

        // IdentityProfile configuration
        modelBuilder.Entity<IdentityProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.LifePurpose).HasMaxLength(1000);
        });

        // LifeEvent configuration
        modelBuilder.Entity<LifeEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.Description).HasMaxLength(2000);
        });

        // EmotionalGrowthMetrics configuration
        modelBuilder.Entity<EmotionalGrowthMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
        });
    }
}
