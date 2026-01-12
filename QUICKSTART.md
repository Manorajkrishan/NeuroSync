# NeuroSync Quick Start Guide

## Quick Setup (5 minutes)

### 1. Prerequisites Check
Ensure you have .NET 8.0 SDK installed:
```bash
dotnet --version
```
Should show 8.0.x or higher.

### 2. Build the Solution
```bash
cd "E:\human ai"
dotnet build
```

### 3. Run the Application
```bash
cd NeuroSync.Api
dotnet run
```

### 4. Access the Application
- **Web Interface**: Open `https://localhost:5001` or `http://localhost:5000` in your browser
- **API Documentation**: Visit `https://localhost:5001/swagger`

### 5. Test It Out!

Try typing these sample emotions:
- "I'm feeling really happy today!" → Should detect **Happy**
- "This is so frustrating!" → Should detect **Frustrated**
- "I feel calm and peaceful" → Should detect **Calm**
- "I'm really worried about this" → Should detect **Anxious**

## What to Expect

1. **First Run**: The ML model will be trained automatically (takes a few seconds)
2. **Emotion Detection**: Type text and click "Detect Emotion"
3. **Real-time Updates**: See emotion results, adaptive responses, and IoT actions appear instantly
4. **IoT Simulation**: Watch simulated device actions based on your emotions

## Troubleshooting

**Port already in use?**
- Change ports in `Properties/launchSettings.json`
- Or kill the process using the port

**Model training takes time?**
- Normal on first run
- Subsequent runs will use the saved model

**SignalR connection issues?**
- Check browser console for errors
- Ensure HTTPS certificate is trusted (run `dotnet dev-certs https --trust`)

## Next Steps

- Read the full [README.md](README.md) for detailed documentation
- Explore the API using Swagger UI
- Check the code structure to understand the architecture
- Extend the system with your own features!

Happy coding! 🚀


