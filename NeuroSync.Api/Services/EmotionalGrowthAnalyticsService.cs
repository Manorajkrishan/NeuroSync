using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroSync.Api.Data;
using NeuroSync.Core.Models;
using System.Text.Json;

namespace NeuroSync.Api.Services;

public class EmotionalGrowthAnalyticsService
{
    private readonly NeuroSyncDbContext _context;
    private readonly ILogger<EmotionalGrowthAnalyticsService> _logger;

    public EmotionalGrowthAnalyticsService(
        NeuroSyncDbContext context,
        ILogger<EmotionalGrowthAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EmotionalGrowthMetrics> CalculateMaturityScoreAsync(string userId)
    {
        var metrics = await _context.EmotionalGrowthMetrics
            .FirstOrDefaultAsync(m => m.UserId == userId);

        if (metrics == null)
        {
            metrics = new EmotionalGrowthMetrics { UserId = userId };
            _context.EmotionalGrowthMetrics.Add(metrics);
        }

        // Calculate emotional intelligence components
        var summaries = await _context.DailyEmotionalSummaries
            .Where(s => s.UserId == userId && s.Date >= DateTime.UtcNow.AddMonths(-3))
            .ToListAsync();

        // Self-awareness: Consistency in emotion recognition
        var emotionConsistency = CalculateEmotionConsistency(summaries);
        metrics.SelfAwareness = emotionConsistency;

        // Regulation ability: Stress management
        var avgStress = summaries.Any() ? summaries.Average(s => s.StressLevel) : 50;
        metrics.RegulationAbility = Math.Max(0, 100 - avgStress);

        // Empathy score: Would integrate with relationship data
        metrics.EmpathyScore = 60; // Placeholder

        // Social skills: Would integrate with social interaction data
        metrics.SocialSkills = 60; // Placeholder

        // Overall emotional intelligence
        metrics.EmotionalIntelligence = (
            metrics.SelfAwareness + 
            metrics.RegulationAbility + 
            metrics.EmpathyScore + 
            metrics.SocialSkills
        ) / 4;

        // Overall maturity score
        metrics.MaturityScore = metrics.EmotionalIntelligence;

        metrics.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return metrics;
    }

    public async Task<ResilienceMetrics> CalculateResilienceScoreAsync(string userId)
    {
        var metrics = await _context.EmotionalGrowthMetrics
            .FirstOrDefaultAsync(m => m.UserId == userId);

        if (metrics == null)
        {
            metrics = await CalculateMaturityScoreAsync(userId);
        }

        // Analyze recovery patterns from life events
        var recentEvents = await _context.LifeEvents
            .Where(e => e.UserId == userId && e.Timestamp >= DateTime.UtcNow.AddMonths(-6))
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        // Recovery speed: Time between negative and positive events
        var recoverySpeed = CalculateRecoverySpeed(recentEvents);
        metrics.RecoverySpeed = recoverySpeed;

        // Bounce-back ability: Ratio of growth events after trauma
        var bounceBack = CalculateBounceBackAbility(recentEvents);
        metrics.BounceBackAbility = bounceBack;

        // Stress tolerance: Ability to handle high stress
        var summaries = await _context.DailyEmotionalSummaries
            .Where(s => s.UserId == userId && s.Date >= DateTime.UtcNow.AddMonths(-3))
            .ToListAsync();
        
        var stressTolerance = CalculateStressTolerance(summaries);
        metrics.StressTolerance = stressTolerance;

        // Adaptation capacity: Handling change
        var adaptationCapacity = CalculateAdaptationCapacity(recentEvents);
        metrics.AdaptationCapacity = adaptationCapacity;

        // Support utilization: Would integrate with support network data
        metrics.SupportUtilization = 60; // Placeholder

        // Overall resilience score
        metrics.ResilienceScore = (
            metrics.RecoverySpeed +
            metrics.BounceBackAbility +
            metrics.StressTolerance +
            metrics.AdaptationCapacity +
            metrics.SupportUtilization
        ) / 5;

        metrics.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new ResilienceMetrics
        {
            OverallScore = metrics.ResilienceScore,
            RecoverySpeed = metrics.RecoverySpeed,
            BounceBackAbility = metrics.BounceBackAbility,
            StressTolerance = metrics.StressTolerance,
            AdaptationCapacity = metrics.AdaptationCapacity,
            SupportUtilization = metrics.SupportUtilization
        };
    }

    public async Task<GrowthReport> GenerateGrowthReportAsync(string userId, int months = 6)
    {
        var metrics = await _context.EmotionalGrowthMetrics
            .FirstOrDefaultAsync(m => m.UserId == userId);

        if (metrics == null)
        {
            metrics = await CalculateMaturityScoreAsync(userId);
            await CalculateResilienceScoreAsync(userId);
        }

        // Get historical metrics for comparison
        var cutoffDate = DateTime.UtcNow.AddMonths(-months);
        
        var summaries = await _context.DailyEmotionalSummaries
            .Where(s => s.UserId == userId && s.Date >= cutoffDate)
            .OrderBy(s => s.Date)
            .ToListAsync();

        var events = await _context.LifeEvents
            .Where(e => e.UserId == userId && e.Timestamp >= cutoffDate)
            .ToListAsync();

        // Calculate growth trends
        var maturityTrend = CalculateTrend(summaries, s => s.EmotionalGrowthScore);
        var resilienceTrend = "Improving"; // Would calculate from events

        // Identify strengths developed
        var strengths = IdentifyStrengths(metrics, events);

        // Areas improving
        var improving = IdentifyImprovingAreas(metrics, summaries);

        // Recommendations
        var recommendations = GenerateGrowthRecommendations(metrics, summaries, events);

        return new GrowthReport
        {
            UserId = userId,
            Period = $"{months} months",
            MaturityScore = metrics.MaturityScore,
            ResilienceScore = metrics.ResilienceScore,
            MaturityTrend = maturityTrend,
            ResilienceTrend = resilienceTrend,
            StrengthsDeveloped = strengths,
            AreasImproving = improving,
            Recommendations = recommendations
        };
    }

    // Private helper methods

    private double CalculateEmotionConsistency(List<DailyEmotionalSummary> summaries)
    {
        if (!summaries.Any()) return 50;

        // Simple consistency: How stable emotions are
        var emotions = summaries.Select(s => s.CurrentEmotion).ToList();
        var uniqueEmotions = emotions.Distinct().Count();
        var totalEmotions = emotions.Count;

        // More consistency = fewer unique emotions relative to total
        var consistency = 100 - ((double)uniqueEmotions / totalEmotions * 50);
        return Math.Max(0, Math.Min(100, consistency));
    }

    private double CalculateRecoverySpeed(List<LifeEvent> events)
    {
        if (!events.Any()) return 50;

        var traumaEvents = events.Where(e => e.EventType == LifeEventType.Trauma || 
                                            e.EventType == LifeEventType.Crisis).ToList();
        
        if (!traumaEvents.Any()) return 70; // No trauma = high resilience

        var recoveryTimes = new List<double>();

        foreach (var trauma in traumaEvents)
        {
            var growthAfter = events
                .Where(e => e.Timestamp > trauma.Timestamp && 
                           e.EventType == LifeEventType.Growth &&
                           e.Timestamp <= trauma.Timestamp.AddDays(90))
                .OrderBy(e => e.Timestamp)
                .FirstOrDefault();

            if (growthAfter != null)
            {
                var days = (growthAfter.Timestamp - trauma.Timestamp).TotalDays;
                var speed = Math.Max(0, 100 - (days / 90 * 100)); // Faster = better
                recoveryTimes.Add(speed);
            }
        }

        return recoveryTimes.Any() ? recoveryTimes.Average() : 50;
    }

    private double CalculateBounceBackAbility(List<LifeEvent> events)
    {
        var traumaEvents = events.Where(e => 
            e.EventType == LifeEventType.Trauma || 
            e.EventType == LifeEventType.Crisis).ToList();

        if (!traumaEvents.Any()) return 80;

        var bounceBackCount = 0;
        foreach (var trauma in traumaEvents)
        {
            var hasGrowthAfter = events.Any(e => 
                e.Timestamp > trauma.Timestamp && 
                e.EventType == LifeEventType.Growth);
            
            if (hasGrowthAfter) bounceBackCount++;
        }

        return (bounceBackCount / (double)traumaEvents.Count) * 100;
    }

    private double CalculateStressTolerance(List<DailyEmotionalSummary> summaries)
    {
        if (!summaries.Any()) return 50;

        var highStressDays = summaries.Count(s => s.StressLevel > 70);
        var totalDays = summaries.Count;

        // High tolerance = can handle high stress without breaking
        // Simple calculation: more high stress days = higher tolerance (up to a point)
        var tolerance = Math.Min(100, (highStressDays / (double)totalDays) * 100 + 40);
        return tolerance;
    }

    private double CalculateAdaptationCapacity(List<LifeEvent> events)
    {
        var changeEvents = events.Where(e => 
            e.LifeImpact == LifeImpactLevel.High || 
            e.LifeImpact == LifeImpactLevel.Transformative).ToList();

        if (!changeEvents.Any()) return 60;

        var growthAfterChange = changeEvents.Count(e =>
            events.Any(g => 
                g.Timestamp > e.Timestamp && 
                g.EventType == LifeEventType.Growth &&
                g.Timestamp <= e.Timestamp.AddDays(30)));

        return (growthAfterChange / (double)changeEvents.Count) * 100;
    }

    private string CalculateTrend<T>(List<T> items, Func<T, double> valueSelector) where T : class
    {
        if (items.Count < 3) return "Stable";

        var recent = items.TakeLast(3).Average(valueSelector);
        var earlier = items.Take(3).Average(valueSelector);

        if (recent > earlier * 1.1) return "Improving";
        if (recent < earlier * 0.9) return "Declining";
        return "Stable";
    }

    private List<string> IdentifyStrengths(EmotionalGrowthMetrics metrics, List<LifeEvent> events)
    {
        var strengths = new List<string>();

        if (metrics.MaturityScore > 70)
            strengths.Add("High emotional maturity");
        if (metrics.ResilienceScore > 70)
            strengths.Add("Strong resilience");
        if (metrics.SelfAwareness > 70)
            strengths.Add("High self-awareness");
        if (metrics.RegulationAbility > 70)
            strengths.Add("Good emotional regulation");

        var growthEvents = events.Count(e => e.EventType == LifeEventType.Growth);
        if (growthEvents > 0)
            strengths.Add("Consistent personal growth");

        return strengths;
    }

    private List<string> IdentifyImprovingAreas(EmotionalGrowthMetrics metrics, List<DailyEmotionalSummary> summaries)
    {
        var improving = new List<string>();

        // Compare recent vs earlier
        if (summaries.Count >= 6)
        {
            var recent = summaries.TakeLast(3);
            var earlier = summaries.Take(3);

            var recentMaturity = recent.Average(s => s.EmotionalGrowthScore);
            var earlierMaturity = earlier.Average(s => s.EmotionalGrowthScore);

            if (recentMaturity > earlierMaturity * 1.1)
                improving.Add("Emotional maturity");
        }

        if (metrics.ResilienceScore > 60 && metrics.ResilienceScore < 80)
            improving.Add("Resilience");

        return improving;
    }

    private List<string> GenerateGrowthRecommendations(
        EmotionalGrowthMetrics metrics,
        List<DailyEmotionalSummary> summaries,
        List<LifeEvent> events)
    {
        var recommendations = new List<string>();

        if (metrics.SelfAwareness < 60)
            recommendations.Add("🧠 Focus on developing self-awareness through reflection");
        
        if (metrics.RegulationAbility < 60)
            recommendations.Add("🧘 Practice emotional regulation techniques (meditation, breathing)");
        
        if (metrics.ResilienceScore < 60)
            recommendations.Add("💪 Build resilience through challenging experiences and support");
        
        if (metrics.EmpathyScore < 60)
            recommendations.Add("💚 Develop empathy through connection and understanding others");

        return recommendations;
    }
}

// DTOs
public class ResilienceMetrics
{
    public double OverallScore { get; set; } // 0-100
    public double RecoverySpeed { get; set; } // 0-100
    public double BounceBackAbility { get; set; } // 0-100
    public double StressTolerance { get; set; } // 0-100
    public double AdaptationCapacity { get; set; } // 0-100
    public double SupportUtilization { get; set; } // 0-100
}

public class GrowthReport
{
    public string UserId { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public double MaturityScore { get; set; } // 0-100
    public double ResilienceScore { get; set; } // 0-100
    public string MaturityTrend { get; set; } = "Stable"; // Improving/Declining/Stable
    public string ResilienceTrend { get; set; } = "Stable";
    public List<string> StrengthsDeveloped { get; set; } = new();
    public List<string> AreasImproving { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}
