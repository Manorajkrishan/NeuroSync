using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroSync.Api.Data;
using NeuroSync.Core;
using NeuroSync.Core.Models;
using System.Text.Json;

namespace NeuroSync.Api.Services;

public class EmotionalOSDashboardService
{
    private readonly NeuroSyncDbContext _context;
    private readonly ILogger<EmotionalOSDashboardService> _logger;
    private readonly EmotionDetectionService _emotionDetection;
    private readonly ICollapseRiskPredictor? _collapsePredictor;
    private readonly ConversationMemory? _conversationMemory;

    public EmotionalOSDashboardService(
        NeuroSyncDbContext context,
        ILogger<EmotionalOSDashboardService> logger,
        EmotionDetectionService emotionDetection,
        ICollapseRiskPredictor? collapsePredictor = null,
        ConversationMemory? conversationMemory = null)
    {
        _context = context;
        _logger = logger;
        _emotionDetection = emotionDetection;
        _collapsePredictor = collapsePredictor;
        _conversationMemory = conversationMemory;
    }

    public async Task<DailyEmotionalSummary> GetDailyEmotionalSummaryAsync(string userId, DateTime? date = null)
    {
        var targetDate = (date ?? DateTime.UtcNow).Date;

        // Get or create today's summary
        var summary = await _context.DailyEmotionalSummaries
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Date == targetDate);

        if (summary == null)
        {
            summary = new DailyEmotionalSummary
            {
                UserId = userId,
                Date = targetDate
            };
            _context.DailyEmotionalSummaries.Add(summary);
        }

        // Calculate current emotional state from recent emotions
        var currentEmotion = await CalculateCurrentEmotionAsync(userId);
        summary.CurrentEmotion = currentEmotion.Emotion;
        summary.CurrentEmotionConfidence = currentEmotion.Confidence;

        // Calculate 7-day trend
        var trend = await CalculateEmotionalTrendAsync(userId, 7);
        summary.EmotionalTrend = trend.Trend;
        summary.AverageEmotionScore = trend.AverageScore;

        // Calculate stress and mental load
        var stressAnalysis = await CalculateStressAndMentalLoadAsync(userId);
        summary.StressLevel = stressAnalysis.StressLevel;
        summary.MentalLoad = stressAnalysis.MentalLoad;
        summary.EnergyLevel = stressAnalysis.EnergyLevel;

        // Get burnout risk
        if (_collapsePredictor != null)
        {
            var burnoutRisk = await _collapsePredictor.CalculateBurnoutRiskAsync(userId);
            summary.BurnoutRisk = burnoutRisk.Score;
            summary.BurnoutRiskLevel = burnoutRisk.Level.ToString();
        }
        else
        {
            // Simple calculation based on stress
            summary.BurnoutRisk = Math.Min(100, stressAnalysis.StressLevel * 1.2);
            summary.BurnoutRiskLevel = summary.BurnoutRisk switch
            {
                < 30 => "Low",
                < 60 => "Medium",
                < 80 => "High",
                _ => "Critical"
            };
        }

        // Calculate emotional growth score
        var growthMetrics = await _context.EmotionalGrowthMetrics
            .FirstOrDefaultAsync(m => m.UserId == userId);
        summary.EmotionalGrowthScore = growthMetrics?.MaturityScore ?? 50;

        // Get domain states
        var domains = await _context.LifeDomains
            .Where(d => d.UserId == userId)
            .ToListAsync();
        var domainStates = domains.ToDictionary(d => d.Domain.ToString(), d => d.EmotionalScore);
        summary.DomainStates = JsonSerializer.Serialize(domainStates);

        // Generate key insights (includes ConversationMemory: concerning patterns, most common emotion)
        var insights = GenerateKeyInsights(userId, summary, domains, trend);
        summary.KeyInsights = JsonSerializer.Serialize(insights);

        summary.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return summary;
    }

    public async Task<BurnoutRiskAnalysis> GetBurnoutRiskScoreAsync(string userId)
    {
        if (_collapsePredictor != null)
        {
            return await _collapsePredictor.CalculateBurnoutRiskAsync(userId);
        }

        // Fallback calculation
        var stress = await CalculateStressAndMentalLoadAsync(userId);
        var riskScore = Math.Min(100, stress.StressLevel * 1.2);

        return new BurnoutRiskAnalysis
        {
            Score = riskScore,
            Level = riskScore switch
            {
                < 30 => RiskLevel.Low,
                < 60 => RiskLevel.Moderate,
                < 80 => RiskLevel.High,
                _ => RiskLevel.Critical
            },
            ContributingFactors = new List<string> { "High stress level", "Mental load" },
            WarningSigns = new List<string>(),
            InterventionActions = new List<string>()
        };
    }

    public async Task<EmotionalGrowthMetrics> GetEmotionalGrowthScoreAsync(string userId)
    {
        var metrics = await _context.EmotionalGrowthMetrics
            .FirstOrDefaultAsync(m => m.UserId == userId);

        if (metrics == null)
        {
            // Initialize default metrics
            metrics = new EmotionalGrowthMetrics
            {
                UserId = userId,
                MaturityScore = 50,
                EmotionalIntelligence = 50,
                SelfAwareness = 50,
                RegulationAbility = 50,
                EmpathyScore = 50,
                SocialSkills = 50,
                ResilienceScore = 50,
                RecoverySpeed = 50,
                BounceBackAbility = 50,
                StressTolerance = 50,
                AdaptationCapacity = 50,
                SupportUtilization = 50
            };
            _context.EmotionalGrowthMetrics.Add(metrics);
            await _context.SaveChangesAsync();
        }

        return metrics;
    }

    public async Task<MentalLoadAnalysis> GetMentalLoadAnalysisAsync(string userId)
    {
        var stress = await CalculateStressAndMentalLoadAsync(userId);
        
        // Get domain loads
        var domains = await _context.LifeDomains
            .Where(d => d.UserId == userId)
            .ToListAsync();

        var domainLoads = domains.ToDictionary(
            d => d.Domain.ToString(),
            d => d.StressLevel
        );

        var totalLoad = stress.MentalLoad;
        var overloadThreshold = 70;

        return new MentalLoadAnalysis
        {
            TotalMentalLoad = totalLoad,
            LoadByDomain = domainLoads,
            OverloadIndicators = totalLoad > overloadThreshold
                ? new List<string> { $"Mental load exceeds safe threshold ({overloadThreshold})" }
                : new List<string>(),
            ReliefSuggestions = GenerateReliefSuggestions(totalLoad, domains)
        };
    }

    // Private helper methods

    private async Task<(string Emotion, double Confidence)> CalculateCurrentEmotionAsync(string userId)
    {
        if (_conversationMemory != null)
        {
            var ctx = _conversationMemory.GetOrCreateContext(userId);
            var last = ctx.History.LastOrDefault();
            if (last?.DetectedEmotion != null)
            {
                var em = last.DetectedEmotion.Emotion.ToString();
                var conf = (double)last.DetectedEmotion.Confidence;
                return (em, conf);
            }
            if (ctx.LastEmotion.HasValue)
                return (ctx.LastEmotion.Value.ToString(), 0.7);
        }
        await Task.CompletedTask;
        return ("Neutral", 0.5);
    }

    private async Task<(string Trend, double AverageScore)> CalculateEmotionalTrendAsync(string userId, int days)
    {
        var summaries = await _context.DailyEmotionalSummaries
            .Where(s => s.UserId == userId && s.Date >= DateTime.UtcNow.Date.AddDays(-days))
            .OrderBy(s => s.Date)
            .ToListAsync();

        if (summaries.Count < 2)
        {
            return ("Stable", 50);
        }

        var averageScore = summaries.Average(s => s.AverageEmotionScore);
        var recentAverage = summaries.TakeLast(3).Average(s => s.AverageEmotionScore);
        var earlierAverage = summaries.Take(3).Average(s => s.AverageEmotionScore);

        var trend = recentAverage > earlierAverage * 1.1 ? "Improving"
                   : recentAverage < earlierAverage * 0.9 ? "Declining"
                   : "Stable";

        return (trend, averageScore);
    }

    private async Task<(double StressLevel, double MentalLoad, double EnergyLevel)> CalculateStressAndMentalLoadAsync(string userId)
    {
        var domains = await _context.LifeDomains
            .Where(d => d.UserId == userId)
            .ToListAsync();

        var avgDomainStress = domains.Any() ? domains.Average(d => d.StressLevel) : 50;
        var concerningPattern = _conversationMemory?.HasConcerningPattern(userId) == true;
        if (concerningPattern)
        {
            avgDomainStress = Math.Min(100, avgDomainStress + 5);
        }
        var mentalLoad = Math.Min(100, avgDomainStress * 1.5 + (concerningPattern ? 5 : 0));
        var energyLevel = Math.Max(0, 100 - mentalLoad);

        return (avgDomainStress, mentalLoad, energyLevel);
    }

    private List<string> GenerateKeyInsights(
        string userId,
        DailyEmotionalSummary summary,
        List<LifeDomain> domains,
        (string Trend, double AverageScore) trend)
    {
        var insights = new List<string>();

        if (_conversationMemory != null)
        {
            if (_conversationMemory.HasConcerningPattern(userId))
                insights.Add("💜 Recurring difficult emotions noticed. It's okay to seek support.");
            var mostCommon = _conversationMemory.GetMostCommonEmotion(userId);
            if (mostCommon != null && mostCommon.Frequency >= 3)
                insights.Add($"📊 You often feel {mostCommon.Emotion.ToString().ToLower()}. We can explore what helps.");
        }

        if (summary.BurnoutRisk > 60)
        {
            insights.Add($"⚠️ Burnout risk is elevated ({summary.BurnoutRiskLevel}). Consider rest and support.");
        }

        if (trend.Trend == "Declining")
        {
            insights.Add("📉 Emotional trend is declining. Let's explore what might be contributing.");
        }
        else if (trend.Trend == "Improving")
        {
            insights.Add("📈 Your emotional state is improving! Keep up the great work.");
        }

        var unhealthyDomains = domains.Where(d => d.RiskLevel == "Unhealthy" || d.RiskLevel == "Crisis").ToList();
        if (unhealthyDomains.Any())
        {
            insights.Add($"🎯 {unhealthyDomains.Count} life domain(s) need attention: {string.Join(", ", unhealthyDomains.Select(d => d.Domain.ToString()))}");
        }

        if (summary.EnergyLevel < 30)
        {
            insights.Add("🔋 Energy levels are low. Consider rest, nutrition, and self-care.");
        }

        return insights;
    }

    private List<string> GenerateReliefSuggestions(double totalLoad, List<LifeDomain> domains)
    {
        var suggestions = new List<string>();

        if (totalLoad > 80)
        {
            suggestions.Add("🛑 High priority: Immediate rest and stress reduction needed");
            suggestions.Add("💚 Consider reaching out to support resources or professionals");
        }
        else if (totalLoad > 60)
        {
            suggestions.Add("⏸️ Take breaks throughout the day");
            suggestions.Add("🧘 Practice stress-reduction techniques");
        }

        var highStressDomains = domains.Where(d => d.StressLevel > 70).ToList();
        foreach (var domain in highStressDomains)
        {
            suggestions.Add($"🎯 Focus on reducing stress in {domain.Domain.ToString()} domain");
        }

        return suggestions;
    }
}

// DTOs
public class BurnoutRiskAnalysis
{
    public double Score { get; set; }
    public RiskLevel Level { get; set; }
    public List<string> ContributingFactors { get; set; } = new();
    public List<string> WarningSigns { get; set; } = new();
    public List<string> InterventionActions { get; set; } = new();
}

public class MentalLoadAnalysis
{
    public double TotalMentalLoad { get; set; }
    public Dictionary<string, double> LoadByDomain { get; set; } = new();
    public List<string> OverloadIndicators { get; set; } = new();
    public List<string> ReliefSuggestions { get; set; } = new();
}
