# 📊 Real-World Emotion Datasets for Training

## 🎯 Recommended Datasets

### 1. **GoEmotions Dataset** (Google Research) ⭐ BEST FOR EMOTION
- **Size**: 58,000 Reddit comments
- **Emotions**: 27 categories (happy, sad, angry, fear, etc.)
- **Format**: CSV/JSON
- **Source**: 
  - Hugging Face: `huggingface.co/datasets/go_emotions`
  - GitHub: `github.com/google-research/google-research/tree/master/goemotions`
- **Perfect for**: Multi-class emotion classification

### 2. **Emotion Dataset** (Hugging Face) ⭐ EASY TO USE
- **Size**: 20,000 English Twitter messages
- **Emotions**: 6 categories (joy, sadness, anger, fear, love, surprise)
- **Format**: CSV/JSON
- **Source**: `huggingface.co/datasets/dair-ai/emotion`
- **Perfect for**: Quick start, real-world social media emotions

### 3. **Sentiment140** (Stanford)
- **Size**: 1.6 million tweets
- **Labels**: Positive, Negative, Neutral
- **Format**: CSV
- **Source**: `kaggle.com/datasets/sentiment140`
- **Perfect for**: Large-scale sentiment analysis

### 4. **SemEval 2018 Task 1** (Affect in Tweets)
- **Size**: 11,000 tweets
- **Task**: Emotion intensity detection
- **Format**: TSV
- **Source**: `competition.codalab.org/competitions/17751`
- **Perfect for**: Emotion intensity prediction

### 5. **CrowdFlower Emotion Dataset**
- **Size**: 40,000 tweets
- **Emotions**: 13 categories
- **Format**: CSV
- **Source**: `dataforgood.fb.com`, Kaggle
- **Perfect for**: Diverse emotion scenarios

---

## 📥 How to Download & Use

### Option 1: Hugging Face Emotion Dataset (Easiest)

1. **Download**:
   ```bash
   # Using Python
   pip install datasets
   python -c "from datasets import load_dataset; ds = load_dataset('dair-ai/emotion'); ds['train'].to_csv('emotions.csv', index=False)"
   ```

2. **Or download manually**:
   - Go to: https://huggingface.co/datasets/dair-ai/emotion
   - Click "Files and versions"
   - Download CSV files

3. **Place in**: `NeuroSync.Api/Data/emotions.csv`

4. **Format should be**: 
   ```csv
   text,label
   "I'm feeling great today!",joy
   "I'm so sad about this.",sadness
   ```

### Option 2: GoEmotions (Best Quality)

1. **Download from GitHub**:
   ```bash
   git clone https://github.com/google-research/google-research.git
   cd google-research/goemotions
   # Follow instructions in README
   ```

2. **Convert to CSV format**:
   - Use provided scripts or Python
   - Format: `text,emotion` (e.g., "text", "joy")

3. **Place in**: `NeuroSync.Api/Data/emotions.csv`

### Option 3: Kaggle Datasets

1. **Create Kaggle account**: kaggle.com
2. **Search**: "emotion detection dataset"
3. **Download**: CSV files
4. **Place in**: `NeuroSync.Api/Data/emotions.csv`

---

## 🔄 How Our System Uses Datasets

The `ModelService` in `NeuroSync.Api/Services/ModelService.cs` automatically:

1. **Checks for external dataset**: `NeuroSync.Api/Data/emotions.csv`
2. **If found**: Uses real data for training
3. **If not found**: Uses synthetic data from `TrainingDataGenerator`
4. **Training happens automatically** on first run

---

## 📝 Dataset Format Requirements

### Required CSV Format:
```csv
text,label
"I'm so happy!",happy
"I feel terrible.",sad
"This is frustrating!",frustrated
```

### Column Names (flexible):
- `text` or `Text` or `sentence` or `message`
- `label` or `Label` or `emotion` or `emotion_label`

### Emotion Labels Mapping:
Our system maps these labels to `EmotionType` enum:
- `happy`, `joy`, `happiness` → `Happy`
- `sad`, `sadness`, `sorrow` → `Sad`
- `angry`, `anger`, `rage` → `Angry`
- `anxious`, `anxiety`, `worry` → `Anxious`
- `calm`, `peaceful`, `relaxed` → `Calm`
- `excited`, `excitement`, `enthusiasm` → `Excited`
- `frustrated`, `frustration` → `Frustrated`
- `neutral`, `none` → `Neutral`

---

## ✅ Quick Start (Recommended)

1. **Download Hugging Face Emotion Dataset** (easiest):
   - Visit: https://huggingface.co/datasets/dair-ai/emotion
   - Download CSV
   - Rename to `emotions.csv`
   - Place in `NeuroSync.Api/Data/`

2. **Restart server**:
   ```bash
   dotnet run
   ```

3. **System will automatically**:
   - Detect the CSV file
   - Load real data
   - Train model with real emotions
   - Use real data instead of synthetic

4. **Result**: More accurate emotion detection! 🎉

---

## 🔗 Direct Download Links

- **Emotion Dataset (Hugging Face)**: https://huggingface.co/datasets/dair-ai/emotion
- **GoEmotions (GitHub)**: https://github.com/google-research/google-research/tree/master/goemotions
- **Kaggle Emotion Search**: https://www.kaggle.com/datasets?search=emotion
- **SemEval 2018**: https://competition.codalab.org/competitions/17751

---

## 💡 Tips

- **Start small**: Try Hugging Face Emotion Dataset first (20k examples)
- **Scale up**: Use GoEmotions for production (58k examples)
- **Combine datasets**: You can merge multiple CSV files
- **Keep format consistent**: Use same column names and emotion labels
- **Test with real data**: The system improves significantly with real-world data

---

## 🎯 Recommended for Production

1. **GoEmotions** - Best quality, diverse emotions
2. **Emotion Dataset (Hugging Face)** - Easy to use, good quality
3. **Combined**: Use both for maximum accuracy
