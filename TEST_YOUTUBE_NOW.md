# Test YouTube Music Integration Now

## ✅ Tokens Configured!

Your YouTube OAuth tokens have been added to `appsettings.json`. Let's test it!

## Quick Test Steps

### Step 1: Restart the App

1. **Stop the app** (if running):
   - Press `Ctrl + C` in the terminal

2. **Start the app:**
   ```powershell
   cd NeuroSync.Api
   dotnet run
   ```

### Step 2: Watch Server Logs

When the app starts, look for:
- ✅ "YouTube Music: Connection test"
- ✅ "YouTube Music: Selected" (if available)
- ⚠️ Any error messages

### Step 3: Test Emotion Detection

1. **Open browser:** http://localhost:5063
2. **Enter text that triggers music:**
   - "I feel sad" → Should trigger calming music
   - "I'm so happy!" → Should trigger upbeat music
   - "I'm feeling anxious" → Should trigger ambient music

3. **Check what happens:**
   - Look at "IoT Device Actions" section
   - Should show music action (not just simulation)
   - Check server console for API calls

### Step 4: Check Server Console

Look for messages like:
```
YouTube Music: Connection test
YouTube Music: Selected
YouTube Music: Playing calming music...
YouTube Music: Search videos...
```

## Expected Behavior

### If YouTube Music Works:

✅ **Server logs show:**
- "YouTube Music: Connection test succeeded"
- "YouTube Music: Selected"
- "YouTube Music: Playing music..."

✅ **UI shows:**
- Music action with playlist details
- May show "SIMULATION MODE" badge removed (if implementation updated)

### If YouTube Music Doesn't Work:

⚠️ **Server logs show:**
- "YouTube Music: Not configured"
- "YouTube Music: Connection test failed"
- Error messages

**Common issues:**
1. **API not enabled:** Enable YouTube Data API v3 in Google Cloud Console
2. **Token expired:** Access token expires in 1 hour
3. **Scope issues:** Token might not have required scopes
4. **Service limitations:** YouTube Music API has playback limitations

## YouTube Music API Limitations

**Important:** YouTube Music API has limitations:

1. **Playback Control:**
   - Full playback control requires **YouTube Music Premium API** (limited availability)
   - Basic API allows search and playlist access
   - Actual playback may require:
     - YouTube Music Premium subscription
     - Device integration (Chromecast SDK)
     - Or use Spotify (better playback support)

2. **What You CAN Do:**
   - ✅ Search for videos/playlists
   - ✅ Get playlist information
   - ✅ Get video metadata
   - ✅ Generate playlist URLs

3. **What You CAN'T Do (easily):**
   - ❌ Direct playback control (limited)
   - ❌ Play music automatically (requires Premium API or device integration)
   - ❌ Full control without Premium API

## Troubleshooting

### Issue: "Connection test failed"

**Check:**
1. YouTube Data API v3 enabled in Google Cloud Console?
2. Access token valid? (expires in 1 hour)
3. Credentials correct in `appsettings.json`?

### Issue: "Not configured"

**Check:**
1. `appsettings.json` has access token?
2. Token format correct?
3. Restarted app after adding tokens?

### Issue: Music Doesn't Play

**This is expected!** YouTube Music API limitations:
- Basic API doesn't support direct playback
- Need Premium API or device integration
- System will search/select music but may not play automatically

**For actual playback, consider:**
- Spotify (better playback support)
- Or implement YouTube Music Premium API integration
- Or use Chromecast SDK for device integration

## Next Steps

1. ✅ Restart app
2. ✅ Test emotion detection
3. ✅ Check server logs
4. ✅ Verify YouTube Music service is detected
5. ⏭️ Consider Spotify for better playback control (if needed)

## Summary

- ✅ Tokens configured
- ✅ Ready to test
- ⚠️ YouTube Music API has playback limitations
- 💡 For better music playback: Consider Spotify

**Restart the app and test it now!**

