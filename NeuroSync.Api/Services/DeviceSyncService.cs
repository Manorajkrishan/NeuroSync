using System.Collections.Concurrent;
using System.Text.Json;
using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Syncs owner identity + learning across phones, PCs, and any connected device.
/// </summary>
public class DeviceSyncService
{
    private readonly ConcurrentDictionary<string, ConnectedDevice> _devices = new();
    private readonly ConcurrentDictionary<string, PairingSession> _pairings = new();
    private readonly UserProfileService _profiles;
    private readonly ConversationMemory _memory;
    private readonly ILogger<DeviceSyncService> _logger;
    private readonly string _storagePath;

    public DeviceSyncService(
        UserProfileService profiles,
        ConversationMemory memory,
        IWebHostEnvironment env,
        ILogger<DeviceSyncService> logger)
    {
        _profiles = profiles;
        _memory = memory;
        _logger = logger;
        _storagePath = Path.Combine(env.ContentRootPath, "ConnectedDevices");
        Directory.CreateDirectory(_storagePath);
        LoadDevices();
    }

    public ConnectedDevice RegisterDevice(string userId, DeviceRegisterRequest req)
    {
        var deviceId = string.IsNullOrWhiteSpace(req.DeviceId)
            ? Guid.NewGuid().ToString("N")[..12]
            : req.DeviceId.Trim();

        var device = _devices.AddOrUpdate(deviceId, _ => new ConnectedDevice
        {
            DeviceId = deviceId,
            UserId = userId,
            DeviceName = req.DeviceName ?? InferName(req.Platform, req.UserAgent),
            Platform = req.Platform ?? InferPlatform(req.UserAgent),
            UserAgent = req.UserAgent,
            FirstSeenUtc = DateTime.UtcNow,
            LastSeenUtc = DateTime.UtcNow,
            InteractionCount = 1
        }, (_, existing) =>
        {
            existing.UserId = userId;
            existing.DeviceName = req.DeviceName ?? existing.DeviceName;
            existing.Platform = req.Platform ?? existing.Platform;
            existing.UserAgent = req.UserAgent ?? existing.UserAgent;
            existing.LastSeenUtc = DateTime.UtcNow;
            existing.InteractionCount++;
            return existing;
        });

        // Learn habits from this device
        LearnFromDevice(userId, device);

        SaveDevices();
        _logger.LogInformation("Device {DeviceId} ({Platform}) synced for owner {UserId}", deviceId, device.Platform, userId);
        return device;
    }

    public PairingCodeResponse CreatePairingCode(string userId)
    {
        // Clean expired
        foreach (var kv in _pairings.Where(p => p.Value.ExpiresUtc < DateTime.UtcNow).ToList())
            _pairings.TryRemove(kv.Key, out _);

        var code = Random.Shared.Next(100000, 999999).ToString();
        _pairings[code] = new PairingSession
        {
            Code = code,
            UserId = userId,
            ExpiresUtc = DateTime.UtcNow.AddMinutes(10)
        };

        return new PairingCodeResponse
        {
            PairingCode = code,
            UserId = userId,
            ExpiresAtUtc = _pairings[code].ExpiresUtc,
            Instructions = "Open NeuroSync on your other device → Sync → Enter this code"
        };
    }

    public DeviceSyncPayload? JoinWithPairingCode(string pairingCode, DeviceRegisterRequest deviceReq)
    {
        if (!_pairings.TryGetValue(pairingCode.Trim(), out var session) || session.ExpiresUtc < DateTime.UtcNow)
            return null;

        _pairings.TryRemove(pairingCode.Trim(), out _);
        var device = RegisterDevice(session.UserId, deviceReq);
        return GetSyncPayload(session.UserId, device.DeviceId);
    }

    public DeviceSyncPayload GetSyncPayload(string userId, string? currentDeviceId = null)
    {
        var profile = _profiles.GetOrCreateProfile(userId);
        var context = _memory.GetOrCreateContext(userId);
        var devices = _devices.Values.Where(d => d.UserId == userId).OrderByDescending(d => d.LastSeenUtc).ToList();
        var recent = _memory.GetRecentHistory(userId, 8);

        return new DeviceSyncPayload
        {
            UserId = userId,
            PreferredName = profile.PreferredName ?? profile.UserName,
            WhatIKnow = profile.GetWhatIKnow(),
            LearningStage = profile.LearningStage,
            InteractionCount = profile.InteractionCount,
            FavoriteActivities = profile.FavoriteActivities.Take(5).ToList(),
            ThingsThatHelp = profile.ThingsThatHelp.Take(5).ToList(),
            ThingsThatMakeHappy = profile.ThingsThatMakeHappy.Take(5).ToList(),
            Devices = devices,
            CurrentDeviceId = currentDeviceId,
            RecentEmotions = recent.Select(e => new SyncEmotionSnap
            {
                Emotion = e.DetectedEmotion?.Emotion.ToString(),
                Text = e.UserMessage,
                At = e.Timestamp
            }).ToList(),
            LastEmotion = context.LastEmotion?.ToString(),
            SyncedAtUtc = DateTime.UtcNow,
            Message = BuildWelcomeBack(profile, devices)
        };
    }

    public List<ConnectedDevice> ListDevices(string userId) =>
        _devices.Values.Where(d => d.UserId == userId).OrderByDescending(d => d.LastSeenUtc).ToList();

    public void Heartbeat(string userId, string deviceId)
    {
        if (_devices.TryGetValue(deviceId, out var d) && d.UserId == userId)
        {
            d.LastSeenUtc = DateTime.UtcNow;
            LearnFromDevice(userId, d);
            SaveDevices();
        }
    }

    private void LearnFromDevice(string userId, ConnectedDevice device)
    {
        var profile = _profiles.GetOrCreateProfile(userId);
        var hour = DateTime.Now.Hour;
        profile.ActiveHours = hour switch
        {
            < 12 => "morning",
            < 18 => "afternoon",
            < 22 => "evening",
            _ => "night"
        };
        profile.CustomAttributes["lastDevice"] = device.DeviceName;
        profile.CustomAttributes["lastPlatform"] = device.Platform;
        profile.CustomAttributes[$"device:{device.DeviceId}"] = new
        {
            device.Platform,
            device.DeviceName,
            device.LastSeenUtc,
            device.InteractionCount
        };
        profile.LastLearningUpdate = DateTime.UtcNow;
        _profiles.SaveProfilePublic(profile);
    }

    private static string BuildWelcomeBack(UserProfile profile, List<ConnectedDevice> devices)
    {
        var name = profile.PreferredName ?? "friend";
        var count = devices.Count;
        if (count <= 1)
            return $"Hey {name}. This device is connected — I'll keep learning with you here.";
        return $"Hey {name}. You're synced across {count} devices. I remember you from all of them.";
    }

    private static string InferPlatform(string? ua)
    {
        if (string.IsNullOrEmpty(ua)) return "unknown";
        ua = ua.ToLowerInvariant();
        if (ua.Contains("iphone") || ua.Contains("ipad")) return "ios";
        if (ua.Contains("android")) return "android";
        if (ua.Contains("windows")) return "windows";
        if (ua.Contains("mac")) return "mac";
        if (ua.Contains("linux")) return "linux";
        return "web";
    }

    private static string InferName(string? platform, string? ua)
    {
        var p = platform ?? InferPlatform(ua);
        return p switch
        {
            "ios" => "iPhone",
            "android" => "Android phone",
            "windows" => "Windows PC",
            "mac" => "Mac",
            _ => "Web browser"
        };
    }

    private void SaveDevices()
    {
        try
        {
            var path = Path.Combine(_storagePath, "devices.json");
            var json = JsonSerializer.Serialize(_devices.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not save connected devices");
        }
    }

    private void LoadDevices()
    {
        try
        {
            var path = Path.Combine(_storagePath, "devices.json");
            if (!File.Exists(path)) return;
            var list = JsonSerializer.Deserialize<List<ConnectedDevice>>(File.ReadAllText(path));
            if (list == null) return;
            foreach (var d in list)
                _devices[d.DeviceId] = d;
            _logger.LogInformation("Loaded {Count} connected devices", _devices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load connected devices");
        }
    }
}

public class ConnectedDevice
{
    public string DeviceId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = "Device";
    public string Platform { get; set; } = "web";
    public string? UserAgent { get; set; }
    public DateTime FirstSeenUtc { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public int InteractionCount { get; set; }
}

public class PairingSession
{
    public string Code { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime ExpiresUtc { get; set; }
}

public class DeviceRegisterRequest
{
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? Platform { get; set; }
    public string? UserAgent { get; set; }
}

public class PairingCodeResponse
{
    public string PairingCode { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string Instructions { get; set; } = string.Empty;
}

public class DeviceSyncPayload
{
    public string UserId { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public string? WhatIKnow { get; set; }
    public int LearningStage { get; set; }
    public int InteractionCount { get; set; }
    public List<string> FavoriteActivities { get; set; } = new();
    public List<string> ThingsThatHelp { get; set; } = new();
    public List<string> ThingsThatMakeHappy { get; set; } = new();
    public List<ConnectedDevice> Devices { get; set; } = new();
    public string? CurrentDeviceId { get; set; }
    public List<SyncEmotionSnap> RecentEmotions { get; set; } = new();
    public string? LastEmotion { get; set; }
    public DateTime SyncedAtUtc { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SyncEmotionSnap
{
    public string? Emotion { get; set; }
    public string? Text { get; set; }
    public DateTime At { get; set; }
}
