# Dataset Guide for NeuroSync

## How Training Works

### Current Setup

The project **WILL run** even without an external dataset because:

1. **Automatic Training**: On first run, the system automatically trains a model using built-in sample data (~100+ examples)
2. **Model Persistence**: The trained model is saved to `NeuroSync.Api/Models/emotion-model.zip`
3. **Fast Subsequent Runs**: After first run, the saved model is loaded (no retraining needed)

### Current Training Data

The system includes **~100 sample training examples** covering all 8 emotion types:
- Happy: ~15 examples
- Sad: ~13 examples  
- Angry: ~12 examples
- Anxious: ~12 examples
- Calm: ~10 examples
- Excited: ~10 examples
- Frustrated: ~10 examples
- Neutral: ~11 examples

**⚠️ Important**: This is a **minimal dataset** for demonstration. For production use, you should use a larger, real dataset.

---

## Using Your Own Dataset

### Option 1: CSV File (Recommended)

1. **Create a CSV file** at: `NeuroSync.Api/Data/emotions.csv`

2. **Format** (Text,Label):
   ```csv
   Text,Label
   "I'm feeling great today!",happy
   "This is so frustrating!",frustrated
   "I feel calm and peaceful.",calm
   "I'm really worried about this.",anxious
   ```

3. **Supported Labels**:
   - `happy`
   - `sad`
   - `angry`
   - `anxious`
   - `calm`
   - `excited`
   - `frustrated`
   - `neutral`

4. **The system will automatically**:
   - Detect the CSV file
   - Load it on startup
   - Train the model with your data
   - Save the trained model

### Option 2: TSV File

Same as CSV but use `.tsv` extension and tab-separated values:
```tsv
Text	Label
"I'm feeling great today!"	happy
"This is so frustrating!"	frustrated
```

---

## Where to Get Datasets

### Free Emotion Datasets:

1. **GoEmotions Dataset** (Google)
   - Large dataset with 27 emotion categories
   - Available on GitHub/HuggingFace
   - Need to filter/convert to your 8 categories

2. **Emotion Dataset** (Kaggle)
   - Search "emotion classification dataset" on Kaggle
   - Many free datasets available

3. **ISEAR Dataset**
   - International Survey on Emotion Antecedents and Reactions
   - Academic dataset

4. **Twitter Emotion Dataset**
   - Various Twitter emotion datasets available
   - Good for real-world text

### Recommended Dataset Size:

- **Minimum**: 500-1000 examples per emotion (4000-8000 total)
- **Good**: 2000+ examples per emotion (16,000+ total)
- **Excellent**: 5000+ examples per emotion (40,000+ total)

---

## How to Add Your Dataset

### Step 1: Prepare Your Dataset

1. Download or create a CSV file with format:
   ```csv
   Text,Label
   "Your text here",happy
   "Another text",sad
   ```

2. Ensure labels match exactly:
   - `happy`, `sad`, `angry`, `anxious`, `calm`, `excited`, `frustrated`, `neutral`
   - All lowercase

### Step 2: Place the File

Put your dataset file here:
```
NeuroSync.Api/
  └── Data/
      └── emotions.csv  (or emotions.tsv)
```

### Step 3: Run the Application

```bash
cd NeuroSync.Api
dotnet run
```

The system will:
1. Detect your dataset file
2. Load all examples
3. Train the model (takes longer with large datasets)
4. Save the trained model
5. Use it for predictions

---

## Example: Creating a Dataset File

### Using Python (if you have it):

```python
import csv

# Your data
data = [
    ("I'm so happy!", "happy"),
    ("This is great!", "happy"),
    ("I feel sad.", "sad"),
    # ... more examples
]

# Write to CSV
with open('emotions.csv', 'w', newline='', encoding='utf-8') as f:
    writer = csv.writer(f)
    writer.writerow(['Text', 'Label'])
    for text, label in data:
        writer.writerow([text, label])
```

### Using Excel/Google Sheets:

1. Create two columns: `Text` and `Label`
2. Fill with your data
3. Export as CSV
4. Place in `NeuroSync.Api/Data/emotions.csv`

### Manual Creation:

Just create a text file with:
```csv
Text,Label
"I'm happy",happy
"I'm sad",sad
```

---

## Model Training Process

When you run the application:

1. **Check for saved model**: Looks for `Models/emotion-model.zip`
   - If found → Loads it (fast, ~1 second)
   - If not found → Trains new model

2. **Load training data**:
   - First checks: `Data/emotions.csv` (your dataset)
   - If not found: Uses built-in sample data

3. **Train model**:
   - Splits data: 80% training, 20% testing
   - Trains using ML.NET SDCA algorithm
   - Evaluates accuracy
   - Saves model to disk

4. **Console output**:
   ```
   Training model with 100 examples
   Model Accuracy: 85.50%
   Log Loss: 0.4521
   Model saved to: .../emotion-model.zip
   ```

---

## Improving Model Accuracy

### 1. More Data
- Add more examples to your dataset
- Aim for balanced distribution across emotions

### 2. Better Data Quality
- Remove typos and errors
- Ensure labels are correct
- Use diverse text samples

### 3. Data Preprocessing
- The system automatically handles text featurization
- No manual preprocessing needed

### 4. Retrain Model
- Delete `Models/emotion-model.zip`
- Run the application again
- New model will be trained with updated data

---

## Current Limitations

⚠️ **With sample data only (~100 examples)**:
- Accuracy: ~60-75% (varies)
- May misclassify similar emotions
- Works for demonstration but not production

✅ **With real dataset (1000+ examples)**:
- Accuracy: ~80-90%+
- Much better classification
- Suitable for real-world use

---

## Testing Your Model

After training, test with:

```bash
# Via Web Interface
https://localhost:7008

# Try various inputs:
- "I'm feeling great!" → Should detect Happy
- "This is frustrating!" → Should detect Frustrated
- "I'm really worried" → Should detect Anxious
```

---

## Troubleshooting

### Dataset Not Loading?

1. **Check file path**: Must be `NeuroSync.Api/Data/emotions.csv`
2. **Check format**: Must be `Text,Label` with header
3. **Check labels**: Must match exactly (lowercase)
4. **Check encoding**: Use UTF-8 encoding

### Low Accuracy?

1. **Add more data**: More examples = better accuracy
2. **Check data quality**: Ensure labels are correct
3. **Balance data**: Similar number of examples per emotion
4. **Retrain**: Delete model file and retrain

### Training Takes Too Long?

- Normal for large datasets (1000+ examples)
- First training: 30 seconds - 5 minutes depending on size
- Subsequent runs: Fast (uses saved model)

---

## Summary

✅ **The project WILL run** without external dataset (uses sample data)  
✅ **For better accuracy**, add your own dataset file  
✅ **Place dataset** at: `NeuroSync.Api/Data/emotions.csv`  
✅ **Format**: CSV with `Text,Label` columns  
✅ **Model auto-trains** on first run or when dataset changes  

**The system is designed to work out-of-the-box, but improves significantly with real training data!**

