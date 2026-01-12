# How to Run NeuroSync

## Quick Start (3 Steps)

### Step 1: Navigate to the API Project
```bash
cd "E:\human ai\NeuroSync.Api"
```

### Step 2: Run the Application
```bash
dotnet run
```

### Step 3: Open in Browser
Once you see "Now listening on: https://localhost:7008", open your browser and go to:

**🌐 Web Interface:** https://localhost:7008

**📚 API Documentation:** https://localhost:7008/swagger

---

## Detailed Instructions

### Option A: Using Command Line (PowerShell/CMD)

1. **Open Terminal/PowerShell**
   - Press `Win + X` and select "Terminal" or "PowerShell"

2. **Navigate to the project**
   ```powershell
   cd "E:\human ai\NeuroSync.Api"
   ```

3. **Run the application**
   ```powershell
   dotnet run
   ```

4. **Wait for the application to start**
   You'll see output like:
   ```
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: https://localhost:7008
         Now listening on: http://localhost:5063
   ```

5. **Open your browser**
   - Go to: **https://localhost:7008**
   - Or: **http://localhost:5063**

### Option B: Using Visual Studio

1. **Open the solution**
   - Double-click `NeuroSync.sln` or open it in Visual Studio

2. **Set startup project**
   - Right-click `NeuroSync.Api` project
   - Select "Set as Startup Project"

3. **Run the application**
   - Press `F5` (Debug) or `Ctrl+F5` (Run without debugging)

4. **Browser will open automatically**
   - Usually opens to Swagger UI
   - Navigate to root URL for the web interface

### Option C: Using Visual Studio Code

1. **Open the folder**
   - File → Open Folder → Select `E:\human ai`

2. **Open Terminal**
   - Press `` Ctrl + ` `` (backtick) or Terminal → New Terminal

3. **Run the application**
   ```bash
   cd NeuroSync.Api
   dotnet run
   ```

---

## First Run Notes

⚠️ **On the first run:**
- The ML model will be trained automatically (takes 10-30 seconds)
- You'll see console output about model training
- The model will be saved to `NeuroSync.Api/Models/emotion-model.zip`
- Subsequent runs will be faster as they load the saved model

---

## Accessing the Application

### Web Interface (Main UI)
- **URL:** https://localhost:7008 or http://localhost:5063
- **Features:** 
  - Text input for emotion detection
  - Real-time results display
  - IoT action visualization

### API Documentation (Swagger)
- **URL:** https://localhost:7008/swagger
- **Features:**
  - Interactive API testing
  - Endpoint documentation
  - Try out API calls directly

### API Endpoints
- **Detect Emotion:** POST `/api/emotion/detect`
- **Get Emotion Types:** GET `/api/emotion/types`
- **SignalR Hub:** `/emotionHub`

---

## Testing the Application

### 1. Test via Web Interface
1. Open https://localhost:7008
2. Type: "I'm feeling really happy today!"
3. Click "Detect Emotion" or press Enter
4. See the results appear in real-time!

### 2. Test via Swagger
1. Open https://localhost:7008/swagger
2. Find `POST /api/emotion/detect`
3. Click "Try it out"
4. Enter JSON:
   ```json
   {
     "text": "I'm feeling great!"
   }
   ```
5. Click "Execute"

### 3. Sample Test Cases
Try these emotions:
- **Happy:** "I'm so happy today!"
- **Sad:** "I feel really down"
- **Angry:** "This makes me furious!"
- **Anxious:** "I'm really worried about this"
- **Calm:** "I feel peaceful and relaxed"
- **Excited:** "I'm thrilled about this!"
- **Frustrated:** "This is so frustrating!"
- **Neutral:** "I see, that makes sense"

---

## Troubleshooting

### Port Already in Use
**Error:** "Address already in use"

**Solution:**
1. Find and kill the process using the port:
   ```powershell
   netstat -ano | findstr :7008
   taskkill /PID <PID> /F
   ```
2. Or change the port in `Properties/launchSettings.json`

### HTTPS Certificate Error
**Error:** "Your connection is not private"

**Solution:**
```bash
dotnet dev-certs https --trust
```

### Model Training Takes Time
**Normal!** First run trains the model. Wait 10-30 seconds.

### SignalR Connection Issues
- Check browser console (F12) for errors
- Ensure you're using HTTPS (https://localhost:7008)
- Check CORS settings if accessing from different origin

### Build Errors
```bash
# Restore packages
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build
```

---

## Stopping the Application

- **In Terminal:** Press `Ctrl + C`
- **In Visual Studio:** Click the Stop button or press `Shift + F5`

---

## Default URLs

- **HTTPS:** https://localhost:7008
- **HTTP:** http://localhost:5063
- **Swagger:** https://localhost:7008/swagger

---

## Need Help?

- Check `README.md` for detailed documentation
- Check `QUICKSTART.md` for quick reference
- Review console output for error messages
- Check browser console (F12) for frontend errors

---

**Happy coding! 🚀**

