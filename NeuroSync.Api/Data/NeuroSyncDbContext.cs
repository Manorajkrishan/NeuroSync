using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NeuroSync.Api.Auth;
using NeuroSync.Core.Models;

namespace NeuroSync.Api.Data;

public class NeuroSyncDbContext : IdentityDbContext<ApplicationUser>
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

    // Conversation memory persistence
    public DbSet<ConversationSessionEntity> ConversationSessions { get; set; }
    public DbSet<ConversationEntryEntity> ConversationEntries { get; set; }
    public DbSet<EmotionPatternEntity> EmotionPatterns { get; set; }

    public DbSet<UserPrivacyProfile> UserPrivacyProfiles { get; set; }
    public DbSet<UserPreferenceRecord> UserPreferences { get; set; }
    public DbSet<PersistentMemoryItem> PersistentMemories { get; set; }
    public DbSet<ConversationHistoryEntry> ConversationHistories { get; set; }
    public DbSet<UserBaselineRecord> UserBaselines { get; set; }
    public DbSet<CompanionFeedbackRecord> CompanionFeedback { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AuditEvent> AuditEvents { get; set; }

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

        // Conversation memory entities
        modelBuilder.Entity<ConversationSessionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.UserId).HasMaxLength(128);
        });
        modelBuilder.Entity<ConversationEntryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.UserId).HasMaxLength(128);
            entity.Property(e => e.UserMessage).HasMaxLength(2000);
            entity.Property(e => e.ResponseMessage).HasMaxLength(2000);
        });
        modelBuilder.Entity<EmotionPatternEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Emotion }).IsUnique();
            entity.Property(e => e.UserId).HasMaxLength(128);
            entity.Property(e => e.CommonTriggersJson).HasMaxLength(2000);
        });

        modelBuilder.Entity<UserPrivacyProfile>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.HasOne<ApplicationUser>().WithOne().HasForeignKey<UserPrivacyProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<UserPreferenceRecord>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.DisplayName).HasMaxLength(40);
            entity.Property(e => e.Locale).HasMaxLength(16);
            entity.HasOne<ApplicationUser>().WithOne().HasForeignKey<UserPreferenceRecord>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PersistentMemoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.Content).HasMaxLength(500);
            entity.HasIndex(e => e.UserId);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ConversationHistoryEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.UserMessage).HasMaxLength(2000);
            entity.Property(e => e.AssistantMessage).HasMaxLength(4000);
            entity.HasIndex(e => e.UserId);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<UserBaselineRecord>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.HasOne<ApplicationUser>().WithOne().HasForeignKey<UserBaselineRecord>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CompanionFeedbackRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.ResponseId).HasMaxLength(64);
            entity.Property(e => e.ReasonCode).HasMaxLength(32);
            entity.Property(e => e.ProviderId).HasMaxLength(32);
            entity.Property(e => e.Intent).HasMaxLength(32);
            entity.Property(e => e.Mode).HasMaxLength(32);
            entity.Property(e => e.Locale).HasMaxLength(16);
            entity.HasIndex(e => new { e.UserId, e.ResponseId }).IsUnique();
            entity.HasIndex(e => e.CreatedAtUtc);
            entity.HasIndex(e => e.ShareForLearning);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.TokenHash).HasMaxLength(128);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(64);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.IpHash).HasMaxLength(128);
            entity.HasIndex(e => e.OccurredAtUtc);
        });
    }
}
