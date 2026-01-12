# 🎤 Voice Features Guide

## ✨ New Voice Capabilities

Your NeuroSync system now has **powerful voice features** that make it feel more like a real friend!

### 1. **Text-to-Speech (TTS)** 🔊

The AI can now **speak** to you! When you enable TTS, the system will automatically speak its responses.

**How to Use:**
1. Click the **"🔇 TTS Off"** button to enable TTS
2. The button changes to **"🔊 TTS On"** when enabled
3. All AI responses will now be spoken automatically
4. Click again to disable

**Features:**
- Natural-sounding speech
- Automatic speaking of all responses
- Can be toggled on/off anytime
- Settings are saved in your browser

---

### 2. **Voice Cloning** 🎭

**Upload a voice sample** from someone you care about, and the AI will **speak in their voice** when you miss them!

**How to Use:**
1. **Upload Voice Sample:**
   - Click **"📤 Upload Voice Sample"**
   - Select an audio file (15-60 seconds recommended)
   - Enter the person's name (e.g., "Dilshika")
   - Click "Upload"

2. **Use Cloned Voice:**
   - After uploading, you'll see the cloned voice in the list
   - Click **"🎭 Use This Voice"** to activate it
   - The AI will now speak in that person's voice!

**Perfect For:**
- Missing someone: "I miss my best friend" → AI speaks in their voice
- Comfort: When you're sad, hear their voice
- Emotional connection: Feel closer to people you care about

**Requirements:**
- Audio file (MP3, WAV, OGG, etc.)
- 15-60 seconds of clear speech
- Person's name for identification

**Note:** Voice cloning requires ElevenLabs API key (optional). Without it, default TTS is used.

---

## 🎯 Integration with Emotions

The voice features are **automatically integrated** with emotion detection:

- **When you're sad** → AI speaks comforting words in a warm voice
- **When you miss someone** → AI can speak in their cloned voice
- **When you're happy** → AI celebrates with you in an energetic voice
- **All responses** → Spoken automatically when TTS is enabled

---

## 🔧 Setup (Optional)

For **voice cloning** to work, you need an ElevenLabs API key:

1. Sign up at [ElevenLabs](https://elevenlabs.io/)
2. Get your API key
3. Add to `appsettings.json`:
   ```json
   {
     "ElevenLabs": {
       "ApiKey": "your-api-key-here"
     }
   }
   ```

**Without API key:**
- TTS still works (browser-based)
- Voice cloning uses default TTS
- All other features work normally

---

## 💡 Tips

1. **Best Voice Samples:**
   - Clear, quiet environment
   - Natural speech (not too fast or slow)
   - 15-60 seconds of continuous speech
   - No background noise

2. **When to Use Cloned Voices:**
   - When you miss someone
   - During emotional moments
   - For comfort and connection
   - Special occasions

3. **TTS Settings:**
   - Enable for hands-free interaction
   - Disable if you prefer reading
   - Great for accessibility
   - Perfect for multitasking

---

## 🎉 Result

Your system now:
- ✅ **Speaks** all responses when enabled
- ✅ **Clones voices** from uploaded samples
- ✅ **Uses cloned voices** when you miss someone
- ✅ **Feels more personal** and connected
- ✅ **Comforts you** in familiar voices

**The AI truly feels like a friend now!** 💚
