using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroSync.Api.Data;
using NeuroSync.Core.Models;
using System.Text.Json;

namespace NeuroSync.Api.Services;

public class LifeMemoryGraphService
{
    private readonly NeuroSyncDbContext _context;
    private readonly ILogger<LifeMemoryGraphService> _logger;

    public LifeMemoryGraphService(
        NeuroSyncDbContext context,
        ILogger<LifeMemoryGraphService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LifeEvent> StoreLifeEventAsync(
        string userId,
        LifeEventType eventType,
        string description,
        double emotionalSignificance = 50,
        LifeImpactLevel lifeImpact = LifeImpactLevel.Medium,
        LifeDomainType? affectedDomain = null,
        List<string>? tags = null)
    {
        var lifeEvent = new LifeEvent
        {
            UserId = userId,
            EventType = eventType,
            Description = description,
            EmotionalSignificance = emotionalSignificance,
            LifeImpact = lifeImpact,
            AffectedDomain = affectedDomain,
            Tags = tags != null ? JsonSerializer.Serialize(tags) : null,
            Timestamp = DateTime.UtcNow
        };

        _context.LifeEvents.Add(lifeEvent);
        await _context.SaveChangesAsync();

        return lifeEvent;
    }

    public async Task<EmotionalNarrativeArc> BuildEmotionalNarrativeAsync(string userId, int months = 6)
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-months);

        var events = await _context.LifeEvents
            .Where(e => e.UserId == userId && e.Timestamp >= cutoffDate)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        if (!events.Any())
        {
            return new EmotionalNarrativeArc
            {
                UserId = userId,
                Period = $"{months} months",
                StartEmotion = "Neutral",
                EndEmotion = "Neutral",
                TransformationLevel = 0,
                Themes = new List<string>()
            };
        }

        // Get emotional trajectory
        var startEvent = events.First();
        var endEvent = events.Last();

        // Identify turning points
        var turningPoints = IdentifyTurningPoints(events);

        // Extract themes
        var themes = ExtractThemes(events);

        // Calculate transformation level
        var transformationLevel = CalculateTransformationLevel(events);

        // Build storytelling narrative
        var storytelling = BuildStorytelling(events, turningPoints, themes);

        return new EmotionalNarrativeArc
        {
            UserId = userId,
            Period = $"{months} months",
            StartEmotion = GetEmotionFromEvent(startEvent),
            EndEmotion = GetEmotionFromEvent(endEvent),
            TurningPoints = turningPoints.Select(tp => tp.Description).ToList(),
            Themes = themes,
            TransformationLevel = transformationLevel,
            Storytelling = storytelling
        };
    }

    public async Task<List<GrowthMilestone>> IdentifyGrowthMilestonesAsync(string userId)
    {
        var events = await _context.LifeEvents
            .Where(e => e.UserId == userId &&
                       (e.EventType == LifeEventType.Growth ||
                        e.EventType == LifeEventType.Milestone ||
                        e.LifeImpact == LifeImpactLevel.Transformative))
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        var milestones = new List<GrowthMilestone>();

        foreach (var evt in events)
        {
            milestones.Add(new GrowthMilestone
            {
                Id = evt.Id,
                Date = evt.Timestamp,
                Type = GetMilestoneType(evt),
                Description = evt.Description,
                EmotionalSignificance = evt.EmotionalSignificance,
                LifeImpact = evt.LifeImpact.ToString()
            });
        }

        return milestones;
    }

    public async Task<string> GenerateLifeStoryAsync(string userId, int months = 12)
    {
        var narrative = await BuildEmotionalNarrativeAsync(userId, months);
        var milestones = await IdentifyGrowthMilestonesAsync(userId);

        var story = $"Over the past {narrative.Period}, your journey has been marked by ";
        
        if (narrative.Themes.Any())
        {
            story += $"themes of {string.Join(", ", narrative.Themes.Take(3))}. ";
        }

        if (narrative.TurningPoints.Any())
        {
            story += $"Key turning points included {string.Join(" and ", narrative.TurningPoints.Take(2))}. ";
        }

        if (milestones.Any())
        {
            story += $"You've achieved {milestones.Count} significant growth milestones, ";
            story += $"including {milestones.Last().Description}. ";
        }

        story += $"Your emotional journey shows ";
        
        if (narrative.EndEmotion != narrative.StartEmotion)
        {
            story += $"a transition from {narrative.StartEmotion} to {narrative.EndEmotion}. ";
        }
        else
        {
            story += $"stability in {narrative.EndEmotion}. ";
        }

        if (narrative.TransformationLevel > 50)
        {
            story += "You've experienced significant transformation and growth.";
        }

        return story;
    }

    // Private helper methods

    private List<TurningPoint> IdentifyTurningPoints(List<LifeEvent> events)
    {
        var turningPoints = new List<TurningPoint>();

        for (int i = 1; i < events.Count - 1; i++)
        {
            var current = events[i];
            var previous = events[i - 1];
            var next = events[i + 1];

            // Significant change in emotional significance
            if (Math.Abs(current.EmotionalSignificance - previous.EmotionalSignificance) > 30)
            {
                turningPoints.Add(new TurningPoint
                {
                    Date = current.Timestamp,
                    Description = current.Description,
                    Type = current.EmotionalSignificance > previous.EmotionalSignificance 
                        ? "Positive" : "Challenging"
                });
            }

            // Transformative events
            if (current.LifeImpact == LifeImpactLevel.Transformative)
            {
                turningPoints.Add(new TurningPoint
                {
                    Date = current.Timestamp,
                    Description = current.Description,
                    Type = "Transformative"
                });
            }
        }

        return turningPoints.OrderBy(tp => tp.Date).ToList();
    }

    private List<string> ExtractThemes(List<LifeEvent> events)
    {
        var themes = new List<string>();

        var growthCount = events.Count(e => e.EventType == LifeEventType.Growth);
        if (growthCount > events.Count * 0.3)
            themes.Add("Personal Growth");

        var traumaCount = events.Count(e => e.EventType == LifeEventType.Trauma);
        var healingCount = events.Count(e => 
            e.EventType == LifeEventType.Growth && 
            e.Description.ToLower().Contains("heal"));
        if (traumaCount > 0 && healingCount > 0)
            themes.Add("Healing & Resilience");

        var relationshipCount = events.Count(e => e.EventType == LifeEventType.Relationship);
        if (relationshipCount > events.Count * 0.3)
            themes.Add("Relationships");

        var crisisCount = events.Count(e => e.EventType == LifeEventType.Crisis);
        if (crisisCount > 0)
            themes.Add("Navigating Challenges");

        var transformativeCount = events.Count(e => e.LifeImpact == LifeImpactLevel.Transformative);
        if (transformativeCount > 0)
            themes.Add("Transformation");

        if (!themes.Any())
            themes.Add("Building Your Story");

        return themes;
    }

    private double CalculateTransformationLevel(List<LifeEvent> events)
    {
        var transformativeEvents = events.Count(e => e.LifeImpact == LifeImpactLevel.Transformative);
        var highImpactEvents = events.Count(e => e.LifeImpact == LifeImpactLevel.High);
        var growthEvents = events.Count(e => e.EventType == LifeEventType.Growth);

        var totalScore = (transformativeEvents * 40) + (highImpactEvents * 20) + (growthEvents * 10);
        var maxPossible = events.Count * 40;

        return maxPossible > 0 ? Math.Min(100, (totalScore / maxPossible) * 100) : 0;
    }

    private List<string> BuildStorytelling(List<LifeEvent> events, List<TurningPoint> turningPoints, List<string> themes)
    {
        var storytelling = new List<string>();

        if (themes.Any())
        {
            storytelling.Add($"Your story is marked by {string.Join(", ", themes.Take(2))}");
        }

        if (turningPoints.Any())
        {
            storytelling.Add($"Key moments include: {string.Join(", ", turningPoints.Take(2).Select(tp => tp.Description))}");
        }

        var growthCount = events.Count(e => e.EventType == LifeEventType.Growth);
        if (growthCount > 0)
        {
            storytelling.Add($"You've shown resilience through {growthCount} growth experiences");
        }

        return storytelling;
    }

    private string GetEmotionFromEvent(LifeEvent evt)
    {
        return evt.EventType switch
        {
            LifeEventType.Trauma => "Sad",
            LifeEventType.Crisis => "Anxious",
            LifeEventType.Growth => "Happy",
            LifeEventType.Milestone => "Excited",
            _ => "Neutral"
        };
    }

    private string GetMilestoneType(LifeEvent evt)
    {
        return evt.LifeImpact switch
        {
            LifeImpactLevel.Transformative => "Transformation",
            LifeImpactLevel.High => "Significant Growth",
            _ => evt.EventType == LifeEventType.Growth ? "Growth Point" : "Milestone"
        };
    }
}

// DTOs
public class EmotionalNarrativeArc
{
    public string UserId { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string StartEmotion { get; set; } = "Neutral";
    public string EndEmotion { get; set; } = "Neutral";
    public List<string> TurningPoints { get; set; } = new();
    public List<string> Themes { get; set; } = new();
    public double TransformationLevel { get; set; } // 0-100
    public List<string> Storytelling { get; set; } = new();
}

public class TurningPoint
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Positive/Challenging/Transformative
}

public class GrowthMilestone
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double EmotionalSignificance { get; set; }
    public string LifeImpact { get; set; } = string.Empty;
}
