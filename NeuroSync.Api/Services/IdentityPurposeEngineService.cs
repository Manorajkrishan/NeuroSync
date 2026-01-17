using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroSync.Api.Data;
using NeuroSync.Core.Models;
using System.Text.Json;

namespace NeuroSync.Api.Services;

public class IdentityPurposeEngineService
{
    private readonly NeuroSyncDbContext _context;
    private readonly ILogger<IdentityPurposeEngineService> _logger;

    public IdentityPurposeEngineService(
        NeuroSyncDbContext context,
        ILogger<IdentityPurposeEngineService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IdentityProfile> ExtractIdentityAsync(string userId)
    {
        var profile = await _context.IdentityProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            // Initialize default profile
            profile = new IdentityProfile
            {
                UserId = userId,
                CoreValues = JsonSerializer.Serialize(new[] { "Growth", "Connection", "Authenticity" }), // Default values
                IdentityClarityScore = 50,
                ConfidenceInSelf = 50,
                PurposeClarityScore = 50,
                PurposeAlignment = 50,
                DirectionConfidence = 50,
                SelfPerception = SelfPerceptionType.Neutral
            };
            _context.IdentityProfiles.Add(profile);
            await _context.SaveChangesAsync();
        }

        // Analyze identity from recent life events and conversations
        profile = await AnalyzeIdentityFromDataAsync(profile);

        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return profile;
    }

    public async Task<PurposeProfile> MapPurposeAsync(string userId)
    {
        var identity = await ExtractIdentityAsync(userId);

        // Extract purpose from identity and life patterns
        var purposeText = !string.IsNullOrEmpty(identity.LifePurpose)
            ? identity.LifePurpose
            : "Discovering purpose through growth and connection"; // Default

        var purposeClarity = identity.PurposeClarityScore;
        var alignment = identity.PurposeAlignment;
        var directionConfidence = identity.DirectionConfidence;

        // Analyze purpose indicators from life events
        var purposeIndicators = await AnalyzePurposeIndicatorsAsync(userId);
        var fulfillmentAreas = await IdentifyFulfillmentAreasAsync(userId);
        var gaps = await IdentifyPurposeGapsAsync(userId, purposeIndicators, fulfillmentAreas);

        return new PurposeProfile
        {
            UserId = userId,
            LifePurpose = purposeText,
            PurposeClarityScore = purposeClarity,
            PurposeAlignment = alignment,
            DirectionConfidence = directionConfidence,
            PurposeIndicators = purposeIndicators,
            FulfillmentAreas = fulfillmentAreas,
            Gaps = gaps
        };
    }

    public async Task<LifeDirectionAnalysis> AnalyzeLifeDirectionAsync(string userId)
    {
        var identity = await ExtractIdentityAsync(userId);
        var purpose = await MapPurposeAsync(userId);

        // Analyze current trajectory from recent life events
        var recentEvents = await _context.LifeEvents
            .Where(e => e.UserId == userId && e.Timestamp >= DateTime.UtcNow.AddMonths(-6))
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();

        var trajectory = AnalyzeTrajectory(recentEvents);
        var alignmentWithValues = CalculateAlignmentWithValues(identity, recentEvents);
        var purposeFulfillment = CalculatePurposeFulfillment(purpose, recentEvents);
        var directionConfidence = identity.DirectionConfidence;

        var recommendedAdjustments = GenerateDirectionAdjustments(
            trajectory, alignmentWithValues, purposeFulfillment, directionConfidence);
        var nextSteps = GenerateNextSteps(alignmentWithValues, purposeFulfillment, purpose.Gaps);

        return new LifeDirectionAnalysis
        {
            UserId = userId,
            CurrentTrajectory = trajectory,
            AlignmentWithValues = alignmentWithValues,
            PurposeFulfillment = purposeFulfillment,
            DirectionConfidence = directionConfidence,
            RecommendedAdjustments = recommendedAdjustments,
            NextSteps = nextSteps
        };
    }

    public async Task<List<IdentityEvolution>> TrackIdentityEvolutionAsync(string userId, int months = 12)
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-months);
        
        var events = await _context.LifeEvents
            .Where(e => e.UserId == userId && 
                       e.Timestamp >= cutoffDate &&
                       (e.EventType == LifeEventType.Growth || 
                        e.EventType == LifeEventType.Milestone ||
                        e.LifeImpact == LifeImpactLevel.Transformative))
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        var evolution = new List<IdentityEvolution>();

        foreach (var evt in events)
        {
            evolution.Add(new IdentityEvolution
            {
                Date = evt.Timestamp,
                EventType = evt.EventType.ToString(),
                Description = evt.Description,
                Impact = evt.LifeImpact.ToString(),
                EmotionalSignificance = evt.EmotionalSignificance
            });
        }

        // Parse evolution timeline if exists
        var profile = await _context.IdentityProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile != null && !string.IsNullOrEmpty(profile.EvolutionTimeline))
        {
            var timelineEvents = JsonSerializer.Deserialize<List<IdentityEvolution>>(profile.EvolutionTimeline) ?? new();
            evolution.AddRange(timelineEvents.Where(e => e.Date >= cutoffDate));
        }

        return evolution.OrderBy(e => e.Date).ToList();
    }

    public async Task<List<string>> GeneratePurposeInsightsAsync(string userId)
    {
        var identity = await ExtractIdentityAsync(userId);
        var purpose = await MapPurposeAsync(userId);
        var direction = await AnalyzeLifeDirectionAsync(userId);

        var insights = new List<string>();

        // Clarity insights
        if (purpose.PurposeClarityScore < 50)
        {
            insights.Add("💭 Your life purpose could be clearer. Consider reflecting on what truly matters to you.");
        }
        else if (purpose.PurposeClarityScore > 75)
        {
            insights.Add($"✨ You have a clear sense of purpose! (Clarity: {purpose.PurposeClarityScore:F0}%)");
        }

        // Alignment insights
        if (direction.AlignmentWithValues < 50)
        {
            insights.Add("🎯 Consider aligning your actions more closely with your core values.");
        }

        // Fulfillment insights
        if (purpose.FulfillmentAreas.Any())
        {
            insights.Add($"💚 You're finding fulfillment in: {string.Join(", ", purpose.FulfillmentAreas)}");
        }

        // Gap insights
        if (purpose.Gaps.Any())
        {
            insights.Add($"🎯 Areas for growth: {string.Join(", ", purpose.Gaps)}");
        }

        // Direction insights
        if (direction.DirectionConfidence < 50)
        {
            insights.Add("🧭 Your direction confidence is low. Exploring your values and purpose may help.");
        }

        return insights;
    }

    // Private helper methods

    private async Task<IdentityProfile> AnalyzeIdentityFromDataAsync(IdentityProfile profile)
    {
        // Analyze from life events
        var recentEvents = await _context.LifeEvents
            .Where(e => e.UserId == profile.UserId && e.Timestamp >= DateTime.UtcNow.AddMonths(-6))
            .ToListAsync();

        // Update clarity score based on consistency in events
        var growthEvents = recentEvents.Count(e => e.EventType == LifeEventType.Growth);
        var totalEvents = recentEvents.Count;
        
        if (totalEvents > 0)
        {
            var growthRatio = (double)growthEvents / totalEvents;
            profile.IdentityClarityScore = Math.Min(100, 50 + (growthRatio * 30));
        }

        // Update confidence based on positive events
        var positiveEvents = recentEvents.Count(e => 
            e.LifeImpact == LifeImpactLevel.High || 
            e.LifeImpact == LifeImpactLevel.Transformative);
        
        if (totalEvents > 0)
        {
            var positiveRatio = (double)positiveEvents / totalEvents;
            profile.ConfidenceInSelf = Math.Min(100, 50 + (positiveRatio * 30));
        }

        return profile;
    }

    private async Task<List<string>> AnalyzePurposeIndicatorsAsync(string userId)
    {
        var events = await _context.LifeEvents
            .Where(e => e.UserId == userId && e.Timestamp >= DateTime.UtcNow.AddMonths(-12))
            .ToListAsync();

        var indicators = new List<string>();

        // Purpose indicators from event patterns
        var growthEvents = events.Count(e => e.EventType == LifeEventType.Growth);
        if (growthEvents > events.Count * 0.3)
            indicators.Add("Consistent personal growth");

        var transformativeEvents = events.Count(e => e.LifeImpact == LifeImpactLevel.Transformative);
        if (transformativeEvents > 0)
            indicators.Add("Transformative life experiences");

        var highSignificanceEvents = events.Count(e => e.EmotionalSignificance > 70);
        if (highSignificanceEvents > 0)
            indicators.Add("Events with deep emotional significance");

        if (!indicators.Any())
            indicators.Add("Exploring and discovering purpose");

        return indicators;
    }

    private async Task<List<string>> IdentifyFulfillmentAreasAsync(string userId)
    {
        var domains = await _context.LifeDomains
            .Where(d => d.UserId == userId && d.EmotionalScore > 70)
            .ToListAsync();

        return domains.Select(d => d.Domain.ToString()).ToList();
    }

    private async Task<List<string>> IdentifyPurposeGapsAsync(string userId, List<string> indicators, List<string> fulfillmentAreas)
    {
        var gaps = new List<string>();

        var allDomains = Enum.GetValues<LifeDomainType>()
            .Where(d => d != LifeDomainType.All)
            .ToList();

        var fulfilledDomains = fulfillmentAreas.Select(a => Enum.Parse<LifeDomainType>(a)).ToList();
        var unfulfilledDomains = allDomains.Except(fulfilledDomains).ToList();

        foreach (var domain in unfulfilledDomains)
        {
            gaps.Add($"{domain} domain needs attention");
        }

        if (gaps.Count == 0 && indicators.Count < 3)
        {
            gaps.Add("Continue exploring what brings you fulfillment");
        }

        return gaps;
    }

    private string AnalyzeTrajectory(List<LifeEvent> events)
    {
        if (!events.Any()) return "Stable - limited recent activity";

        var growthEvents = events.Count(e => e.EventType == LifeEventType.Growth);
        var crisisEvents = events.Count(e => e.EventType == LifeEventType.Crisis || e.EventType == LifeEventType.Trauma);

        if (growthEvents > crisisEvents * 2)
            return "Upward trajectory - significant growth";
        if (crisisEvents > growthEvents * 2)
            return "Challenging period - navigating difficulties";
        if (growthEvents > 0)
            return "Mixed trajectory - growth with challenges";
        
        return "Stable - maintaining current direction";
    }

    private double CalculateAlignmentWithValues(IdentityProfile identity, List<LifeEvent> events)
    {
        // Simple calculation - would be enhanced with actual value extraction
        var positiveEvents = events.Count(e => 
            e.LifeImpact == LifeImpactLevel.High || 
            e.LifeImpact == LifeImpactLevel.Transformative);
        
        var totalEvents = events.Count;
        if (totalEvents == 0) return identity.PurposeAlignment;

        var positiveRatio = (double)positiveEvents / totalEvents;
        return Math.Min(100, 50 + (positiveRatio * 40));
    }

    private double CalculatePurposeFulfillment(PurposeProfile purpose, List<LifeEvent> events)
    {
        // Simple calculation - would be enhanced with purpose-specific analysis
        return purpose.PurposeAlignment * 0.8 + purpose.PurposeClarityScore * 0.2;
    }

    private List<string> GenerateDirectionAdjustments(
        string trajectory,
        double alignment,
        double fulfillment,
        double confidence)
    {
        var adjustments = new List<string>();

        if (alignment < 50)
            adjustments.Add("🎯 Align actions more closely with core values");
        
        if (fulfillment < 50)
            adjustments.Add("💚 Explore activities that bring more fulfillment");
        
        if (confidence < 50)
            adjustments.Add("🧭 Build confidence through small, meaningful actions");
        
        if (trajectory.Contains("Challenging"))
            adjustments.Add("🛡️ Focus on resilience and self-care during difficult periods");

        return adjustments;
    }

    private List<string> GenerateNextSteps(double alignment, double fulfillment, List<string> gaps)
    {
        var steps = new List<string>();

        if (alignment < 50)
            steps.Add("Reflect on core values and how current actions align");
        
        if (fulfillment < 50)
            steps.Add("Experiment with activities in different life domains");
        
        if (gaps.Any())
            steps.Add($"Address gaps: {string.Join(", ", gaps.Take(2))}");

        if (!steps.Any())
            steps.Add("Continue current path with regular self-reflection");

        return steps;
    }
}

// DTOs
public class PurposeProfile
{
    public string UserId { get; set; } = string.Empty;
    public string LifePurpose { get; set; } = string.Empty;
    public double PurposeClarityScore { get; set; } // 0-100
    public double PurposeAlignment { get; set; } // 0-100
    public double DirectionConfidence { get; set; } // 0-100
    public List<string> PurposeIndicators { get; set; } = new();
    public List<string> FulfillmentAreas { get; set; } = new();
    public List<string> Gaps { get; set; } = new();
}

public class LifeDirectionAnalysis
{
    public string UserId { get; set; } = string.Empty;
    public string CurrentTrajectory { get; set; } = string.Empty;
    public double AlignmentWithValues { get; set; } // 0-100
    public double PurposeFulfillment { get; set; } // 0-100
    public double DirectionConfidence { get; set; } // 0-100
    public List<string> RecommendedAdjustments { get; set; } = new();
    public List<string> NextSteps { get; set; } = new();
}

public class IdentityEvolution
{
    public DateTime Date { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public double EmotionalSignificance { get; set; }
}
