# Real Device Integration Guide

## Overview

NeuroSync now supports real device integration for music services and smart lights. The system will automatically detect configured services and use them, falling back to simulation if none are available.

## Music Services

### Supported Services
- **Spotify** (Priority 1)
- **YouTube Music** (Priority 2)

The system automatically selects the best available service (Spotify > YouTube Music), or you can specify a preference in configuration.

### Spotify Setup

1. **Create a Spotify App:**
   - Go to [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
   - Create a new app
   - Note your **Client ID** and **Client Secret**

2. **Get Access Token:**
   - Use Spotify's OAuth flow to get an access token
   - For testing, use the [Web API Console](https://developer.spotify.com/console/)
   - Access tokens expire (usually 1 hour), refresh tokens are needed for long-term use

3. **Configure in `appsettings.json`:**
   ```json
   {
     "IoT": {
       "Spotify": {
         "ClientId": "your-client-id",
         "ClientSecret": "your-client-secret",
         "AccessToken": "your-access-token",
         "RefreshToken": "your-refresh-token"
       }
     }
   }
   ```

### YouTube Music Setup

1. **Get YouTube Data API Key:**
   - Go to [Google Cloud Console](https://console.cloud.google.com/)
   - Create a new project or select existing
   - Enable YouTube Data API v3
   - Create credentials (API Key)
   - Note: For full playback control, you need YouTube Music Premium API (limited availability)

2. **Configure in `appsettings.json`:**
   ```json
   {
     "IoT": {
       "YouTubeMusic": {
         "ApiKey": "your-api-key",
         "AccessToken": "your-oauth-token" // Optional, for user-specific access
       }
     }
   }
   ```

**Note:** YouTube Music API has limitations. Full playback control requires:
- YouTube Music Premium subscription
- OAuth authentication for user accounts
- Device integration (Cast SDK for Chromecast, etc.)

## Smart Lights

### Supported Services
- **Philips Hue** (Priority 1)
- **LIFX** (Priority 2)

The system tries Philips Hue first, then LIFX.

### Philips Hue Setup

1. **Find Your Bridge IP:**
   - Use the Philips Hue app or check your router
   - Or use discovery: `https://www.meethue.com/api/nupnp`

2. **Create Username:**
   - Press the button on your Hue Bridge
   - Make a POST request to `http://BRIDGE_IP/api` with body:
     ```json
     { "devicetype": "NeuroSync#app" }
     ```
   - Or use the [Hue Developer Tools](https://developers.meethue.com/develop/get-started-2/)

3. **Get Light IDs:**
   - GET `http://BRIDGE_IP/api/USERNAME/lights`
   - Note the light IDs (usually "1", "2", "3", etc.)

4. **Configure in `appsettings.json`:**
   ```json
   {
     "IoT": {
       "PhilipsHue": {
         "BridgeIp": "192.168.1.100",
         "Username": "your-username-here"
       }
     }
   }
   ```

### LIFX Setup

1. **Get API Key:**
   - Go to [LIFX Cloud](https://cloud.lifx.com/)
   - Sign in and create an API token
   - Copy your API key

2. **Get Light IDs:**
   - Use the LIFX API: `GET https://api.lifx.com/v1/lights/all`
   - Use `Authorization: Bearer YOUR_API_KEY` header
   - Note the light IDs (labels)

3. **Configure in `appsettings.json`:**
   ```json
   {
     "IoT": {
       "LIFX": {
         "ApiKey": "your-api-key"
       }
     }
   }
   ```

## Configuration

### Preferred Music Service

Set `PreferredMusicService` in `appsettings.json`:
- `"Auto"` - Automatically selects best available (Spotify > YouTube Music)
- `"Spotify"` - Force Spotify (if available)
- `"YouTubeMusic"` - Force YouTube Music (if available)

Example:
```json
{
  "IoT": {
    "PreferredMusicService": "Spotify"
  }
}
```

## How It Works

1. **Service Detection:** On startup, the system checks which services are configured
2. **Fallback:** If no services are configured, the system falls back to simulation mode
3. **Priority:** Services are tried in priority order (Spotify > YouTube Music, Philips Hue > LIFX)
4. **Error Handling:** If a service fails, it logs the error and continues (simulation mode)

## Testing

1. **Check Service Availability:**
   - Use the diagnostic endpoint: `GET /api/diagnostic/test`
   - Check server logs for service initialization messages

2. **Test Music Playback:**
   - Detect an emotion in the UI
   - Check server logs for music service calls
   - Verify music plays on your device (Spotify app, YouTube Music, etc.)

3. **Test Light Control:**
   - Detect an emotion that triggers light changes
   - Check server logs for light control calls
   - Verify lights change color/brightness

## Troubleshooting

### Music Not Playing

1. **Check Credentials:**
   - Verify API keys/tokens are correct
   - Check if tokens have expired (Spotify tokens expire after 1 hour)

2. **Check Device:**
   - Make sure a device is available/active (Spotify: device must be open)
   - For YouTube Music, device integration may be required

3. **Check Logs:**
   - Look for error messages in server console
   - Check for "Not configured" or "Connection test failed" messages

### Lights Not Changing

1. **Check Network:**
   - Verify bridge/device is on the same network
   - Check IP addresses are correct

2. **Check Authentication:**
   - For Philips Hue, verify username is correct
   - For LIFX, verify API key is valid

3. **Check Device IDs:**
   - Use API calls to list devices and verify IDs

## Security Notes

- **Never commit credentials to Git!**
- Use environment variables or User Secrets for production
- Consider using Azure Key Vault, AWS Secrets Manager, etc. for production

Example with User Secrets (Development):
```bash
dotnet user-secrets set "IoT:Spotify:ClientId" "your-client-id"
dotnet user-secrets set "IoT:Spotify:ClientSecret" "your-client-secret"
```

## Next Steps

1. **Add More Services:**
   - Apple Music
   - Amazon Music
   - More smart light brands

2. **Add Device Discovery:**
   - Auto-discover devices on network
   - List available devices in UI

3. **Add User Preferences:**
   - Allow users to select preferred services
   - Save preferences per user

4. **Add OAuth Flows:**
   - Implement proper OAuth for Spotify/YouTube
   - Token refresh automation

