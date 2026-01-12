# Troubleshooting Guide

## Error: 500 Internal Server Error

### Understanding the Error

The `500 Internal Server Error` means something went wrong on the server side. The chrome-extension errors can be ignored - they're from browser extensions.

### How to Debug

#### Step 1: Check Server Logs

When you run the application, check the **console/terminal output** where you ran `dotnet run`. Look for:
- Error messages
- Stack traces
- "Error detecting emotion" messages

#### Step 2: Common Causes

1. **Model Not Loaded Properly**
   - First run: Model training might fail
   - Check if `Models/emotion-model.zip` exists after first run
   - Look for "Training model" messages in console

2. **Dataset Path Issue**
   - If you added a dataset file, check the path
   - Should be: `NeuroSync.Api/Data/emotions.csv`

3. **ML.NET Model Issues**
   - Model might be corrupted
   - Delete `Models/emotion-model.zip` and restart

#### Step 3: Fix Steps

**Option A: Restart the Application**
```bash
# Stop the running app (Ctrl+C)
# Then restart
cd NeuroSync.Api
dotnet run
```

**Option B: Delete and Retrain Model**
```bash
# Stop the app first (Ctrl+C)
# Delete the model
Remove-Item "NeuroSync.Api\Models\emotion-model.zip" -ErrorAction SilentlyContinue
# Restart
cd NeuroSync.Api
dotnet run
```

**Option C: Check for Build Errors**
```bash
# Stop the app first
# Clean and rebuild
cd "E:\human ai"
dotnet clean
dotnet build
```

### Getting Detailed Error Messages

The updated code now shows detailed errors in development mode. When you get a 500 error:

1. **Check Browser Console** (F12 → Console tab)
   - Look for the error response
   - It should now show the actual error message

2. **Check Server Console**
   - Look for red error messages
   - Check the stack trace

### Common Fixes

#### Fix 1: Model Training Failed

**Symptoms:**
- Error during first run
- "Failed to load model" messages

**Solution:**
```bash
# Delete model and retrain
Remove-Item "NeuroSync.Api\Models\*" -Recurse -Force
dotnet run
```

#### Fix 2: File Locked Error

**Symptoms:**
- "Cannot access file because it is being used by another process"

**Solution:**
1. Stop the running application (Ctrl+C in terminal)
2. Wait a few seconds
3. Try building/running again

#### Fix 3: Path Issues

**Symptoms:**
- "File not found" errors
- Dataset not loading

**Solution:**
- Check that paths are correct
- Ensure directories exist
- Use absolute paths if needed

### Testing the Fix

After applying fixes:

1. **Restart the application**
   ```bash
   cd NeuroSync.Api
   dotnet run
   ```

2. **Wait for startup messages:**
   ```
   Training model with 100 examples
   Model Accuracy: XX.XX%
   Model saved to: ...
   ```

3. **Test the API:**
   - Open: https://localhost:7008
   - Try detecting an emotion
   - Check browser console (F12) for errors

### Still Not Working?

1. **Check .NET Version:**
   ```bash
   dotnet --version
   ```
   Should be 8.0 or higher

2. **Restore Packages:**
   ```bash
   dotnet restore
   ```

3. **Clean Build:**
   ```bash
   dotnet clean
   dotnet build
   ```

4. **Check Logs:**
   - Look at the console output carefully
   - Copy the full error message
   - Check for specific ML.NET errors

### Getting Help

If you're still stuck:

1. **Copy the full error message** from:
   - Server console (where you ran `dotnet run`)
   - Browser console (F12 → Console)

2. **Check these files:**
   - `NeuroSync.Api/Models/emotion-model.zip` (should exist after first run)
   - Server console output (look for training messages)

3. **Common Issues:**
   - App still running (stop it first)
   - Model file corrupted (delete and retrain)
   - Missing dependencies (run `dotnet restore`)

---

## Quick Fix Checklist

- [ ] Stopped the running application (Ctrl+C)
- [ ] Checked server console for error messages
- [ ] Deleted model file and retrained (if needed)
- [ ] Restarted the application
- [ ] Checked browser console (F12) for detailed errors
- [ ] Verified .NET SDK version (8.0+)

---

**Remember**: Always stop the application before rebuilding!

