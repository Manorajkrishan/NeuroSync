# Fix: YouTube Music Not Working

## The Problem

YouTube Music service was **not registered** in `Program.cs`. The services were created but never wired up to dependency injection, so they were never actually used.

## What Was Missing

The `Program.cs` file was missing:
- ❌ IoT Configuration loading from `appsettings.json`
- ❌ Music service registration (Spotify, YouTube Music)
- ❌ MusicServiceManager registration
- ❌ SmartLightService registration
- ❌ RealIoTController registration
- ❌ Integration with DecisionEngine

## The Fix Applied

✅ **Added to `Program.cs`:**
1. Load IoT configuration from `appsettings.json`
2. Register HttpClient for API calls
3. Register Spotify Music Service (if configured)
4. Register YouTube Music Service (if configured)
5. Register MusicServiceManager
6. Register SmartLightService
7. Register RealIoTController
8. Updated DecisionEngine to use RealIoTController

✅ **Updated `DecisionEngine.cs`:**
- Now accepts RealIoTController
- Executes actions on real devices when available
- Falls back to simulation if not configured

## What You Need to Do

### Step 1: Stop the Running App

**Press `Ctrl + C` in the terminal** where the app is running.

### Step 2: Restart the App

```powershell
cd NeuroSync.Api
dotnet run
```

### Step 3: Watch the Logs

When the app starts, you should see:
```
YouTube Music: Connection test
YouTube Music: Selected (if available)
```

### Step 4: Test

1. **Open browser:** http://localhost:5063
2. **Detect an emotion** (e.g., "I feel sad")
3. **Check server console** for:
   - "YouTube Music: Connection test"
   - "YouTube Music: Playing music..."
   - Or error messages if something's wrong

## Expected Behavior

### If YouTube Music Works:

✅ **Server logs:**
- "YouTube Music: Connection test succeeded"
- "YouTube Music: Selected"
- "YouTube Music: Playing music..."

✅ **UI:**
- Music actions should execute (not just simulate)
- May show actual playlist/video information

### If YouTube Music Doesn't Work:

⚠️ **Check:**
1. **Access token expired?** (expires in 1 hour)
2. **YouTube Data API v3 enabled?** (in Google Cloud Console)
3. **Token has correct scopes?**
4. **Check server logs** for specific error messages

## Common Issues

### Issue: "Connection test failed"

**Possible causes:**
1. Access token expired (get new one)
2. YouTube Data API v3 not enabled
3. Token doesn't have required scopes
4. Network/API issues

**Solution:**
- Check Google Cloud Console
- Verify API is enabled
- Get new access token if expired

### Issue: "Not configured"

**Possible causes:**
1. Configuration not loaded correctly
2. Access token missing or empty
3. App not restarted after adding tokens

**Solution:**
- Verify `appsettings.json` has access token
- Restart the app
- Check configuration loading in logs

## Summary

- ✅ **Fixed:** Services now registered in `Program.cs`
- ✅ **Fixed:** DecisionEngine now uses real devices
- ⏭️ **Action needed:** Stop app, restart, and test

**The code is fixed! Stop the app and restart it to use YouTube Music.**

