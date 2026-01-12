# How to Retrain the Model with New Data

## Why Retrain?

The model was trained with limited data. We've added more training examples (especially for breakup/sad situations), but the **existing model file** still has the old training data.

## Steps to Retrain

### Step 1: Stop the Running App
- Press `Ctrl + C` in the terminal where the app is running

### Step 2: Delete the Old Model File

**Option A: Delete from Project Folder**
```
Delete: NeuroSync.Api/Models/emotion-model.zip
```

**Option B: Delete from Build Output**
```
Delete: NeuroSync.Api/bin/Debug/net8.0/Models/emotion-model.zip
```

**Option C: Use Command Line (PowerShell)**
```powershell
Remove-Item "NeuroSync.Api\Models\emotion-model.zip" -ErrorAction SilentlyContinue
Remove-Item "NeuroSync.Api\bin\Debug\net8.0\Models\emotion-model.zip" -ErrorAction SilentlyContinue
```

### Step 3: Restart the App

```powershell
cd NeuroSync.Api
dotnet run
```

### Step 4: Wait for Training

When you start the app:
1. It will detect the model file is missing
2. It will automatically retrain with the new data
3. You'll see console output like:
   ```
   Training model with new data...
   Model Accuracy: XX.XX%
   Model saved to: ...
   ```

### Step 5: Test

Try detecting emotion with:
- "I am going thru a breakup, i can not control my self"
- Should now detect as **Sad** instead of Neutral

## What Changed in Training Data

Added 12+ new examples for sad/breakup situations:
- "I am going through a breakup"
- "I can't control myself"
- "My relationship ended and I feel lost"
- "I'm heartbroken about the breakup"
- And more...

## Note

- The model will be retrained automatically on first run after deletion
- Training takes a few seconds
- The new model will be saved and reused on subsequent runs
- You only need to delete the model file once

