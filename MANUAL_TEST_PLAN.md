# 🧪 Manual Testing Plan

## Testing the System Manually

While automated tests run, let's also test the system manually:

### 1. Start the System
```bash
cd NeuroSync.Api
dotnet run
```

### 2. Test Scenarios

#### A. Emotion Detection Tests
1. **Happy Emotions** (20 tests)
   - "I'm so happy!"
   - "I'm feeling great!"
   - "This is wonderful!"
   - "I'm delighted!"
   - etc.

2. **Sad Emotions** (20 tests)
   - "I'm sad"
   - "I feel down"
   - "I'm depressed"
   - "I'm miserable"
   - etc.

3. **Angry Emotions** (15 tests)
   - "I'm angry"
   - "I'm furious"
   - "I'm mad"
   - etc.

4. **Anxious Emotions** (15 tests)
   - "I'm anxious"
   - "I'm worried"
   - "I'm nervous"
   - etc.

5. **All Other Emotions** (30 tests)
   - Calm, Excited, Frustrated, Neutral

#### B. Response Quality Tests
- Check if responses are warm and human-like
- Verify responses match emotions
- Test follow-up questions
- Check IoT actions are appropriate

#### C. Performance Tests
- Test response time (< 500ms per request)
- Test with 50+ requests in sequence
- Test with rapid requests

#### D. Edge Cases
- Empty strings
- Very long strings
- Special characters
- Mixed languages
- Null/undefined inputs

### 3. Expected Results

**Performance:**
- Response time: < 500ms per request
- 100 requests: < 30 seconds total
- System stability: No crashes

**Accuracy:**
- Emotion detection: > 70% accuracy on clear emotions
- Appropriate responses for each emotion
- Valid IoT actions

**Quality:**
- Warm, human-like responses
- Context-aware follow-up questions
- Smooth user experience

### 4. Test Results Log

Document results as you test:
- ✅ Passed tests
- ❌ Failed tests
- ⚠️ Issues found
- 📊 Performance metrics
