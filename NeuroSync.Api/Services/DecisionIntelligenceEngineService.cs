using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroSync.Api.Data;
using NeuroSync.Core.Models;
using System.Text.Json;

namespace NeuroSync.Api.Services;

public class DecisionIntelligenceEngineService
{
    private readonly NeuroSyncDbContext _context;
    private readonly ILogger<DecisionIntelligenceEngineService> _logger;

    public DecisionIntelligenceEngineService(
        NeuroSyncDbContext context,
        ILogger<DecisionIntelligenceEngineService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Decision> FrameDecisionAsync(string userId, string decisionText)
    {
        var decisionType = ClassifyDecisionType(decisionText);
        var stakes = AssessStakes(decisionText);

        var decision = new Decision
        {
            UserId = userId,
            DecisionText = decisionText,
            DecisionType = decisionType,
            Stakes = stakes,
            Status = DecisionStatus.Active
        };

        _context.Decisions.Add(decision);
        await _context.SaveChangesAsync();

        return decision;
    }

    public async Task<DecisionAnalysis> AnalyzeDecisionOptionsAsync(
        string userId, 
        int decisionId, 
        List<string> options)
    {
        var decision = await _context.Decisions
            .Include(d => d.Options)
            .FirstOrDefaultAsync(d => d.Id == decisionId && d.UserId == userId);

        if (decision == null)
        {
            throw new ArgumentException($"Decision {decisionId} not found for user {userId}");
        }

        // Remove existing options
        _context.DecisionOptions.RemoveRange(decision.Options);

        // Analyze each option
        var analyzedOptions = new List<DecisionOption>();
        DecisionOption? recommendedOption = null;
        var maxValueAlignment = 0.0;

        foreach (var optionText in options)
        {
            var option = await AnalyzeOptionAsync(decision, optionText);
            analyzedOptions.Add(option);

            if (option.ValueAlignment > maxValueAlignment)
            {
                maxValueAlignment = option.ValueAlignment;
                recommendedOption = option;
            }
        }

        // Mark recommended option
        if (recommendedOption != null)
        {
            recommendedOption.IsRecommended = true;
        }

        decision.Options = analyzedOptions;
        decision.Analysis = JsonSerializer.Serialize(new
        {
            TotalOptions = analyzedOptions.Count,
            RecommendedOptionId = recommendedOption?.Id,
            AnalysisDate = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return BuildDecisionAnalysis(decision, analyzedOptions);
    }

    public async Task<EmotionalOutcomePrediction> PredictEmotionalOutcomeAsync(
        string userId,
        int decisionId,
        int optionId,
        Timeframe timeframe)
    {
        var decision = await _context.Decisions
            .Include(d => d.Options)
            .FirstOrDefaultAsync(d => d.Id == decisionId && d.UserId == userId);

        var option = decision?.Options.FirstOrDefault(o => o.Id == optionId);
        if (option == null)
        {
            throw new ArgumentException($"Option {optionId} not found for decision {decisionId}");
        }

        // Predict emotional outcomes
        var outcome = PredictOutcomesForTimeframe(option, decision!, timeframe);

        // Parse predicted outcomes
        var outcomes = !string.IsNullOrEmpty(option.PredictedOutcomes)
            ? JsonSerializer.Deserialize<Dictionary<string, EmotionalOutcome>>(option.PredictedOutcomes) ?? new()
            : new();

        outcomes[timeframe.ToString()] = outcome;

        option.PredictedOutcomes = JsonSerializer.Serialize(outcomes);
        await _context.SaveChangesAsync();

        // Return as EmotionalOutcomePrediction
        return new EmotionalOutcomePrediction
        {
            Timeframe = timeframe,
            Outcome = outcome
        };
    }

    public async Task<DecisionScenarioModel> ModelDecisionScenariosAsync(string userId, int decisionId)
    {
        var decision = await _context.Decisions
            .Include(d => d.Options)
            .FirstOrDefaultAsync(d => d.Id == decisionId && d.UserId == userId);

        if (decision == null || !decision.Options.Any())
        {
            throw new ArgumentException($"Decision {decisionId} not found or has no options");
        }

        var scenarios = new DecisionScenarioModel
        {
            DecisionId = decisionId,
            BestCase = ModelBestCaseScenario(decision),
            WorstCase = ModelWorstCaseScenario(decision),
            MostLikely = ModelMostLikelyScenario(decision)
        };

        return scenarios;
    }

    // Private helper methods

    private DecisionType ClassifyDecisionType(string decisionText)
    {
        var text = decisionText.ToLower();
        
        if (text.Contains("job") || text.Contains("career") || text.Contains("work") || text.Contains("employ"))
            return DecisionType.Career;
        if (text.Contains("relationship") || text.Contains("partner") || text.Contains("marry") || text.Contains("breakup"))
            return DecisionType.Relationship;
        if (text.Contains("money") || text.Contains("financial") || text.Contains("buy") || text.Contains("invest"))
            return DecisionType.Financial;
        if (text.Contains("crisis") || text.Contains("emergency") || text.Contains("urgent"))
            return DecisionType.Crisis;
        
        return DecisionType.Life;
    }

    private StakesLevel AssessStakes(string decisionText)
    {
        var text = decisionText.ToLower();
        
        if (text.Contains("critical") || text.Contains("life") || text.Contains("death") || text.Contains("divorce"))
            return StakesLevel.Critical;
        if (text.Contains("major") || text.Contains("important") || text.Contains("significant"))
            return StakesLevel.High;
        if (text.Contains("minor") || text.Contains("small") || text.Contains("trivial"))
            return StakesLevel.Low;
        
        return StakesLevel.Medium;
    }

    private async Task<DecisionOption> AnalyzeOptionAsync(Decision decision, string optionText)
    {
        var option = new DecisionOption
        {
            DecisionId = decision.Id,
            OptionText = optionText
        };

        // Analyze emotional pros/cons
        var (pros, cons) = AnalyzeEmotionalAspects(optionText, decision.DecisionType);
        option.EmotionalPros = JsonSerializer.Serialize(pros);
        option.EmotionalCons = JsonSerializer.Serialize(cons);

        // Predict outcomes for different timeframes
        var outcomes = new Dictionary<string, EmotionalOutcome>
        {
            ["Short"] = PredictOutcomesForTimeframe(option, decision, Timeframe.Short),
            ["Medium"] = PredictOutcomesForTimeframe(option, decision, Timeframe.Medium),
            ["Long"] = PredictOutcomesForTimeframe(option, decision, Timeframe.Long)
        };
        option.PredictedOutcomes = JsonSerializer.Serialize(outcomes);

        // Calculate regret probability
        option.RegretProbability = CalculateRegretProbability(pros, cons, decision.Stakes);

        // Assess risk level
        option.RiskLevel = AssessRiskLevel(option.RegretProbability, decision.Stakes);

        // Calculate value alignment (would integrate with IdentityProfile)
        option.ValueAlignment = await CalculateValueAlignmentAsync(decision.UserId, optionText);

        _context.DecisionOptions.Add(option);

        return option;
    }

    private (List<string> Pros, List<string> Cons) AnalyzeEmotionalAspects(string optionText, DecisionType type)
    {
        var pros = new List<string>();
        var cons = new List<string>();

        // Simple rule-based analysis - would be enhanced with ML/NLP
        var text = optionText.ToLower();

        // Common emotional pros
        if (text.Contains("growth") || text.Contains("learn") || text.Contains("develop"))
            pros.Add("Promotes personal growth");
        if (text.Contains("happ") || text.Contains("fulfill") || text.Contains("joy"))
            pros.Add("May increase happiness");
        if (text.Contains("stabilit") || text.Contains("security") || text.Contains("safe"))
            pros.Add("Provides stability and security");

        // Common emotional cons
        if (text.Contains("risk") || text.Contains("uncertain") || text.Contains("unknown"))
            cons.Add("Involves uncertainty and risk");
        if (text.Contains("stress") || text.Contains("pressure") || text.Contains("difficult"))
            cons.Add("May increase stress levels");
        if (text.Contains("change") || text.Contains("different"))
            cons.Add("Requires significant change and adaptation");

        if (!pros.Any()) pros.Add("No specific emotional benefits identified");
        if (!cons.Any()) cons.Add("No specific emotional drawbacks identified");

        return (pros, cons);
    }

    private EmotionalOutcome PredictOutcomesForTimeframe(DecisionOption option, Decision decision, Timeframe timeframe)
    {
        var pros = !string.IsNullOrEmpty(option.EmotionalPros)
            ? JsonSerializer.Deserialize<List<string>>(option.EmotionalPros) ?? new()
            : new();
        var cons = !string.IsNullOrEmpty(option.EmotionalCons)
            ? JsonSerializer.Deserialize<List<string>>(option.EmotionalCons) ?? new()
            : new();

        // Simple prediction based on pros/cons balance
        var prosCount = pros.Count;
        var consCount = cons.Count;
        var balance = prosCount - consCount;

        var timeframeMultiplier = timeframe switch
        {
            Timeframe.Short => 1.0,
            Timeframe.Medium => 0.7,
            Timeframe.Long => 0.5,
            _ => 1.0
        };

        var emotionalScore = 50 + (balance * 10 * timeframeMultiplier);
        emotionalScore = Math.Max(0, Math.Min(100, emotionalScore));

        var primaryEmotion = emotionalScore switch
        {
            > 70 => "Happy",
            > 50 => "Neutral",
            > 30 => "Anxious",
            _ => "Sad"
        };

        return new EmotionalOutcome
        {
            PrimaryEmotion = primaryEmotion,
            EmotionalScore = emotionalScore,
            Confidence = 0.6, // Would be enhanced with historical data
            PredictedImpact = balance > 0 ? "Positive" : balance < 0 ? "Negative" : "Neutral"
        };
    }

    private double CalculateRegretProbability(List<string> pros, List<string> cons, StakesLevel stakes)
    {
        var prosCount = pros.Count;
        var consCount = cons.Count;

        if (prosCount == 0 && consCount == 0) return 50;

        var balance = (double)(prosCount - consCount) / (prosCount + consCount);
        var baseRegret = 50 - (balance * 30); // Range: 20-80

        // Adjust based on stakes
        var stakesMultiplier = stakes switch
        {
            StakesLevel.Critical => 1.2,
            StakesLevel.High => 1.1,
            StakesLevel.Medium => 1.0,
            StakesLevel.Low => 0.9,
            _ => 1.0
        };

        return Math.Max(0, Math.Min(100, baseRegret * stakesMultiplier));
    }

    private string AssessRiskLevel(double regretProbability, StakesLevel stakes)
    {
        if (stakes == StakesLevel.Critical && regretProbability > 40)
            return "High";
        if (stakes == StakesLevel.High && regretProbability > 50)
            return "High";
        if (regretProbability > 70)
            return "High";
        if (regretProbability > 50)
            return "Medium";
        return "Low";
    }

    private async Task<double> CalculateValueAlignmentAsync(string userId, string optionText)
    {
        // Would integrate with IdentityProfile to calculate alignment with core values
        // For now, return a default calculation
        return 60; // Placeholder
    }

    private DecisionScenario ModelBestCaseScenario(Decision decision)
    {
        var bestOption = decision.Options
            .OrderByDescending(o => o.ValueAlignment)
            .FirstOrDefault();

        return new DecisionScenario
        {
            ScenarioType = "Best Case",
            OptionId = bestOption?.Id ?? 0,
            Description = $"Best case scenario: {bestOption?.OptionText}",
            EmotionalTrajectory = "Positive growth with high satisfaction",
            Probability = 0.3
        };
    }

    private DecisionScenario ModelWorstCaseScenario(Decision decision)
    {
        var worstOption = decision.Options
            .OrderBy(o => o.ValueAlignment)
            .FirstOrDefault();

        return new DecisionScenario
        {
            ScenarioType = "Worst Case",
            OptionId = worstOption?.Id ?? 0,
            Description = $"Worst case scenario: {worstOption?.OptionText}",
            EmotionalTrajectory = "Challenging period with potential stress",
            Probability = 0.2
        };
    }

    private DecisionScenario ModelMostLikelyScenario(Decision decision)
    {
        var avgOption = decision.Options
            .OrderByDescending(o => o.ValueAlignment)
            .Skip(decision.Options.Count / 2)
            .FirstOrDefault() ?? decision.Options.First();

        return new DecisionScenario
        {
            ScenarioType = "Most Likely",
            OptionId = avgOption.Id,
            Description = $"Most likely scenario: {avgOption.OptionText}",
            EmotionalTrajectory = "Mixed outcomes with moderate impact",
            Probability = 0.5
        };
    }

    private DecisionAnalysis BuildDecisionAnalysis(Decision decision, List<DecisionOption> options)
    {
        var emotionalFactors = options
            .SelectMany(o => JsonSerializer.Deserialize<List<string>>(o.EmotionalPros ?? "[]") ?? new())
            .Distinct()
            .ToList();

        var recommendations = new List<string>();
        var recommended = options.FirstOrDefault(o => o.IsRecommended);
        if (recommended != null)
        {
            recommendations.Add($"Recommended: {recommended.OptionText} (Value Alignment: {recommended.ValueAlignment:F0}%)");
        }

        var warnings = options
            .Where(o => o.RiskLevel == "High" || o.RegretProbability > 70)
            .Select(o => $"{o.OptionText}: High risk (Regret probability: {o.RegretProbability:F0}%)")
            .ToList();

        return new DecisionAnalysis
        {
            DecisionId = decision.Id,
            Options = options.Select(o => new DecisionOptionSummary
            {
                Id = o.Id,
                OptionText = o.OptionText,
                ValueAlignment = o.ValueAlignment,
                RegretProbability = o.RegretProbability,
                RiskLevel = o.RiskLevel,
                IsRecommended = o.IsRecommended
            }).ToList(),
            EmotionalFactors = emotionalFactors,
            Recommendations = recommendations,
            Warnings = warnings,
            NextSteps = new List<string> { "Consider short-term outcomes", "Evaluate long-term impact", "Reflect on personal values" }
        };
    }
}

// DTOs
public enum Timeframe
{
    Short = 0,  // 1-3 months
    Medium = 1, // 3-12 months
    Long = 2    // 1+ years
}

public class EmotionalOutcome
{
    public string PrimaryEmotion { get; set; } = string.Empty;
    public double EmotionalScore { get; set; } // 0-100
    public double Confidence { get; set; } // 0-1
    public string PredictedImpact { get; set; } = string.Empty;
}

public class EmotionalOutcomePrediction
{
    public Timeframe Timeframe { get; set; }
    public EmotionalOutcome Outcome { get; set; } = new();
}

public class DecisionAnalysis
{
    public int DecisionId { get; set; }
    public List<DecisionOptionSummary> Options { get; set; } = new();
    public List<string> EmotionalFactors { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> NextSteps { get; set; } = new();
}

public class DecisionOptionSummary
{
    public int Id { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public double ValueAlignment { get; set; }
    public double RegretProbability { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public bool IsRecommended { get; set; }
}

public class DecisionScenarioModel
{
    public int DecisionId { get; set; }
    public DecisionScenario BestCase { get; set; } = new();
    public DecisionScenario WorstCase { get; set; } = new();
    public DecisionScenario MostLikely { get; set; } = new();
}

public class DecisionScenario
{
    public string ScenarioType { get; set; } = string.Empty;
    public int OptionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string EmotionalTrajectory { get; set; } = string.Empty;
    public double Probability { get; set; }
}
