# YouTube Music Setup Guide

## ✅ Credentials Added

Your YouTube/Google OAuth credentials have been added to `appsettings.json`.

## Important Security Note ⚠️

**These credentials are sensitive!**
- ⚠️ **Never commit this file to public Git repositories**
- ⚠️ **Don't share these credentials publicly**
- ✅ For production, use environment variables or Azure Key Vault
- ✅ Consider adding `appsettings.json` to `.gitignore`

## What You Have

You have **OAuth 2.0 credentials**, which is better than just an API key because:
- ✅ Supports user-specific access
- ✅ Can refresh tokens automatically
- ✅ More secure than API keys

## Next Steps

### Step 1: Get an Access Token (One-Time Setup)

To use YouTube Music API, you need an **access token**. There are two ways:

#### Option A: Use YouTube Data API v3 (Simpler - No OAuth)
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Enable **YouTube Data API v3**
3. Create an **API Key** (not OAuth)
4. Add the API key to `appsettings.json`:
   ```json
   "YouTubeMusic": {
     "ApiKey": "YOUR_API_KEY_HERE",
     "ClientId": "YOUR_CLIENT_ID_HERE",
     "ClientSecret": "YOUR_CLIENT_SECRET_HERE"
   }
   ```

#### Option B: Use OAuth 2.0 Flow (More Powerful)
For full playback control, you need to complete the OAuth flow:

1. **Set up OAuth Redirect URI:**
   - Go to [Google Cloud Console](https://console.cloud.google.com/)
   - Navigate to: APIs & Services → Credentials
   - Edit your OAuth 2.0 Client
   - Add Authorized redirect URIs:
     - `http://localhost:5063/api/auth/youtube/callback` (for development)
     - `https://yourdomain.com/api/auth/youtube/callback` (for production)

2. **Get Access Token:**
   - Use Google's OAuth 2.0 Playground: https://developers.google.com/oauthplayground/
   - Or implement OAuth flow in your app
   - Scopes needed:
     - `https://www.googleapis.com/auth/youtube.readonly`
     - `https://www.googleapis.com/auth/youtube.force-ssl` (for playback control)

3. **Add Token to Config:**
   ```json
   "YouTubeMusic": {
     "ClientId": "YOUR_CLIENT_ID_HERE",
     "ClientSecret": "YOUR_CLIENT_SECRET_HERE",
     "AccessToken": "YOUR_ACCESS_TOKEN_HERE",
     "RefreshToken": "YOUR_REFRESH_TOKEN_HERE"
   }
   ```

### Step 2: Enable YouTube Data API v3

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Select your project: `ceremonial-team-461012-r2`
3. Navigate to: APIs & Services → Library
4. Search for "YouTube Data API v3"
5. Click "Enable"

### Step 3: Update YouTubeMusicService (If Needed)

The current implementation supports both API keys and OAuth tokens. The service will:
- Try OAuth token first (if provided)
- Fall back to API key (if provided)
- Use client credentials for authentication (if available)

### Step 4: Test the Integration

1. **Restart your app:**
   ```powershell
   cd NeuroSync.Api
   dotnet run
   ```

2. **Check logs:**
   - Look for "YouTube Music: Connection test" messages
   - Should see "YouTube Music: Selected" if configured correctly

3. **Test emotion detection:**
   - Detect an emotion that triggers music
   - Check server logs for YouTube Music API calls

## Current Configuration

Your `appsettings.json` now has:
```json
{
  "IoT": {
    "YouTubeMusic": {
      "ClientId": "YOUR_CLIENT_ID_HERE",
      "ClientSecret": "YOUR_CLIENT_SECRET_HERE"
    }
  }
}
```

## Limitations

⚠️ **YouTube Music API Limitations:**
- Full playback control requires **YouTube Music Premium API** (limited availability)
- Basic API key allows searching and getting playlists
- For actual playback, you may need:
  - YouTube Music Premium subscription
  - Chromecast SDK for device integration
  - Or use Spotify (which has better API support)

## Recommendation

For **music playback**, Spotify is recommended because:
- ✅ Better API support for playback control
- ✅ Easier to implement
- ✅ More reliable
- ✅ Full playback control

YouTube Music is better for:
- ✅ Video content
- ✅ Playlist management
- ✅ Search functionality

## Security Reminders

1. **Add to .gitignore:**
   ```gitignore
   appsettings.json
   appsettings.Development.json
   ```

2. **Use User Secrets (Development):**
   ```powershell
   dotnet user-secrets set "IoT:YouTubeMusic:ClientSecret" "YOUR_CLIENT_SECRET_HERE"
   ```

3. **Use Environment Variables (Production):**
   ```bash
   export IoT__YouTubeMusic__ClientSecret="YOUR_CLIENT_SECRET_HERE"
   ```

## Next Steps Summary

1. ✅ Credentials added to config
2. ⏭️ Get API key OR complete OAuth flow
3. ⏭️ Enable YouTube Data API v3
4. ⏭️ Add API key or access token to config
5. ⏭️ Restart app and test

Your credentials are configured! You just need to get an access token or API key to start using it.

