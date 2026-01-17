using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroSync.Api.Data;
using NeuroSync.Core.Models;
using System.Text.Json;

namespace NeuroSync.Api.Services;

public class CollapseRiskPredictorService : ICollapseRiskPredictor
{
    private readonly NeuroSyncDbContext _context;
    private readonly ILogger<CollapseRiskPredictorService> _logger;
    private readonly EmotionalOSDashboardService? _dashboardService;

    public CollapseRiskPredictorService(
        NeuroSyncDbContext context,
        ILogger<CollapseRiskPredictorService> logger,
        EmotionalOSDashboardService? dashboardService = null)
    {
        _context = context;
        _logger = logger;
        _dashboardService = dashboardService;
    }

    public async Task<BurnoutRiskAnalysis> CalculateBurnoutRiskAsync(string userId)
    {
        // Get recent stress levels
        var recentSummaries = await _context.DailyEmotionalSummaries
            .Where(s => s.UserId == userId && s.Date >= DateTime.UtcNow.AddDays(-30))
            .OrderByDescending(s => s.Date)
            .Take(30)
            .ToListAsync();

        var avgStress = recentSummaries.Any() ? recentSummaries.Average(s => s.StressLevel) : 50;
        var avgMentalLoad = recentSummaries.Any() ? recentSummaries.Average(s => s.MentalLoad) : 50;
        var avgEnergy = recentSummaries.Any() ? recentSummaries.Average(s => s.EnergyLevel) : 50;

        // Calculate burnout risk
        var burnoutScore = CalculateBurnoutScore(avgStress, avgMentalLoad, avgEnergy);

        var contributingFactors = new List<string>();
        if (avgStress > 70) contributingFactors.Add("Consistently high stress levels");
        if (avgMentalLoad > 75) contributingFactors.Add("High mental load");
        if (avgEnergy < 30) contributingFactors.Add("Persistently low energy");
        if (recentSummaries.Count(s => s.BurnoutRisk > 60) > 15)
            contributingFactors.Add("Sustained high burnout risk over 2+ weeks");

        var warningSigns = DetectWarningSigns(recentSummaries);

        var interventionActions = GenerateInterventionActions(burnoutScore, contributingFactors);

        return new BurnoutRiskAnalysis
        {
            Score = burnoutScore,
            Level = burnoutScore switch
            {
                < 30 => RiskLevel.Low,
                < 60 => RiskLevel.Moderate,
                < 80 => RiskLevel.High,
                _ => RiskLevel.Critical
            },
            ContributingFactors = contributingFactors,
            WarningSigns = warningSigns,
            InterventionActions = interventionActions
        };
    }

    public async Task<CollapseRiskAssessment> CalculateCollapseRiskAsync(string userId)
    {
        var burnoutRisk = await CalculateBurnoutRiskAsync(userId);
        var depressionRisk = await CalculateDepressionRiskAsync(userId);
        var anxietyRisk = await CalculateAnxietyRiskAsync(userId);

        var overallScore = Math.Max(burnoutRisk.Score, Math.Max(depressionRisk.Score, anxietyRisk.Score));
        var riskLevel = overallScore switch
        {
            < 30 => RiskLevel.Low,
            < 60 => RiskLevel.Moderate,
            < 80 => RiskLevel.High,
            _ => RiskLevel.Critical
        };

        var assessment = new CollapseRiskAssessment
        {
            UserId = userId,
            OverallRiskScore = overallScore,
            RiskLevel = riskLevel,
            BurnoutRiskScore = burnoutRisk.Score,
            BurnoutContributingFactors = JsonSerializer.Serialize(burnoutRisk.ContributingFactors),
            BurnoutWarningSigns = JsonSerializer.Serialize(burnoutRisk.WarningSigns),
            DepressionRiskScore = depressionRisk.Score,
            DepressionSymptoms = JsonSerializer.Serialize(depressionRisk.Symptoms),
            DepressionTriggers = JsonSerializer.Serialize(depressionRisk.Triggers),
            DepressionSeverity = depressionRisk.Severity,
            AnxietyRiskScore = anxietyRisk.Score,
            AnxietyEscalationPattern = JsonSerializer.Serialize(anxietyRisk.EscalationPattern),
            AnxietyTriggers = JsonSerializer.Serialize(anxietyRisk.Triggers),
            AnxietyImpact = anxietyRisk.Impact,
            InterventionUrgency = riskLevel switch
            {
                RiskLevel.Critical => "Critical",
                RiskLevel.High => "High",
                _ => "Medium"
            },
            ImmediateActions = JsonSerializer.Serialize(GenerateImmediateActions(riskLevel)),
            SupportResources = JsonSerializer.Serialize(GetSupportResources()),
            ProfessionalReferrals = JsonSerializer.Serialize(GetProfessionalReferrals(riskLevel)),
            SafetyMeasures = JsonSerializer.Serialize(GetSafetyMeasures(riskLevel)),
            NextCheckDate = DateTime.UtcNow.AddDays(riskLevel == RiskLevel.Critical ? 1 : 7)
        };

        // Save assessment
        var existing = await _context.CollapseRiskAssessments
            .FirstOrDefaultAsync(a => a.UserId == userId);
        
        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(assessment);
            assessment.Id = existing.Id;
        }
        else
        {
            _context.CollapseRiskAssessments.Add(assessment);
        }

        await _context.SaveChangesAsync();

        return assessment;
    }

    public async Task<List<string>> DetectWarningSignsAsync(string userId)
    {
        var recentSummaries = await _context.DailyEmotionalSummaries
            .Where(s => s.UserId == userId && s.Date >= DateTime.UtcNow.AddDays(-14))
            .OrderByDescending(s => s.Date)
            .ToListAsync();

        return DetectWarningSigns(recentSummaries);
    }

    // Private helper methods

    private double CalculateBurnoutScore(double avgStress, double avgMentalLoad, double avgEnergy)
    {
        // Weighted calculation
        var stressComponent = avgStress * 0.4;
        var loadComponent = avgMentalLoad * 0.4;
        var energyComponent = (100 - avgEnergy) * 0.2; // Low energy increases risk

        return Math.Min(100, stressComponent + loadComponent + energyComponent);
    }

    private List<string> DetectWarningSigns(List<DailyEmotionalSummary> summaries)
    {
        var signs = new List<string>();

        if (summaries.Count < 3) return signs;

        // Declining trend
        var recentAvg = summaries.Take(3).Average(s => s.EmotionalGrowthScore);
        var earlierAvg = summaries.Skip(3).Take(3).Average(s => s.EmotionalGrowthScore);
        if (recentAvg < earlierAvg * 0.9)
            signs.Add("Declining emotional growth trend");

        // High stress persistence
        if (summaries.Count(s => s.StressLevel > 70) > summaries.Count * 0.7)
            signs.Add("Persistently high stress (70%+ of recent days)");

        // Low energy persistence
        if (summaries.Count(s => s.EnergyLevel < 30) > summaries.Count * 0.5)
            signs.Add("Persistently low energy levels");

        // Increasing burnout risk
        var recentBurnout = summaries.Take(3).Average(s => s.BurnoutRisk);
        var earlierBurnout = summaries.Skip(3).Take(3).Average(s => s.BurnoutRisk);
        if (recentBurnout > earlierBurnout * 1.2)
            signs.Add("Increasing burnout risk trend");

        return signs;
    }

    private List<string> GenerateInterventionActions(double burnoutScore, List<string> factors)
    {
        var actions = new List<string>();

        if (burnoutScore > 80)
        {
            actions.Add("🚨 Urgent: Immediate professional support recommended");
            actions.Add("🛑 Take time off if possible - rest is critical");
            actions.Add("💚 Reach out to trusted support network immediately");
        }
        else if (burnoutScore > 60)
        {
            actions.Add("⏸️ Take a break - schedule time for rest and recovery");
            actions.Add("🧘 Practice stress-reduction techniques daily");
            actions.Add("📅 Consider reducing workload or responsibilities");
        }
        else if (burnoutScore > 40)
        {
            actions.Add("💚 Implement regular self-care routines");
            actions.Add("⏰ Set boundaries to protect your energy");
            actions.Add("🎯 Focus on stress management techniques");
        }

        // Add factor-specific actions
        if (factors.Contains("Consistently high stress levels"))
            actions.Add("🧘 Prioritize stress reduction activities (meditation, exercise)");
        if (factors.Contains("High mental load"))
            actions.Add("📋 Consider delegating or simplifying tasks");
        if (factors.Contains("Persistently low energy"))
            actions.Add("🔋 Focus on nutrition, sleep, and physical health");

        return actions;
    }

    private async Task<DepressionRiskAnalysis> CalculateDepressionRiskAsync(string userId)
    {
        var recentSummaries = await _context.DailyEmotionalSummaries
            .Where(s => s.UserId == userId && s.Date >= DateTime.UtcNow.AddDays(-30))
            .ToListAsync();

        var sadnessFrequency = recentSummaries.Count(s => s.CurrentEmotion == "Sad") / (double)Math.Max(1, recentSummaries.Count);
        var lowEnergyFrequency = recentSummaries.Count(s => s.EnergyLevel < 30) / (double)Math.Max(1, recentSummaries.Count);

        var riskScore = ((sadnessFrequency * 50) + (lowEnergyFrequency * 50));

        var symptoms = new List<string>();
        if (sadnessFrequency > 0.5) symptoms.Add("Persistent sadness or low mood");
        if (lowEnergyFrequency > 0.5) symptoms.Add("Persistent low energy or fatigue");

        var triggers = new List<string>(); // Would analyze from life events
        var severity = riskScore switch
        {
            > 70 => "Severe",
            > 50 => "Moderate",
            > 30 => "Mild",
            _ => "None"
        };

        return new DepressionRiskAnalysis
        {
            Score = riskScore,
            Symptoms = symptoms,
            Triggers = triggers,
            Severity = severity
        };
    }

    private async Task<AnxietyRiskAnalysis> CalculateAnxietyRiskAsync(string userId)
    {
        var recentSummaries = await _context.DailyEmotionalSummaries
            .Where(s => s.UserId == userId && s.Date >= DateTime.UtcNow.AddDays(-30))
            .ToListAsync();

        var anxietyFrequency = recentSummaries.Count(s => s.CurrentEmotion == "Anxious") / (double)Math.Max(1, recentSummaries.Count);
        var highStressFrequency = recentSummaries.Count(s => s.StressLevel > 70) / (double)Math.Max(1, recentSummaries.Count);

        var riskScore = ((anxietyFrequency * 50) + (highStressFrequency * 50));

        var escalationPattern = new List<string>();
        if (recentSummaries.Any())
        {
            var recentAnxiety = recentSummaries.Take(7).Count(s => s.CurrentEmotion == "Anxious");
            var earlierAnxiety = recentSummaries.Skip(7).Take(7).Count(s => s.CurrentEmotion == "Anxious");
            if (recentAnxiety > earlierAnxiety * 1.5)
                escalationPattern.Add("Anxiety increasing over recent weeks");
        }

        var triggers = new List<string>(); // Would analyze from life events
        var impact = riskScore switch
        {
            > 70 => "High",
            > 50 => "Medium",
            > 30 => "Low",
            _ => "Minimal"
        };

        return new AnxietyRiskAnalysis
        {
            Score = riskScore,
            EscalationPattern = escalationPattern,
            Triggers = triggers,
            Impact = impact
        };
    }

    private List<string> GenerateImmediateActions(RiskLevel riskLevel)
    {
        return riskLevel switch
        {
            RiskLevel.Critical => new List<string>
            {
                "🚨 Contact crisis support line immediately",
                "🛑 Remove yourself from stressful situations",
                "💚 Reach out to trusted person immediately",
                "🏥 Consider emergency mental health services"
            },
            RiskLevel.High => new List<string>
            {
                "⏸️ Take immediate break from stressors",
                "💚 Contact mental health professional today",
                "🧘 Practice immediate calming techniques"
            },
            _ => new List<string>
            {
                "💚 Schedule professional support consultation",
                "🧘 Implement daily stress-reduction practices"
            }
        };
    }

    private List<string> GetSupportResources()
    {
        return new List<string>
        {
            "Crisis Text Line: Text HOME to 741741",
            "National Suicide Prevention Lifeline: 988",
            "Mental Health America: mhanational.org",
            "Crisis support resources in your area"
        };
    }

    private List<string> GetProfessionalReferrals(RiskLevel riskLevel)
    {
        return riskLevel switch
        {
            RiskLevel.Critical or RiskLevel.High => new List<string>
            {
                "Therapist or counselor (urgent appointment)",
                "Psychiatrist (if medication may be needed)",
                "Crisis intervention services"
            },
            _ => new List<string>
            {
                "Therapist or counselor (regular appointment)",
                "Mental health support groups"
            }
        };
    }

    private List<string> GetSafetyMeasures(RiskLevel riskLevel)
    {
        return riskLevel switch
        {
            RiskLevel.Critical => new List<string>
            {
                "24/7 monitoring or supervision",
                "Safety plan with trusted contacts",
                "Remove access to self-harm means",
                "Emergency contact numbers readily available"
            },
            RiskLevel.High => new List<string>
            {
                "Regular check-ins with support network",
                "Safety plan in place",
                "Crisis support numbers saved"
            },
            _ => new List<string>
            {
                "Regular self-monitoring",
                "Maintain connection with support network"
            }
        };
    }
}

// DTOs
public class DepressionRiskAnalysis
{
    public double Score { get; set; }
    public List<string> Symptoms { get; set; } = new();
    public List<string> Triggers { get; set; } = new();
    public string Severity { get; set; } = "None";
}

public class AnxietyRiskAnalysis
{
    public double Score { get; set; }
    public List<string> EscalationPattern { get; set; } = new();
    public List<string> Triggers { get; set; } = new();
    public string Impact { get; set; } = "Minimal";
}
