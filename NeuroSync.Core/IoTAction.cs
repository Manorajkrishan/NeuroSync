namespace NeuroSync.Core;

/// <summary>
/// Represents an IoT device action.
/// </summary>
public class IoTAction
{
    public string DeviceId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public EmotionType TriggeredByEmotion { get; set; }
}


