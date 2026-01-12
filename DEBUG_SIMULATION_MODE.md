# Debug: Why Still in Simulation Mode?

## Current Status

✅ **Emotion Detection:** Working perfectly!
- "i feel sad" → **Sad** (98.4% confidence) ✅

❌ **IoT Actions:** Still showing "SIMULATION MODE"
- No button appearing
- Services not executing

## Possible Causes

### 1. RealIoTController Not Available

**Check:** Is `RealIoTController` being injected into `DecisionEngine`?

**Location:** `NeuroSync.Api/Program.cs` line ~137-143

**Expected:**
```csharp
builder.Services.AddScoped<DecisionEngine>(sp =>
{
    var iotSimulator = sp.GetRequiredService<IoTDeviceSimulator>();
    var realIoTController = sp.GetService<RealIoTController>(); // Should not be null
    var logger = sp.GetRequiredService<ILogger<DecisionEngine>>();
    return new DecisionEngine(iotSimulator, realIoTController, logger);
});
```

**If `realIoTController` is null:**
- Services not registered properly
- Check if YouTube Music service is registered (needs access token)

### 2. YouTube Music Service Not Available

**Check:** Is the access token configured and valid?

**Location:** `NeuroSync.Api/appsettings.json`

**Required:**
```json
"YouTubeMusic": {
    "AccessToken": "ya29...", // Must not be empty
    "RefreshToken": "1//0...",
    "ClientId": "...",
    "ClientSecret": "..."
}
```

**If access token is missing/empty:**
- Service won't be registered
- `MusicServiceManager` won't find any services
- Falls back to simulation

### 3. Service Registration Check

**Check server logs when app starts:**

Look for:
- "YouTube Music: Connection test" (good!)
- "YouTube Music: Not configured" (bad - missing token)
- "RealIoTController: No music service available" (bad - service not available)
- "Successfully executed IoT action" (good - working!)

### 4. Async Execution Issue

**Check:** Is the URL being added before actions are returned?

**Location:** `NeuroSync.Api/Services/DecisionEngine.cs` line ~126-157

**Current implementation:**
- `GetIoTActionsAsync` awaits execution
- URL should be added to `action.Parameters["playlistUrl"]`
- Actions returned with URL included

**If URL not in parameters:**
- Execution failed silently
- Check server logs for errors

## Debugging Steps

### Step 1: Check Server Logs

When you detect an emotion, check the console output for:

```
YouTube Music: Connection test
YouTube Music: Selected
YouTube Music: Playing music...
YouTube Music: Playlist URL generated - https://...
RealIoTController: Successfully executed IoT action
```

**OR errors like:**
```
YouTube Music: Not configured (missing API key)
RealIoTController: No music service available - simulating
RealIoTController: Error executing action - ...
```

### Step 2: Check Configuration

Verify `appsettings.json` has:
- ✅ `YouTubeMusic.AccessToken` is NOT empty
- ✅ `YouTubeMusic.ClientId` is NOT empty
- ✅ `YouTubeMusic.ClientSecret` is NOT empty

### Step 3: Check Service Registration

When app starts, check if services are registered:
- Look for any errors during startup
- Check if `RealIoTController` is being created

### Step 4: Test Service Availability

Add a test endpoint to check service status:

```csharp
[HttpGet("test-services")]
public async Task<IActionResult> TestServices()
{
    // Check if services are registered
    var musicManager = HttpContext.RequestServices.GetService<MusicServiceManager>();
    var realIoT = HttpContext.RequestServices.GetService<RealIoTController>();
    
    return Ok(new {
        musicManager = musicManager != null,
        realIoT = realIoT != null,
        services = musicManager?.GetAvailableServicesAsync()
    });
}
```

## Most Likely Issue

Based on the code, the most likely issue is:

**YouTube Music service is not being registered** because:
1. Access token might be expired
2. Access token might not be in the correct location
3. Service registration condition not met

**Check:** `Program.cs` line ~90:
```csharp
if (!string.IsNullOrEmpty(iotConfig.YouTubeMusic?.AccessToken) || !string.IsNullOrEmpty(iotConfig.YouTubeMusic?.ApiKey))
{
    // Service registered here
}
```

If this condition is false, service won't be registered!

## Quick Fix Test

1. **Check server logs** when emotion is detected
2. **Verify access token** in `appsettings.json`
3. **Restart app** to ensure services are registered
4. **Check if button appears** after restart

The button will only appear if:
- ✅ Service is registered
- ✅ Service is available
- ✅ Execution succeeds
- ✅ URL is added to parameters

