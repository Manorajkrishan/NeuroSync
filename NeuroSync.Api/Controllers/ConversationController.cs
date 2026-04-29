using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;
using NeuroSync.Core;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConversationController : ControllerBase
{
    private readonly ConversationMemory _conversationMemory;
    private readonly ILogger<ConversationController> _logger;

    public ConversationController(ConversationMemory conversationMemory, ILogger<ConversationController> logger)
    {
        _conversationMemory = conversationMemory;
        _logger = logger;
    }

    /// <summary>
    /// Gets recent conversation history for a user.
    /// </summary>
    /// <param name="userId">User ID (default: "default")</param>
    /// <param name="limit">Max entries to return (default: 20, max: 50)</param>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ConversationHistoryResponse), 200)]
    public IActionResult GetHistory([FromQuery] string? userId = "default", [FromQuery] int limit = 20)
    {
        userId ??= "default";
        limit = Math.Clamp(limit, 1, 50);

        var entries = _conversationMemory.GetRecentHistory(userId, limit);
        var items = entries.Select(e => new ConversationHistoryItem
        {
            UserMessage = e.UserMessage,
            Emotion = e.DetectedEmotion != null ? e.DetectedEmotion.Emotion.ToString() : null,
            Confidence = e.DetectedEmotion?.Confidence,
            ResponseMessage = e.Response?.Message,
            Timestamp = e.Timestamp,
            FollowUpQuestion = e.FollowUpQuestion
        }).ToList();

        return Ok(new ConversationHistoryResponse
        {
            UserId = userId,
            Count = items.Count,
            Entries = items
        });
    }
}

public class ConversationHistoryResponse
{
    public string UserId { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<ConversationHistoryItem> Entries { get; set; } = new();
}

public class ConversationHistoryItem
{
    public string UserMessage { get; set; } = string.Empty;
    public string? Emotion { get; set; }
    public float? Confidence { get; set; }
    public string? ResponseMessage { get; set; }
    public DateTime Timestamp { get; set; }
    public string? FollowUpQuestion { get; set; }
}
