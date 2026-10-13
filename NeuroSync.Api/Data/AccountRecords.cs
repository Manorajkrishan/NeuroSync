namespace NeuroSync.Api.Data;

/// <summary>Consent flags. Memory and learning-data sharing default off.</summary>
public class UserPrivacyProfile
{
    public string UserId { get; set; } = "";
    public bool MemoryEnabled { get; set; }
    public bool EmotionHistoryEnabled { get; set; }
    public bool FaceAnalysisEnabled { get; set; }
    public bool IoTEnabled { get; set; }
    public bool ShareLearningData { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class UserPreferenceRecord
{
    public string UserId { get; set; } = "";
    public string? DisplayName { get; set; }
    public string Locale { get; set; } = "en";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Consent-gated personal notes. Separate from the 45-minute session cache.</summary>
public class PersistentMemoryItem
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ConversationHistoryEntry
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public string UserMessage { get; set; } = "";
    public string AssistantMessage { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class UserBaselineRecord
{
    public string UserId { get; set; } = "";
    public string PayloadJson { get; set; } = "{}";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Security audit row. Never stores message text, passwords, or tokens.</summary>
public class AuditEvent
{
    public long Id { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = "";
    public string? UserId { get; set; }
    public string? IpHash { get; set; }
    public int StatusCode { get; set; }
}
