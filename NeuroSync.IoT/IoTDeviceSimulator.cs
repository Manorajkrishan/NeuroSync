using NeuroSync.Core;

namespace NeuroSync.IoT;

/// <summary>
/// Simulates IoT device responses based on emotional states.
/// Can be extended to control real devices.
/// </summary>
public class IoTDeviceSimulator
{
    private readonly Dictionary<string, DeviceState> _devices;
    private readonly RealDeviceController? _realDeviceController;

    public IoTDeviceSimulator(RealDeviceController? realDeviceController = null)
    {
        _devices = new Dictionary<string, DeviceState>
        {
            { "light-1", new DeviceState { DeviceId = "light-1", DeviceType = "light", IsActive = false } },
            { "light-2", new DeviceState { DeviceId = "light-2", DeviceType = "light", IsActive = false } },
            { "speaker", new DeviceState { DeviceId = "speaker", DeviceType = "speaker", IsActive = false } },
            { "notification", new DeviceState { DeviceId = "notification", DeviceType = "notification", IsActive = false } }
        };
        _realDeviceController = realDeviceController;
    }

    /// <summary>
    /// Processes an emotion and returns IoT actions.
    /// </summary>
    public List<IoTAction> ProcessEmotion(EmotionType emotion)
    {
        var actions = new List<IoTAction>();

        switch (emotion)
        {
            case EmotionType.Happy:
                actions.Add(new IoTAction
                {
                    DeviceId = "light-1",
                    ActionType = "setColor",
                    Parameters = new Dictionary<string, object> { { "color", "warm_yellow" }, { "brightness", 80 } },
                    TriggeredByEmotion = emotion
                });
                actions.Add(new IoTAction
                {
                    DeviceId = "speaker",
                    ActionType = "playMusic",
                    Parameters = new Dictionary<string, object> 
                    { 
                        { "genre", "upbeat" }, 
                        { "playlist", "Happy Vibes" },
                        { "volume", 60 },
                        { "suggestion", "Playing upbeat music to enhance your happiness!" }
                    },
                    TriggeredByEmotion = emotion
                });
                _devices["light-1"].IsActive = true;
                _devices["light-1"].CurrentColor = "warm_yellow";
                _devices["speaker"].IsActive = true;
                break;

            case EmotionType.Sad:
                actions.Add(new IoTAction
                {
                    DeviceId = "light-1",
                    ActionType = "setColor",
                    Parameters = new Dictionary<string, object> { { "color", "soft_blue" }, { "brightness", 40 } },
                    TriggeredByEmotion = emotion
                });
                actions.Add(new IoTAction
                {
                    DeviceId = "speaker",
                    ActionType = "playMusic",
                    Parameters = new Dictionary<string, object> 
                    { 
                        { "genre", "calming" }, 
                        { "playlist", "Comfort & Healing" },
                        { "volume", 45 },
                        { "suggestion", "Playing calming music to help soothe your feelings" }
                    },
                    TriggeredByEmotion = emotion
                });
                actions.Add(new IoTAction
                {
                    DeviceId = "notification",
                    ActionType = "showMessage",
                    Parameters = new Dictionary<string, object> { { "message", "Remember, tough times don't last. You've got this!" } },
                    TriggeredByEmotion = emotion
                });
                _devices["light-1"].IsActive = true;
                _devices["light-1"].CurrentColor = "soft_blue";
                _devices["speaker"].IsActive = true;
                break;

            case EmotionType.Angry:
                actions.Add(new IoTAction
                {
                    DeviceId = "light-1",
                    ActionType = "setColor",
                    Parameters = new Dictionary<string, object> { { "color", "cool_white" }, { "brightness", 30 } },
                    TriggeredByEmotion = emotion
                });
                actions.Add(new IoTAction
                {
                    DeviceId = "speaker",
                    ActionType = "playMusic",
                    Parameters = new Dictionary<string, object> 
                    { 
                        { "genre", "meditation" }, 
                        { "playlist", "Peaceful Sounds" },
                        { "volume", 40 },
                        { "suggestion", "Playing meditation music to help you calm down" }
                    },
                    TriggeredByEmotion = emotion
                });
                _devices["light-1"].IsActive = true;
                _devices["light-1"].CurrentColor = "cool_white";
                _devices["speaker"].IsActive = true;
                break;

            case EmotionType.Anxious:
                actions.Add(new IoTAction
                {
                    DeviceId = "light-1",
                    ActionType = "setColor",
                    Parameters = new Dictionary<string, object> { { "color", "soft_purple" }, { "brightness", 50 } },
                    TriggeredByEmotion = emotion
                });
                actions.Add(new IoTAction
                {
                    DeviceId = "light-2",
                    ActionType = "breathing",
                    Parameters = new Dictionary<string, object> { { "speed", "slow" } },
                    TriggeredByEmotion = emotion
                });
                actions.Add(new IoTAction
                {
                    DeviceId = "speaker",
                    ActionType = "playMusic",
                    Parameters = new Dictionary<string, object> 
                    { 
                        { "genre", "ambient" }, 
                        { "playlist", "Nature Sounds & White Noise" },
                        { "volume", 35 },
                        { "suggestion", "Playing ambient sounds to help reduce anxiety" }
                    },
                    TriggeredByEmotion = emotion
                });
                _devices["light-1"].IsActive = true;
                _devices["light-1"].CurrentColor = "soft_purple";
                _devices["light-2"].IsActive = true;
                _devices["speaker"].IsActive = true;
                break;

            case EmotionType.Calm:
                actions.Add(new IoTAction
                {
                    DeviceId = "light-1",
                    ActionType = "setColor",
                    Parameters = new Dictionary<string, object> { { "color", "warm_white" }, { "brightness", 60 } },
                    TriggeredByEmotion = emotion
                });
                actions.Add(new IoTAction
                {
                    DeviceId = "speaker",
                    ActionType = "playMusic",
                    Parameters = new Dictionary<string, object> 
                    { 
                        { "genre", "classical" }, 
                        { "playlist", "Peaceful Classical" },
                        { "volume", 50 },
                        { "suggestion", "Playing peaceful music to maintain your calm state" }
                    },
                    TriggeredByEmotion = emotion
                });
                _devices["light-1"].IsActive = true;
                _devices["light-1"].CurrentColor = "warm_white";
                _devices["speaker"].IsActive = true;
                break;

            case EmotionType.Excited:
                actions.Add(new IoTAction
                {
                    DeviceId = "light-1",
                    ActionType = "setColor",
                    Parameters = new Dictionary<string, object> { { "color", "bright_white" }, { "brightness", 90 } },
                    TriggeredByEmotion = emotion
                });
                actions.Add(new IoTAction
                {
                    DeviceId = "light-2",
                    ActionType = "pulse",
                    Parameters = new Dictionary<string, object> { { "speed", "fast" } },
                    TriggeredByEmotion = emotion
                });
                actions.Add(new IoTAction
                {
                    DeviceId = "speaker",
                    ActionType = "playMusic",
                    Parameters = new Dictionary<string, object> 
                    { 
                        { "genre", "energetic" }, 
                        { "playlist", "Party & Celebration" },
                        { "volume", 75 },
                        { "suggestion", "Playing energetic music to match your excitement!" }
                    },
                    TriggeredByEmotion = emotion
                });
                _devices["light-1"].IsActive = true;
                _devices["light-1"].CurrentColor = "bright_white";
                _devices["light-2"].IsActive = true;
                _devices["speaker"].IsActive = true;
                break;

            case EmotionType.Frustrated:
                actions.Add(new IoTAction
                {
                    DeviceId = "light-1",
                    ActionType = "setColor",
                    Parameters = new Dictionary<string, object> { { "color", "orange" }, { "brightness", 70 } },
                    TriggeredByEmotion = emotion
                });
                actions.Add(new IoTAction
                {
                    DeviceId = "speaker",
                    ActionType = "playMusic",
                    Parameters = new Dictionary<string, object> 
                    { 
                        { "genre", "instrumental" }, 
                        { "playlist", "Focus & Productivity" },
                        { "volume", 50 },
                        { "suggestion", "Playing instrumental music to help you focus and reset" }
                    },
                    TriggeredByEmotion = emotion
                });
                _devices["light-1"].IsActive = true;
                _devices["light-1"].CurrentColor = "orange";
                _devices["speaker"].IsActive = true;
                break;

            case EmotionType.Neutral:
            default:
                actions.Add(new IoTAction
                {
                    DeviceId = "light-1",
                    ActionType = "setColor",
                    Parameters = new Dictionary<string, object> { { "color", "neutral_white" }, { "brightness", 60 } },
                    TriggeredByEmotion = emotion
                });
                _devices["light-1"].IsActive = true;
                _devices["light-1"].CurrentColor = "neutral_white";
                break;
        }

        return actions;
    }

    /// <summary>
    /// Gets the current state of all devices.
    /// </summary>
    public Dictionary<string, DeviceState> GetDeviceStates()
    {
        return new Dictionary<string, DeviceState>(_devices);
    }
}

/// <summary>
/// Represents the state of an IoT device.
/// </summary>
public class DeviceState
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? CurrentColor { get; set; }
}

