# 🔍 Viewing Model Contents

## ML.NET Model (emotion-model.zip)

The ML.NET model is stored as a ZIP file, but you can inspect its contents:

### Method 1: Extract ZIP Manually

1. **Navigate to**: `NeuroSync.Api/Models/`
2. **Right-click** `emotion-model.zip`
3. **Extract** to a folder
4. **View contents**: You'll see the model structure

### Method 2: Use .NET Tools

```powershell
# Install ML.NET Model Inspector (if available)
dotnet tool install -g Microsoft.ML.ModelInspector

# Inspect model
mlnet-inspect NeuroSync.Api/Models/emotion-model.zip
```

### Method 3: Programmatic Inspection

I can create a utility script to:
- Extract model contents
- Show model structure
- Display training metadata
- Show input/output schemas

---

## Face-API.js Models

### Current (Internet)
- Models are downloaded from GitHub
- Cached in browser
- Not visible in project

### After Downloading Locally
Models will be in:
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

**You can:**
- ✅ See all files in project
- ✅ Inspect JSON manifests
- ✅ Check file sizes
- ✅ Version control (if needed)

---

## Why ZIP for ML.NET?

ML.NET saves models as ZIP because:
1. **Single file** - Easy to distribute
2. **Compression** - Smaller file size
3. **Standard format** - ML.NET convention
4. **Contains everything** - Model + schema + metadata

**Inside the ZIP:**
- Model pipeline definition
- Trained weights/parameters
- Input/output schemas
- Training metadata

---

## Quick Answer

**Q: Why can't I see the models?**
- **Face-API models**: They're on the internet (GitHub), not in your project
- **ML.NET model**: It's a ZIP file (standard format), but you CAN see it at `NeuroSync.Api/Models/emotion-model.zip`

**Q: Can I make them visible?**
- **Face-API models**: Yes! Download them locally (script provided)
- **ML.NET model**: Yes! Extract the ZIP or use inspection tools

**Q: Why ZIP format?**
- ML.NET's standard way to save models
- Efficient and portable
- Contains all model components in one file

