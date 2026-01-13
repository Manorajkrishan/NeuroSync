using NeuroSync.Core;
using Microsoft.Extensions.Logging;

namespace NeuroSync.Api.Services;

/// <summary>
/// Planning and Coaching Service - Expanded Action Intelligence
/// Provides: planning, goal tracking, reminders, coaching, structured guidance
/// </summary>
public class PlanningAndCoachingService
{
    private readonly ILogger<PlanningAndCoachingService> _logger;
    private readonly ConversationMemory? _conversationMemory;
    private readonly UserProfileService? _userProfileService;
    private readonly string _storagePath;
    private readonly Dictionary<string, List<UserGoal>> _userGoals = new();
    private readonly Dictionary<string, List<Reminder>> _userReminders = new();

    public PlanningAndCoachingService(
        ILogger<PlanningAndCoachingService> logger,
        IWebHostEnvironment environment,
        ConversationMemory? conversationMemory = null,
        UserProfileService? userProfileService = null)
    {
        _logger = logger;
        _conversationMemory = conversationMemory;
        _userProfileService = userProfileService;
        _storagePath = Path.Combine(environment.ContentRootPath, "UserPlans");

        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }

        LoadPlans();
    }

    /// <summary>
    /// Create or update a goal for the user
    /// </summary>
    public UserGoal CreateGoal(string userId, string goalTitle, string? description = null, DateTime? targetDate = null)
    {
        var goal = new UserGoal
        {
            GoalId = Guid.NewGuid().ToString(),
            UserId = userId,
            Title = goalTitle,
            Description = description ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            TargetDate = targetDate,
            Status = GoalStatus.Active,
            Progress = 0.0f
        };

        if (!_userGoals.ContainsKey(userId))
        {
            _userGoals[userId] = new List<UserGoal>();
        }

        _userGoals[userId].Add(goal);
        SaveGoals(userId);

        _logger.LogInformation("Created goal for user {UserId}: {GoalTitle}", userId, goalTitle);
        return goal;
    }

    /// <summary>
    /// Update goal progress
    /// </summary>
    public UserGoal? UpdateGoalProgress(string userId, string goalId, float progress)
    {
        if (!_userGoals.TryGetValue(userId, out var goals))
        {
            return null;
        }

        var goal = goals.FirstOrDefault(g => g.GoalId == goalId);
        if (goal == null)
        {
            return null;
        }

        goal.Progress = Math.Clamp(progress, 0.0f, 100.0f);
        goal.UpdatedAt = DateTime.UtcNow;

        if (goal.Progress >= 100.0f)
        {
            goal.Status = GoalStatus.Completed;
            goal.CompletedAt = DateTime.UtcNow;
        }

        SaveGoals(userId);
        _logger.LogInformation("Updated goal progress for user {UserId}, goal {GoalId}: {Progress}%", userId, goalId, progress);
        
        return goal;
    }

    /// <summary>
    /// Get all active goals for a user
    /// </summary>
    public List<UserGoal> GetActiveGoals(string userId)
    {
        if (!_userGoals.TryGetValue(userId, out var goals))
        {
            return new List<UserGoal>();
        }

        return goals.Where(g => g.Status == GoalStatus.Active).ToList();
    }

    /// <summary>
    /// Create a reminder
    /// </summary>
    public Reminder CreateReminder(string userId, string reminderText, DateTime reminderTime, string? category = null)
    {
        var reminder = new Reminder
        {
            ReminderId = Guid.NewGuid().ToString(),
            UserId = userId,
            Text = reminderText,
            ReminderTime = reminderTime,
            CreatedAt = DateTime.UtcNow,
            Status = ReminderStatus.Pending,
            Category = category ?? "general"
        };

        if (!_userReminders.ContainsKey(userId))
        {
            _userReminders[userId] = new List<Reminder>();
        }

        _userReminders[userId].Add(reminder);
        SaveReminders(userId);

        _logger.LogInformation("Created reminder for user {UserId}: {ReminderText} at {ReminderTime}", userId, reminderText, reminderTime);
        return reminder;
    }

    /// <summary>
    /// Get upcoming reminders for a user
    /// </summary>
    public List<Reminder> GetUpcomingReminders(string userId, TimeSpan? timeWindow = null)
    {
        if (!_userReminders.TryGetValue(userId, out var reminders))
        {
            return new List<Reminder>();
        }

        var window = timeWindow ?? TimeSpan.FromHours(24);
        var now = DateTime.UtcNow;
        var futureTime = now.Add(window);

        return reminders
            .Where(r => r.Status == ReminderStatus.Pending && 
                       r.ReminderTime >= now && 
                       r.ReminderTime <= futureTime)
            .OrderBy(r => r.ReminderTime)
            .ToList();
    }

    /// <summary>
    /// Provide coaching guidance based on user's current state
    /// </summary>
    public CoachingGuidance ProvideCoaching(
        EmotionType emotion,
        string? userId = null,
        ConversationContext? context = null)
    {
        var guidance = new CoachingGuidance
        {
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            FocusArea = DetermineFocusArea(emotion, context),
            SuggestedActions = GetSuggestedActions(emotion, context, userId),
            MotivationalMessage = GetMotivationalMessage(emotion, context),
            NextSteps = GetNextSteps(emotion, context, userId)
        };

        // Add goal-related coaching if user has active goals
        if (!string.IsNullOrEmpty(userId))
        {
            var activeGoals = GetActiveGoals(userId);
            if (activeGoals.Any())
            {
                guidance.GoalUpdates = GetGoalUpdates(activeGoals, emotion);
            }
        }

        _logger.LogInformation("Provided coaching guidance for user {UserId}, focus: {FocusArea}", userId, guidance.FocusArea);
        return guidance;
    }

    /// <summary>
    /// Create a structured plan for achieving a goal
    /// </summary>
    public StructuredPlan CreatePlan(string userId, string goalTitle, List<string> steps, DateTime? targetDate = null)
    {
        var goal = CreateGoal(userId, goalTitle, targetDate: targetDate);
        
        var plan = new StructuredPlan
        {
            PlanId = Guid.NewGuid().ToString(),
            GoalId = goal.GoalId,
            UserId = userId,
            Steps = steps.Select((step, index) => new PlanStep
            {
                StepId = Guid.NewGuid().ToString(),
                Order = index + 1,
                Description = step,
                Status = StepStatus.Pending,
                CreatedAt = DateTime.UtcNow
            }).ToList(),
            CreatedAt = DateTime.UtcNow,
            Status = PlanStatus.Active
        };

        SavePlan(plan);
        _logger.LogInformation("Created structured plan for user {UserId}: {GoalTitle} with {StepCount} steps", userId, goalTitle, steps.Count);
        
        return plan;
    }

    // Helper methods
    private string DetermineFocusArea(EmotionType emotion, ConversationContext? context)
    {
        return emotion switch
        {
            EmotionType.Sad => "Emotional well-being and self-care",
            EmotionType.Anxious => "Stress management and mindfulness",
            EmotionType.Angry => "Emotional regulation and communication",
            EmotionType.Frustrated => "Problem-solving and resilience",
            EmotionType.Excited => "Channeling energy productively",
            EmotionType.Happy => "Maintaining positive momentum",
            _ => "Overall personal growth"
        };
    }

    private List<string> GetSuggestedActions(EmotionType emotion, ConversationContext? context, string? userId)
    {
        var actions = new List<string>();

        switch (emotion)
        {
            case EmotionType.Sad:
                actions.AddRange(new[]
                {
                    "Take time for self-care activities",
                    "Reach out to a trusted friend or family member",
                    "Engage in activities that bring you joy",
                    "Practice self-compassion and kindness"
                });
                break;

            case EmotionType.Anxious:
                actions.AddRange(new[]
                {
                    "Practice deep breathing exercises",
                    "Break tasks into smaller, manageable steps",
                    "Focus on what you can control",
                    "Try mindfulness or meditation"
                });
                break;

            case EmotionType.Frustrated:
                actions.AddRange(new[]
                {
                    "Take a short break to reset",
                    "Break the problem into smaller parts",
                    "Ask for help or different perspective",
                    "Focus on solutions, not problems"
                });
                break;

            case EmotionType.Excited:
                actions.AddRange(new[]
                {
                    "Channel energy into goal-directed activities",
                    "Set new challenging goals",
                    "Share your excitement with others",
                    "Document your positive experiences"
                });
                break;
        }

        return actions;
    }

    private string GetMotivationalMessage(EmotionType emotion, ConversationContext? context)
    {
        return emotion switch
        {
            EmotionType.Sad => "Remember, difficult moments pass. You're stronger than you know.",
            EmotionType.Anxious => "Take it one step at a time. You've handled challenges before.",
            EmotionType.Frustrated => "Challenges are opportunities for growth. You can overcome this.",
            EmotionType.Excited => "This positive energy is powerful! Let's make the most of it.",
            _ => "You're making progress. Keep going!"
        };
    }

    private List<string> GetNextSteps(EmotionType emotion, ConversationContext? context, string? userId)
    {
        var steps = new List<string>();

        if (!string.IsNullOrEmpty(userId))
        {
            var activeGoals = GetActiveGoals(userId);
            if (activeGoals.Any())
            {
                var nextGoal = activeGoals.OrderBy(g => g.TargetDate ?? DateTime.MaxValue).First();
                steps.Add($"Continue working toward: {nextGoal.Title}");
                steps.Add($"Current progress: {nextGoal.Progress:F0}%");
            }
        }

        steps.Add("Check in with yourself regularly");
        steps.Add("Celebrate small wins along the way");

        return steps;
    }

    private List<GoalUpdate> GetGoalUpdates(List<UserGoal> goals, EmotionType emotion)
    {
        return goals.Select(goal => new GoalUpdate
        {
            GoalId = goal.GoalId,
            Title = goal.Title,
            Progress = goal.Progress,
            Message = emotion == EmotionType.Excited 
                ? $"Great momentum! Keep working on {goal.Title}" 
                : $"You're making progress on {goal.Title}. Keep it up!"
        }).ToList();
    }

    // Storage methods
    private void SaveGoals(string userId)
    {
        try
        {
            if (!_userGoals.TryGetValue(userId, out var goals))
                return;

            var filePath = Path.Combine(_storagePath, $"{userId}_goals.json");
            var json = System.Text.Json.JsonSerializer.Serialize(goals, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving goals for user {UserId}", userId);
        }
    }

    private void SaveReminders(string userId)
    {
        try
        {
            if (!_userReminders.TryGetValue(userId, out var reminders))
                return;

            var filePath = Path.Combine(_storagePath, $"{userId}_reminders.json");
            var json = System.Text.Json.JsonSerializer.Serialize(reminders, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving reminders for user {UserId}", userId);
        }
    }

    private void SavePlan(StructuredPlan plan)
    {
        try
        {
            var filePath = Path.Combine(_storagePath, $"{plan.UserId}_plan_{plan.PlanId}.json");
            var json = System.Text.Json.JsonSerializer.Serialize(plan, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving plan {PlanId} for user {UserId}", plan.PlanId, plan.UserId);
        }
    }

    private void LoadPlans()
    {
        try
        {
            if (!Directory.Exists(_storagePath))
                return;

            // Load goals
            foreach (var file in Directory.GetFiles(_storagePath, "*_goals.json"))
            {
                var json = File.ReadAllText(file);
                var goals = System.Text.Json.JsonSerializer.Deserialize<List<UserGoal>>(json);
                if (goals != null && goals.Count > 0)
                {
                    var userId = goals[0].UserId;
                    _userGoals[userId] = goals;
                }
            }

            // Load reminders
            foreach (var file in Directory.GetFiles(_storagePath, "*_reminders.json"))
            {
                var json = File.ReadAllText(file);
                var reminders = System.Text.Json.JsonSerializer.Deserialize<List<Reminder>>(json);
                if (reminders != null && reminders.Count > 0)
                {
                    var userId = reminders[0].UserId;
                    _userReminders[userId] = reminders;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading plans");
        }
    }
}

// Data models
public class UserGoal
{
    public string GoalId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? TargetDate { get; set; }
    public GoalStatus Status { get; set; }
    public float Progress { get; set; }
}

public enum GoalStatus
{
    Active,
    Paused,
    Completed,
    Cancelled
}

public class Reminder
{
    public string ReminderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime ReminderTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ReminderStatus Status { get; set; }
    public string Category { get; set; } = "general";
}

public enum ReminderStatus
{
    Pending,
    Completed,
    Cancelled
}

public class CoachingGuidance
{
    public string? UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string FocusArea { get; set; } = string.Empty;
    public List<string> SuggestedActions { get; set; } = new();
    public string MotivationalMessage { get; set; } = string.Empty;
    public List<string> NextSteps { get; set; } = new();
    public List<GoalUpdate> GoalUpdates { get; set; } = new();
}

public class GoalUpdate
{
    public string GoalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public float Progress { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class StructuredPlan
{
    public string PlanId { get; set; } = string.Empty;
    public string GoalId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public List<PlanStep> Steps { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public PlanStatus Status { get; set; }
}

public class PlanStep
{
    public string StepId { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public StepStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum StepStatus
{
    Pending,
    InProgress,
    Completed,
    Skipped
}

public enum PlanStatus
{
    Active,
    Completed,
    Paused,
    Cancelled
}
