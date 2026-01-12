# 👀 How to See the Models

## Quick Answer

### **Face-API.js Models** (Facial Detection)
**Currently**: Loaded from internet (not visible in project)
**To make visible**: Download them locally using the script below

### **ML.NET Model** (Text Emotion Detection)
**Location**: `NeuroSync.Api/Models/emotion-model.zip` ✅ **Already visible!**
**Format**: ZIP file (ML.NET standard)

---

## Option 1: Download Face-API.js Models Locally (Recommended)

This makes the models visible in your project folder.

### Step 1: Run the Download Script

```powershell
cd "E:\human ai"
.\download-face-api-models.ps1
```

This will:
- Download all 8 model files (~7MB total)
- Save them to: `NeuroSync.Api/wwwroot/models/face-api/`
- Make them visible in your project

### Step 2: Models Will Be Visible

After downloading, you'll see:
```
NeuroSync.Api/wwwroot/models/face-api/
  ├── tiny_face_detector_model-weights_manifest.json
  ├── tiny_face_detector_model-shard1
  ├── face_landmark_68_model-weights_manifest.json
  ├── face_landmark_68_model-shard1
  ├── face_recognition_model-weights_manifest.json
  ├── face_recognition_model-shard1
  ├── face_expression_model-weights_manifest.json
  └── face_expression_model-shard1
```

### Step 3: Automatic Detection

The code **already updated** to:
- ✅ Try local models first
- ✅ Fall back to internet if not found
- ✅ No code changes needed!

---

## Option 2: View ML.NET Model Contents

The ML.NET model is already visible at:
```
NeuroSync.Api/Models/emotion-model.zip
```

### To See What's Inside:

**Method 1: Extract ZIP**
1. Right-click `emotion-model.zip`
2. Extract to folder
3. View contents

**Method 2: Use PowerShell**
```powershell
# Extract to view
Expand-Archive -Path "NeuroSync.Api\Models\emotion-model.zip" -DestinationPath "NeuroSync.Api\Models\emotion-model-extracted" -Force
```

**Method 3: Just Open It**
- ZIP files can be opened like folders in Windows
- Double-click to view contents

---

## Why Models Are Stored This Way

### Face-API.js Models
- **Why internet**: Standard practice, avoids bloating repository
- **Size**: ~7MB (too large for small repos)
- **Caching**: Browser caches after first download
- **Solution**: Download locally if you want to see them

### ML.NET Model
- **Why ZIP**: ML.NET's standard format
- **Contains**: Model pipeline + weights + schema
- **Size**: ~50-200KB (small, efficient)
- **Already visible**: ✅ Yes, in `Models/` folder

---

## Summary

| Model Type | Current Location | Visible? | How to See |
|------------|-----------------|----------|------------|
| **Face-API.js** | Internet (GitHub) | ❌ No | Run `download-face-api-models.ps1` |
| **ML.NET** | `Models/emotion-model.zip` | ✅ Yes | Already visible! |

---

## Quick Start

**To see Face-API.js models:**
```powershell
.\download-face-api-models.ps1
```

**To see ML.NET model contents:**
```powershell
# Just open the ZIP file or extract it
Expand-Archive -Path "NeuroSync.Api\Models\emotion-model.zip" -DestinationPath "NeuroSync.Api\Models\extracted"
```

**That's it!** The code will automatically use local models if they exist. 🎉

