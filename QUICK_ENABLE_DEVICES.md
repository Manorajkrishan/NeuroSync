# Quick Guide: Enable Real Device Control

## Current Status: Simulation Mode ✅

Your system is working correctly but in **simulation mode** because no real devices are configured.

## What You See

When you detect an emotion, you see:
- 💡 Light actions (color, brightness)
- 🔊 Music actions (genre, playlist, volume)
- 🔔 Notification messages

**These are simulated** - they show what WOULD happen, but don't actually control devices.

## How to Make Them Work

### Step 1: Choose Your Devices

You need at least ONE of these:

#### Option A: Smart Lights (Easiest)
- **Philips Hue** (most popular)
- **LIFX** (alternative)

#### Option B: Music Services
- **Spotify** (best support)
- **YouTube Music** (you already have credentials!)

### Step 2: Configure Devices

#### For Philips Hue (Smart Lights)

1. **Find Bridge IP:**
   - Open Philips Hue app
   - Settings → Bridge settings → IP address
   - Example: `192.168.1.100`

2. **Create Username:**
   - Press button on Hue Bridge
   - Run this command (replace IP):
     ```powershell
     Invoke-WebRequest -Uri "http://192.168.1.100/api" -Method POST -Body '{"devicetype":"NeuroSync#app"}' -ContentType "application/json"
     ```
   - Copy the username from response

3. **Update appsettings.json:**
   ```json
   "PhilipsHue": {
     "BridgeIp": "192.168.1.100",
     "Username": "your-username-here"
   }
   ```

#### For Spotify (Music)

1. **Get Credentials:**
   - Go to https://developer.spotify.com/dashboard
   - Create app → Get Client ID and Secret

2. **Get Access Token:**
   - Use https://developer.spotify.com/console/
   - Authorize → Copy access token

3. **Update appsettings.json:**
   ```json
   "Spotify": {
     "ClientId": "your-client-id",
     "ClientSecret": "your-client-secret",
     "AccessToken": "your-access-token"
   }
   ```

#### For YouTube Music (Music) - You Have Credentials!

1. **Get API Key:**
   - Go to Google Cloud Console
   - APIs & Services → Credentials
   - Create API Key
   - Enable YouTube Data API v3

2. **Update appsettings.json:**
   ```json
   "YouTubeMusic": {
     "ApiKey": "YOUR_API_KEY_HERE",
     "ClientId": "YOUR_CLIENT_ID_HERE",
     "ClientSecret": "YOUR_CLIENT_SECRET_HERE"
   }
   ```

### Step 3: Restart the App

1. **Stop the app:** Press `Ctrl + C`
2. **Start again:**
   ```powershell
   cd NeuroSync.Api
   dotnet run
   ```

### Step 4: Test!

1. **Open the app** in browser
2. **Detect an emotion** (e.g., "I feel sad")
3. **Watch the magic:**
   - If lights configured → **Lights actually change!**
   - If music configured → **Music actually plays!**

## Example Configuration

Here's a complete `appsettings.json` example:

```json
{
  "IoT": {
    "PreferredMusicService": "Auto",
    "Spotify": {
      "ClientId": "your-spotify-client-id",
      "ClientSecret": "your-spotify-secret",
      "AccessToken": "your-spotify-token"
    },
    "YouTubeMusic": {
      "ApiKey": "YOUR_API_KEY_HERE",
      "ClientId": "YOUR_CLIENT_ID_HERE",
      "ClientSecret": "YOUR_CLIENT_SECRET_HERE"
    },
    "PhilipsHue": {
      "BridgeIp": "192.168.1.100",
      "Username": "your-hue-username"
    }
  }
}
```

## What About Phone/Computer Settings?

**Current system does NOT control:**
- ❌ Phone airplane mode
- ❌ Low light mode
- ❌ Computer brightness
- ❌ System settings

**Why?**
- This is a **web application** (runs in browser)
- Cannot access device/system settings
- Requires native apps (mobile/desktop apps)

**To control device settings, you would need:**
- Mobile app (iOS/Android) with permissions
- Desktop application with system access
- This is a different project/feature

## Quick Checklist

- [ ] Choose devices (Hue lights, Spotify, etc.)
- [ ] Get credentials/configuration
- [ ] Update `appsettings.json`
- [ ] Restart the app
- [ ] Test emotion detection
- [ ] Watch real devices respond!

## Need Help?

- **Philips Hue Setup:** See `INTEGRATION_GUIDE.md`
- **Spotify Setup:** See `INTEGRATION_GUIDE.md`
- **YouTube Music Setup:** See `YOUTUBE_SETUP.md`
- **How it works:** See `HOW_DEVICES_WORK.md`

**Once you configure real devices, the simulation mode will automatically switch to real device control!**

