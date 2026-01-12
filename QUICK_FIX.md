# Quick Fix for 500 Error

## Immediate Steps

### 1. Stop the Running Application
Press `Ctrl+C` in the terminal where the app is running.

### 2. Delete the Model (Force Retrain)
```powershell
cd "E:\human ai\NeuroSync.Api"
Remove-Item "Models\emotion-model.zip" -ErrorAction SilentlyContinue
```

### 3. Restart the Application
```powershell
dotnet run
```

### 4. Watch for Errors
Look at the console output. You should see:
- "Training model with X examples"
- "Model Accuracy: XX.XX%"
- "Model saved to: ..."

### 5. Test Again
- Open: https://localhost:7008
- Try detecting an emotion
- Check browser console (F12) if it still fails

---

## If Still Failing

### Check the Console Output
The error message will tell you what's wrong. Common issues:

**"File not found"** → Model directory issue
**"Training failed"** → Not enough data or data format issue
**"Prediction error"** → Model not loaded correctly

### Full Reset
```powershell
# Stop app first (Ctrl+C)
cd "E:\human ai"
dotnet clean
Remove-Item "NeuroSync.Api\Models\*" -Recurse -Force -ErrorAction SilentlyContinue
dotnet build
cd NeuroSync.Api
dotnet run
```

---

**The most common issue is the app still running when you try to rebuild. Always stop it first!**

