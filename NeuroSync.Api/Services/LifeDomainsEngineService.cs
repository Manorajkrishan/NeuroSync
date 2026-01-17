using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroSync.Api.Data;
using NeuroSync.Core.Models;
using System.Text.Json;

namespace NeuroSync.Api.Services;

public class LifeDomainsEngineService
{
    private readonly NeuroSyncDbContext _context;
    private readonly ILogger<LifeDomainsEngineService> _logger;

    public LifeDomainsEngineService(
        NeuroSyncDbContext context,
        ILogger<LifeDomainsEngineService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LifeDomain> GetDomainStateAsync(string userId, LifeDomainType domain)
    {
        var lifeDomain = await _context.LifeDomains
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Domain == domain);

        if (lifeDomain == null)
        {
            // Initialize domain with default state
            lifeDomain = new LifeDomain
            {
                UserId = userId,
                Domain = domain,
                EmotionalScore = 50,
                RiskLevel = "Healthy",
                CurrentState = "Initializing domain...",
                StressLevel = 30
            };
            _context.LifeDomains.Add(lifeDomain);
            await _context.SaveChangesAsync();
        }

        return lifeDomain;
    }

    public async Task<DomainHealthReport> GetDomainHealthReportAsync(string userId)
    {
        var domains = await _context.LifeDomains
            .Where(d => d.UserId == userId)
            .ToListAsync();

        // Ensure all 5 domains exist
        var domainTypes = Enum.GetValues<LifeDomainType>();
        foreach (var domainType in domainTypes)
        {
            if (!domains.Any(d => d.Domain == domainType))
            {
                var newDomain = new LifeDomain
                {
                    UserId = userId,
                    Domain = domainType,
                    EmotionalScore = 50,
                    RiskLevel = "Healthy",
                    CurrentState = "Initializing...",
                    StressLevel = 30
                };
                _context.LifeDomains.Add(newDomain);
                domains.Add(newDomain);
            }
        }

        await _context.SaveChangesAsync();

        // Calculate overall health
        var avgScore = domains.Average(d => d.EmotionalScore);
        var overallHealth = avgScore;

        var criticalIssues = domains
            .Where(d => d.RiskLevel == "Crisis" || d.RiskLevel == "Unhealthy")
            .Select(d => new DomainIssue
            {
                Domain = d.Domain,
                Issue = d.CurrentState,
                RiskLevel = d.RiskLevel,
                StressLevel = d.StressLevel
            })
            .ToList();

        var healthyDomains = domains
            .Where(d => d.RiskLevel == "Healthy" && d.EmotionalScore > 70)
            .Select(d => d.Domain)
            .ToList();

        // Domain relationships (domains affecting each other)
        var domainRelationships = AnalyzeDomainRelationships(domains);

        var recommendations = GenerateDomainRecommendations(domains);

        return new DomainHealthReport
        {
            UserId = userId,
            Domains = domains.Select(d => new DomainStateSummary
            {
                Domain = d.Domain,
                EmotionalScore = d.EmotionalScore,
                RiskLevel = d.RiskLevel,
                StressLevel = d.StressLevel,
                CurrentState = d.CurrentState
            }).ToList(),
            OverallHealthScore = overallHealth,
            CriticalIssues = criticalIssues,
            HealthyDomains = healthyDomains,
            DomainRelationships = domainRelationships,
            Recommendations = recommendations
        };
    }

    public async Task<DomainStressAnalysis> AnalyzeDomainStressAsync(string userId, LifeDomainType domain)
    {
        var lifeDomain = await GetDomainStateAsync(userId, domain);

        // Parse stress triggers from JSON
        var stressTriggers = new List<string>();
        if (!string.IsNullOrEmpty(lifeDomain.StressTriggers))
        {
            try
            {
                stressTriggers = JsonSerializer.Deserialize<List<string>>(lifeDomain.StressTriggers) ?? new();
            }
            catch { }
        }

        // Get recent life events affecting this domain
        var recentEvents = await _context.LifeEvents
            .Where(e => e.UserId == userId && 
                       e.AffectedDomain == domain &&
                       e.Timestamp >= DateTime.UtcNow.AddDays(-30))
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();

        var impactAnalysis = AnalyzeDomainImpact(recentEvents, lifeDomain.StressLevel);

        var reliefStrategies = GenerateReliefStrategies(domain, lifeDomain.StressLevel, stressTriggers);

        return new DomainStressAnalysis
        {
            Domain = domain,
            StressLevel = lifeDomain.StressLevel,
            StressTriggers = stressTriggers,
            StressPatterns = AnalyzeStressPatterns(recentEvents),
            ImpactAnalysis = impactAnalysis,
            ReliefStrategies = reliefStrategies
        };
    }

    public async Task<List<DomainAction>> GetDomainActionsAsync(string userId, LifeDomainType domain)
    {
        var lifeDomain = await GetDomainStateAsync(userId, domain);

        var actions = new List<DomainAction>();

        // Immediate actions based on risk level
        if (lifeDomain.RiskLevel == "Crisis" || lifeDomain.RiskLevel == "Unhealthy")
        {
            actions.Add(new DomainAction
            {
                Type = ActionType.Immediate,
                Priority = "High",
                Title = "Seek Professional Support",
                Description = $"Your {domain} domain is in a critical state. Consider reaching out to professionals.",
                Category = "Support"
            });
        }

        // Actions based on stress level
        if (lifeDomain.StressLevel > 70)
        {
            actions.Add(new DomainAction
            {
                Type = ActionType.Immediate,
                Priority = "High",
                Title = "Reduce Stress Immediately",
                Description = $"High stress in {domain} domain. Practice stress-reduction techniques.",
                Category = "Stress Management"
            });
        }

        // Long-term strategies
        actions.Add(new DomainAction
        {
            Type = ActionType.LongTerm,
            Priority = "Medium",
            Title = $"Develop {domain} Resilience",
            Description = $"Build long-term strength in your {domain} domain through consistent practices.",
            Category = "Growth"
        });

        // Preventive measures
        actions.Add(new DomainAction
        {
            Type = ActionType.Preventive,
            Priority = "Medium",
            Title = "Monitor & Maintain",
            Description = $"Regularly check in on your {domain} domain to maintain health.",
            Category = "Maintenance"
        });

        return actions;
    }

    // Private helper methods

    private List<DomainRelationship> AnalyzeDomainRelationships(List<LifeDomain> domains)
    {
        var relationships = new List<DomainRelationship>();

        // Example: Mental health affects all other domains
        var mentalHealth = domains.FirstOrDefault(d => d.Domain == LifeDomainType.MentalHealth);
        if (mentalHealth != null && mentalHealth.StressLevel > 60)
        {
            relationships.Add(new DomainRelationship
            {
                SourceDomain = LifeDomainType.MentalHealth,
                TargetDomain = LifeDomainType.All,
                Impact = "High mental health stress affects all life domains",
                Recommendation = "Prioritize mental health support"
            });
        }

        // Money stress affects relationships
        var moneyDomain = domains.FirstOrDefault(d => d.Domain == LifeDomainType.MoneySurvival);
        var relationshipDomain = domains.FirstOrDefault(d => d.Domain == LifeDomainType.Relationships);
        if (moneyDomain != null && relationshipDomain != null && moneyDomain.StressLevel > 70)
        {
            relationships.Add(new DomainRelationship
            {
                SourceDomain = LifeDomainType.MoneySurvival,
                TargetDomain = LifeDomainType.Relationships,
                Impact = "Financial stress can strain relationships",
                Recommendation = "Address financial concerns and communicate with loved ones"
            });
        }

        return relationships;
    }

    private List<string> GenerateDomainRecommendations(List<LifeDomain> domains)
    {
        var recommendations = new List<string>();

        var criticalDomains = domains.Where(d => d.RiskLevel == "Crisis").ToList();
        if (criticalDomains.Any())
        {
            recommendations.Add($"🎯 Urgent: {criticalDomains.Count} domain(s) need immediate attention");
        }

        var highStressDomains = domains.Where(d => d.StressLevel > 70).OrderByDescending(d => d.StressLevel).Take(2).ToList();
        foreach (var domain in highStressDomains)
        {
            recommendations.Add($"⚡ Focus on reducing stress in {domain.Domain} domain (current: {domain.StressLevel:F0}%)");
        }

        var healthyDomains = domains.Where(d => d.RiskLevel == "Healthy" && d.EmotionalScore > 70).ToList();
        if (healthyDomains.Any())
        {
            recommendations.Add($"💚 {healthyDomains.Count} domain(s) are healthy - leverage strengths");
        }

        return recommendations;
    }

    private string AnalyzeStressPatterns(List<LifeEvent> events)
    {
        if (!events.Any()) return "No recent stress patterns detected";

        var highImpactEvents = events.Where(e => e.LifeImpact >= LifeImpactLevel.High).Count();
        var negativeEvents = events.Where(e => e.EventType == LifeEventType.Trauma || e.EventType == LifeEventType.Crisis).Count();

        if (negativeEvents > highImpactEvents * 0.5)
        {
            return "Recent pattern of challenging events detected";
        }

        return "Stress patterns within normal range";
    }

    private string AnalyzeDomainImpact(List<LifeEvent> events, double currentStress)
    {
        var impact = events.Sum(e => (int)e.LifeImpact);
        var severity = impact > 50 ? "High" : impact > 20 ? "Medium" : "Low";

        return $"Domain impact: {severity} (from {events.Count} recent events, current stress: {currentStress:F0}%)";
    }

    private List<string> GenerateReliefStrategies(LifeDomainType domain, double stressLevel, List<string> triggers)
    {
        var strategies = new List<string>();

        var domainSpecificStrategies = domain switch
        {
            LifeDomainType.MentalHealth => new[] { "Therapy or counseling", "Meditation and mindfulness", "Self-care routines" },
            LifeDomainType.Relationships => new[] { "Open communication", "Quality time together", "Boundary setting" },
            LifeDomainType.CareerWork => new[] { "Work-life balance", "Skill development", "Career counseling" },
            LifeDomainType.MoneySurvival => new[] { "Budget planning", "Financial counseling", "Income diversification" },
            LifeDomainType.SelfGrowth => new[] { "Personal development", "Goal setting", "Reflection practices" },
            _ => Array.Empty<string>()
        };

        strategies.AddRange(domainSpecificStrategies);

        if (stressLevel > 80)
        {
            strategies.Insert(0, "🚨 Urgent: Professional support recommended");
        }

        return strategies;
    }
}

// DTOs
public class DomainHealthReport
{
    public string UserId { get; set; } = string.Empty;
    public List<DomainStateSummary> Domains { get; set; } = new();
    public double OverallHealthScore { get; set; }
    public List<DomainIssue> CriticalIssues { get; set; } = new();
    public List<LifeDomainType> HealthyDomains { get; set; } = new();
    public List<DomainRelationship> DomainRelationships { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class DomainStateSummary
{
    public LifeDomainType Domain { get; set; }
    public double EmotionalScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public double StressLevel { get; set; }
    public string CurrentState { get; set; } = string.Empty;
}

public class DomainIssue
{
    public LifeDomainType Domain { get; set; }
    public string Issue { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public double StressLevel { get; set; }
}

public class DomainRelationship
{
    public LifeDomainType SourceDomain { get; set; }
    public LifeDomainType TargetDomain { get; set; }
    public string Impact { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

public class DomainStressAnalysis
{
    public LifeDomainType Domain { get; set; }
    public double StressLevel { get; set; }
    public List<string> StressTriggers { get; set; } = new();
    public string StressPatterns { get; set; } = string.Empty;
    public string ImpactAnalysis { get; set; } = string.Empty;
    public List<string> ReliefStrategies { get; set; } = new();
}

public class DomainAction
{
    public ActionType Type { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public enum ActionType
{
    Immediate = 0,
    LongTerm = 1,
    Preventive = 2
}
