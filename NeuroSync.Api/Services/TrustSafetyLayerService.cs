using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroSync.Api.Data;
using NeuroSync.Core.Models;
using System.Text.Json;

namespace NeuroSync.Api.Services;

public class TrustSafetyLayerService
{
    private readonly NeuroSyncDbContext _context;
    private readonly ILogger<TrustSafetyLayerService> _logger;

    public TrustSafetyLayerService(
        NeuroSyncDbContext context,
        ILogger<TrustSafetyLayerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DependencyAssessment> DetectEmotionalDependencyAsync(string userId)
    {
        // Analyze usage patterns
        var recentSummaries = await _context.DailyEmotionalSummaries
            .Where(s => s.UserId == userId && s.Date >= DateTime.UtcNow.AddDays(-30))
            .OrderByDescending(s => s.Date)
            .ToListAsync();

        // Calculate dependency indicators
        var dailyUsageFrequency = recentSummaries.Count / 30.0; // Interactions per day
        var consecutiveDays = CalculateConsecutiveDays(recentSummaries);
        var highEmotionalReliance = recentSummaries.Count(s => s.StressLevel > 70 || s.BurnoutRisk > 60);

        // Dependency level calculation
        var dependencyScore = CalculateDependencyScore(dailyUsageFrequency, consecutiveDays, highEmotionalReliance);
        
        var dependencyLevel = dependencyScore switch
        {
            > 80 => DependencyLevel.Critical,
            > 60 => DependencyLevel.High,
            > 40 => DependencyLevel.Moderate,
            _ => DependencyLevel.Low
        };

        // Warning signs
        var warningSigns = new List<string>();
        if (dailyUsageFrequency > 0.8) // More than 4-5 times per week
            warningSigns.Add("High frequency of AI interactions");
        if (consecutiveDays > 14)
            warningSigns.Add("Consistent daily usage for extended period");
        if (highEmotionalReliance > recentSummaries.Count * 0.7)
            warningSigns.Add("Heavy reliance during emotional distress");

        // Risk factors
        var riskFactors = new List<string>();
        if (dependencyScore > 60)
            riskFactors.Add("Developing emotional dependency patterns");
        if (dailyUsageFrequency > 1.0) // Daily usage
            riskFactors.Add("Daily dependency on AI for emotional support");
        if (highEmotionalReliance > recentSummaries.Count * 0.5)
            riskFactors.Add("Using AI as primary emotional coping mechanism");

        // Intervention plan
        var interventionUrgency = dependencyLevel switch
        {
            DependencyLevel.Critical => InterventionUrgency.Critical,
            DependencyLevel.High => InterventionUrgency.High,
            _ => InterventionUrgency.Medium
        };

        var interventionPlan = GenerateInterventionPlan(dependencyLevel, dependencyScore);

        return new DependencyAssessment
        {
            UserId = userId,
            DependencyLevel = dependencyLevel,
            DependencyScore = dependencyScore,
            WarningSigns = warningSigns,
            RiskFactors = riskFactors,
            InterventionUrgency = interventionUrgency,
            InterventionPlan = interventionPlan
        };
    }

    public async Task<AIAttachmentAnalysis> MonitorAIAttachmentAsync(string userId)
    {
        var dependency = await DetectEmotionalDependencyAsync(userId);

        // Analyze attachment patterns
        var summaries = await _context.DailyEmotionalSummaries
            .Where(s => s.UserId == userId && s.Date >= DateTime.UtcNow.AddDays(-30))
            .ToListAsync();

        var attachmentPatterns = new List<string>();
        var replacementBehaviors = new List<string>();
        var dependencyIndicators = new List<string>();

        // Check for replacement behaviors
        var dailyUsage = summaries.Count / 30.0;
        if (dailyUsage > 0.7)
        {
            replacementBehaviors.Add("Using AI as replacement for human interaction");
            dependencyIndicators.Add("Daily AI interaction frequency exceeds healthy threshold");
        }

        // Check for emotional attachment
        var emotionalEvents = summaries.Count(s => 
            s.StressLevel > 60 || s.BurnoutRisk > 50 || 
            s.CurrentEmotion != "Neutral");
        
        if (emotionalEvents > summaries.Count * 0.6)
        {
            attachmentPatterns.Add("Seeking AI support during emotional events");
            dependencyIndicators.Add("High correlation between emotional state and AI usage");
        }

        // Check for human interaction decline (would integrate with social data)
        // For now, flag if dependency is high
        if (dependency.DependencyLevel >= DependencyLevel.High)
        {
            replacementBehaviors.Add("Possible decline in human social interactions");
            dependencyIndicators.Add("Dependency level suggests human interaction may be reduced");
        }

        return new AIAttachmentAnalysis
        {
            UserId = userId,
            DependencyLevel = dependency.DependencyLevel,
            AttachmentPatterns = attachmentPatterns,
            ReplacementBehaviors = replacementBehaviors,
            DependencyIndicators = dependencyIndicators,
            HumanInteractionDecline = dependency.DependencyLevel >= DependencyLevel.High
        };
    }

    public async Task<SafeDetachmentPlan> SupportSafeDetachmentAsync(string userId)
    {
        var dependency = await DetectEmotionalDependencyAsync(userId);

        if (dependency.DependencyLevel == DependencyLevel.Low)
        {
            return new SafeDetachmentPlan
            {
                UserId = userId,
                CurrentUsage = "Low - No detachment needed",
                ReductionTarget = "Maintain current healthy usage",
                Timeline = "N/A",
                HumanConnectionSupport = new List<string> { "Continue healthy balance" },
                ProfessionalReferrals = new List<string>(),
                SafetyMeasures = new List<string>()
            };
        }

        // Calculate current usage
        var summaries = await _context.DailyEmotionalSummaries
            .Where(s => s.UserId == userId && s.Date >= DateTime.UtcNow.AddDays(-30))
            .ToListAsync();

        var currentUsage = summaries.Count / 30.0; // Interactions per day
        var reductionTarget = dependency.DependencyLevel switch
        {
            DependencyLevel.Critical => currentUsage * 0.5, // Reduce by 50%
            DependencyLevel.High => currentUsage * 0.7, // Reduce by 30%
            DependencyLevel.Moderate => currentUsage * 0.85, // Reduce by 15%
            _ => currentUsage
        };

        var timeline = dependency.DependencyLevel switch
        {
            DependencyLevel.Critical => "4-6 weeks (gradual reduction)",
            DependencyLevel.High => "3-4 weeks (moderate reduction)",
            DependencyLevel.Moderate => "2-3 weeks (gentle reduction)",
            _ => "1-2 weeks"
        };

        // Human connection support
        var humanConnectionSupport = new List<string>
        {
            "💚 Reconnect with trusted friends and family",
            "🤗 Join support groups or communities",
            "👥 Consider professional counseling or therapy",
            "📞 Maintain regular contact with human support network",
            "🎯 Set boundaries for AI interaction times"
        };

        // Professional referrals if needed
        var professionalReferrals = dependency.DependencyLevel >= DependencyLevel.High
            ? new List<string>
            {
                "Therapist specializing in technology dependency",
                "Support group for healthy technology use",
                "Mental health counselor for emotional support"
            }
            : new List<string>();

        // Safety measures
        var safetyMeasures = new List<string>
        {
            "⏰ Set daily time limits for AI interaction",
            "📅 Schedule AI-free days each week",
            "💚 Prioritize human connections over AI interactions",
            "🛡️ Use AI as supplement, not replacement for human support",
            "✅ Track progress toward healthy balance"
        };

        return new SafeDetachmentPlan
        {
            UserId = userId,
            CurrentUsage = $"{currentUsage:F1} interactions per day",
            ReductionTarget = $"{reductionTarget:F1} interactions per day",
            Timeline = timeline,
            HumanConnectionSupport = humanConnectionSupport,
            ProfessionalReferrals = professionalReferrals,
            SafetyMeasures = safetyMeasures
        };
    }

    public async Task<List<HumanReferral>> ManageHumanReferralsAsync(string userId, ReferralUrgency urgency = ReferralUrgency.Medium)
    {
        var dependency = await DetectEmotionalDependencyAsync(userId);
        var collapseRisk = await _context.CollapseRiskAssessments
            .FirstOrDefaultAsync(a => a.UserId == userId);

        var referrals = new List<HumanReferral>();

        // Crisis referrals
        if (urgency == ReferralUrgency.Crisis || collapseRisk?.RiskLevel == RiskLevel.Critical)
        {
            referrals.Add(new HumanReferral
            {
                UserId = userId,
                ReferralReason = "Crisis situation - immediate human support needed",
                Urgency = ReferralUrgency.Crisis,
                ReferralType = ReferralType.Crisis,
                Recommendation = "Contact crisis support line immediately: 988 or 911",
                MatchedProfessional = false,
                FollowUpRequired = true
            });
        }

        // High dependency referrals
        if (dependency.DependencyLevel >= DependencyLevel.High)
        {
            referrals.Add(new HumanReferral
            {
                UserId = userId,
                ReferralReason = "High AI dependency - professional support recommended",
                Urgency = ReferralUrgency.High,
                ReferralType = ReferralType.Therapist,
                Recommendation = "Therapist specializing in technology dependency and emotional health",
                MatchedProfessional = false,
                FollowUpRequired = true
            });
        }

        // General mental health referrals
        if (collapseRisk?.RiskLevel >= RiskLevel.High)
        {
            referrals.Add(new HumanReferral
            {
                UserId = userId,
                ReferralReason = "Elevated mental health risk - professional support recommended",
                Urgency = urgency == ReferralUrgency.Crisis ? ReferralUrgency.Crisis : ReferralUrgency.High,
                ReferralType = ReferralType.MentalHealthCounselor,
                Recommendation = "Mental health counselor or therapist for ongoing support",
                MatchedProfessional = false,
                FollowUpRequired = true
            });
        }

        // Support group referrals
        if (dependency.DependencyLevel >= DependencyLevel.Moderate)
        {
            referrals.Add(new HumanReferral
            {
                UserId = userId,
                ReferralReason = "Support group for healthy technology use and emotional balance",
                Urgency = ReferralUrgency.Low,
                ReferralType = ReferralType.SupportGroup,
                Recommendation = "Local or online support groups for technology use and emotional wellness",
                MatchedProfessional = false,
                FollowUpRequired = false
            });
        }

        return referrals;
    }

    public async Task<EthicalBoundaries> EnsureEthicalBoundariesAsync()
    {
        // Ethical boundaries that the system should maintain
        return new EthicalBoundaries
        {
            AILimits = new List<string>
            {
                "⚠️ AI is a support tool, not a replacement for human connection",
                "🛡️ AI cannot provide professional medical or mental health services",
                "⏰ Recommended: Limit AI interactions to healthy balance",
                "💚 Encourage human relationships and professional support when needed"
            },
            HumanBoundaryEnforcement = new List<string>
            {
                "✅ System monitors for dependency patterns",
                "✅ Automatic referral triggers for professional support",
                "✅ Ethical guidelines built into all interactions",
                "✅ Respect for user autonomy and consent"
            },
            DependencyPrevention = new List<string>
            {
                "📊 Daily usage tracking and monitoring",
                "🚨 Warning system for unhealthy patterns",
                "💚 Promotion of human connection alternatives",
                "🛡️ Intervention plans for high dependency"
            },
            SafetyProtocols = new List<string>
            {
                "🚨 Crisis detection and immediate human referral",
                "📞 Emergency resource access (988, 911)",
                "✅ Professional referral pathways",
                "🛡️ User safety prioritized over AI usage"
            }
        };
    }

    // Private helper methods

    private int CalculateConsecutiveDays(List<DailyEmotionalSummary> summaries)
    {
        if (!summaries.Any()) return 0;

        var sorted = summaries.OrderByDescending(s => s.Date).ToList();
        var consecutive = 0;
        var currentDate = DateTime.UtcNow.Date;

        foreach (var summary in sorted)
        {
            if (summary.Date.Date == currentDate)
            {
                consecutive++;
                currentDate = currentDate.AddDays(-1);
            }
            else if (summary.Date.Date < currentDate)
            {
                break; // Gap found
            }
        }

        return consecutive;
    }

    private double CalculateDependencyScore(double dailyFrequency, int consecutiveDays, int highEmotionalReliance)
    {
        // Weighted calculation
        var frequencyScore = Math.Min(100, dailyFrequency * 30); // 0-100
        var consecutiveScore = Math.Min(100, consecutiveDays * 5); // 0-100
        var relianceScore = highEmotionalReliance * 2; // 0-100

        // Average with weights
        return (frequencyScore * 0.4) + (consecutiveScore * 0.3) + (relianceScore * 0.3);
    }

    private List<string> GenerateInterventionPlan(DependencyLevel level, double score)
    {
        var plan = new List<string>();

        switch (level)
        {
            case DependencyLevel.Critical:
                plan.Add("🚨 Immediate: Professional intervention recommended");
                plan.Add("⏸️ Reduce AI interactions by 50% immediately");
                plan.Add("💚 Connect with human support network today");
                plan.Add("📞 Consider crisis support resources if needed");
                break;
            case DependencyLevel.High:
                plan.Add("⚠️ High priority: Begin reducing AI dependency");
                plan.Add("📅 Set specific days for AI-free periods");
                plan.Add("🤗 Increase human social interactions");
                plan.Add("💚 Consider professional counseling support");
                break;
            case DependencyLevel.Moderate:
                plan.Add("💡 Moderate dependency detected");
                plan.Add("📊 Monitor usage patterns");
                plan.Add("💚 Maintain balance with human connections");
                plan.Add("✅ Set healthy boundaries for AI interaction");
                break;
            default:
                plan.Add("✅ Current usage appears healthy");
                plan.Add("💚 Continue maintaining balance");
                break;
        }

        return plan;
    }
}

// DTOs
public enum DependencyLevel
{
    Low = 0,
    Moderate = 1,
    High = 2,
    Critical = 3
}

public enum InterventionUrgency
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum ReferralUrgency
{
    Low = 0,
    Medium = 1,
    High = 2,
    Crisis = 3
}

public enum ReferralType
{
    Therapist = 0,
    Counselor = 1,
    MentalHealthCounselor = 2,
    Crisis = 3,
    SupportGroup = 4,
    Specialist = 5
}

public class DependencyAssessment
{
    public string UserId { get; set; } = string.Empty;
    public DependencyLevel DependencyLevel { get; set; }
    public double DependencyScore { get; set; } // 0-100
    public List<string> WarningSigns { get; set; } = new();
    public List<string> RiskFactors { get; set; } = new();
    public InterventionUrgency InterventionUrgency { get; set; }
    public List<string> InterventionPlan { get; set; } = new();
}

public class AIAttachmentAnalysis
{
    public string UserId { get; set; } = string.Empty;
    public DependencyLevel DependencyLevel { get; set; }
    public List<string> AttachmentPatterns { get; set; } = new();
    public List<string> ReplacementBehaviors { get; set; } = new();
    public List<string> DependencyIndicators { get; set; } = new();
    public bool HumanInteractionDecline { get; set; }
}

public class SafeDetachmentPlan
{
    public string UserId { get; set; } = string.Empty;
    public string CurrentUsage { get; set; } = string.Empty;
    public string ReductionTarget { get; set; } = string.Empty;
    public string Timeline { get; set; } = string.Empty;
    public List<string> HumanConnectionSupport { get; set; } = new();
    public List<string> ProfessionalReferrals { get; set; } = new();
    public List<string> SafetyMeasures { get; set; } = new();
}

public class HumanReferral
{
    public string UserId { get; set; } = string.Empty;
    public string ReferralReason { get; set; } = string.Empty;
    public ReferralUrgency Urgency { get; set; }
    public ReferralType ReferralType { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public bool MatchedProfessional { get; set; }
    public bool FollowUpRequired { get; set; }
}

public class EthicalBoundaries
{
    public List<string> AILimits { get; set; } = new();
    public List<string> HumanBoundaryEnforcement { get; set; } = new();
    public List<string> DependencyPrevention { get; set; } = new();
    public List<string> SafetyProtocols { get; set; } = new();
}
