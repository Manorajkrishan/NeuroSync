using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;

namespace NeuroSync.Api.Controllers;

/// <summary>
/// User-controlled memory, baseline, and timeline — privacy-first companion APIs.
/// </summary>
[ApiController]
[Route("api/privacy")]
public class PrivacyController : ControllerBase
{
    private readonly ConversationMemory _memory;
    private readonly UserProfileService _profiles;
    private readonly EmotionalBaselineService _baseline;
    private readonly ILogger<PrivacyController> _logger;

    public PrivacyController(
        ConversationMemory memory,
        UserProfileService profiles,
        EmotionalBaselineService baseline,
        ILogger<PrivacyController> logger)
    {
        _memory = memory;
        _profiles = profiles;
        _baseline = baseline;
        _logger = logger;
    }

    [HttpGet("memory/{userId}")]
    public IActionResult GetMemory(string userId, [FromQuery] int recent = 20)
    {
        var profile = _profiles.GetOrCreateProfile(userId);
        var history = _memory.GetRecentHistory(userId, Math.Clamp(recent, 1, 100));
        return Ok(new
        {
            disclaimer = "You control this memory. You can correct or delete it anytime.",
            whatIKnow = profile.GetWhatIKnow(),
            preferredName = profile.PreferredName ?? profile.UserName,
            favoriteActivities = profile.FavoriteActivities,
            whatHelpsWhen = profile.WhatHelpsWhen,
            recentConversation = history.Select(h => new
            {
                h.Timestamp,
                h.UserMessage,
                emotion = h.DetectedEmotion?.Emotion.ToString(),
                response = h.Response?.Message
            }),
            timeline = _memory.GetEmotionalTimeline(userId),
            baseline = _baseline.GetSnapshot(userId)
        });
    }

    [HttpGet("baseline/{userId}")]
    public IActionResult GetBaseline(string userId) => Ok(_baseline.GetSnapshot(userId));

    [HttpGet("timeline/{userId}")]
    public IActionResult GetTimeline(string userId, [FromQuery] int days = 14) =>
        Ok(new
        {
            disclaimer = "Privacy-controlled emotional timeline. Estimates only — not a clinical record.",
            days,
            points = _memory.GetEmotionalTimeline(userId, days)
        });

    [HttpDelete("memory/{userId}")]
    public IActionResult DeleteMemory(string userId)
    {
        _memory.ClearUserData(userId);
        _profiles.DeleteProfile(userId);
        _logger.LogInformation("User {UserId} wiped companion memory + profile", userId);
        return Ok(new
        {
            deleted = true,
            message = "Conversation memory and profile cleared for this user id."
        });
    }
}
