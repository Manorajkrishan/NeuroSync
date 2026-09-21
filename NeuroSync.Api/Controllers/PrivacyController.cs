using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;
using NeuroSync.Core;

namespace NeuroSync.Api.Controllers;

/// <summary>
/// User-controlled memory, consent, baseline, export/delete — privacy-first V1 APIs.
/// </summary>
[ApiController]
[Route("api/privacy")]
public class PrivacyController : ControllerBase
{
    private readonly ConversationMemory _memory;
    private readonly UserProfileService _profiles;
    private readonly EmotionalBaselineService _baseline;
    private readonly EthicalAIFrameworkService _consent;
    private readonly ILogger<PrivacyController> _logger;

    public PrivacyController(
        ConversationMemory memory,
        UserProfileService profiles,
        EmotionalBaselineService baseline,
        EthicalAIFrameworkService consent,
        ILogger<PrivacyController> logger)
    {
        _memory = memory;
        _profiles = profiles;
        _baseline = baseline;
        _consent = consent;
        _logger = logger;
    }

    [HttpGet("consent/{userId}")]
    public IActionResult GetConsent(string userId)
    {
        if (!UserIdSanitizer.TryNormalize(userId, out var safe))
            return BadRequest(new { error = "Invalid userId" });
        return Ok(_consent.GetOrCreateDefault(safe));
    }

    [HttpPut("consent/{userId}")]
    public IActionResult SetConsent(string userId, [FromBody] EthicalAIConsent body)
    {
        if (!UserIdSanitizer.TryNormalize(userId, out var safe))
            return BadRequest(new { error = "Invalid userId" });
        _consent.SetConsent(safe, body);
        return Ok(_consent.GetOrCreateDefault(safe));
    }

    [HttpGet("memory/{userId}")]
    public IActionResult GetMemory(string userId, [FromQuery] int recent = 20)
    {
        if (!_consent.HasConsent(userId, ConsentType.Memory) && !_consent.HasConsent(userId, ConsentType.EmotionHistory))
        {
            return Ok(new
            {
                disclaimer = "MemoryConsent / EmotionHistoryConsent are OFF. Enable them to store and view history.",
                memoryEnabled = false,
                consent = _consent.GetOrCreateDefault(userId)
            });
        }

        var profile = _profiles.GetOrCreateProfile(userId);
        var history = _memory.GetRecentHistory(userId, Math.Clamp(recent, 1, 100));
        return Ok(new
        {
            disclaimer = "You control this memory. Export or delete anytime.",
            memoryEnabled = true,
            whatIKnow = profile.GetWhatIKnow(),
            preferredName = profile.PreferredName ?? profile.UserName,
            favoriteActivities = profile.FavoriteActivities,
            whatHelpsWhen = profile.WhatHelpsWhen,
            recentConversation = history.Select(h => new
            {
                h.Timestamp,
                h.UserMessage,
                emotion = h.DetectedEmotion?.Emotion.ToString(),
                uncertainty = h.DetectedEmotion?.Uncertainty.ToString(),
                response = h.Response?.Message
            }),
            timeline = _memory.GetEmotionalTimeline(userId),
            baseline = _baseline.GetSnapshot(userId)
        });
    }

    [HttpGet("export/{userId}")]
    public IActionResult Export(string userId)
    {
        var profile = _profiles.GetOrCreateProfile(userId);
        var history = _memory.GetRecentHistory(userId, 100);
        var payload = new
        {
            exportedAt = DateTime.UtcNow,
            disclaimer = "Personal export. Handle securely. Not a medical record.",
            consent = _consent.GetOrCreateDefault(userId),
            profile = new
            {
                profile.UserId,
                profile.PreferredName,
                profile.UserName,
                profile.FavoriteActivities,
                profile.ThingsThatHelp,
                profile.WhatHelpsWhen,
                profile.EmotionalPatterns
            },
            conversations = history.Select(h => new
            {
                h.Timestamp,
                h.UserMessage,
                emotion = h.DetectedEmotion?.Emotion.ToString(),
                uncertainty = h.DetectedEmotion?.Uncertainty.ToString(),
                response = h.Response?.Message
            }),
            baseline = _baseline.GetSnapshot(userId),
            timeline = _memory.GetEmotionalTimeline(userId, 90)
        };
        return Ok(payload);
    }

    [HttpGet("baseline/{userId}")]
    public IActionResult GetBaseline(string userId) => Ok(_baseline.GetSnapshot(userId));

    [HttpPost("baseline/{userId}/reset")]
    public IActionResult ResetBaseline(string userId)
    {
        _baseline.ResetBaselineHistory(userId);
        return Ok(new { reset = true, baseline = _baseline.GetSnapshot(userId) });
    }

    [HttpGet("timeline/{userId}")]
    public IActionResult GetTimeline(string userId, [FromQuery] int days = 14) =>
        Ok(new
        {
            disclaimer = "Privacy-controlled emotional timeline. Estimates only — not a clinical record.",
            days,
            points = _memory.GetEmotionalTimeline(userId, days)
        });

    /// <summary>Delete NeuroSync history permanently for this user id.</summary>
    [HttpDelete("memory/{userId}")]
    public IActionResult DeleteMemory(string userId)
    {
        _memory.ClearUserData(userId);
        _profiles.DeleteProfile(userId);
        _consent.RevokeAllSensitive(userId);
        _logger.LogInformation("Permanent delete requested (userId length={Len})", userId.Length);
        return Ok(new
        {
            deleted = true,
            message = "Conversation memory, profile, and sensitive consents cleared permanently for this user id."
        });
    }
}
