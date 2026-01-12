# NeuroSync - Emotion-Aware Intelligent System

## Overview

**NeuroSync** is a real-time emotion-aware intelligent system that detects human emotions from text input and adapts digital and IoT environments accordingly. The system uses ML.NET for emotion classification, SignalR for real-time communication, and simulates IoT device responses based on detected emotional states.

## Features

- **Text-based Emotion Detection**: Classifies emotions from textual input using ML.NET
- **Real-time Communication**: Uses SignalR for instant emotion data transmission
- **Adaptive Responses**: Generates context-aware responses based on detected emotions
- **IoT Integration**: Simulates smart device responses (lights, notifications) that adapt to emotional states
- **Modern Web Interface**: Beautiful, responsive UI for interacting with the system

## System Architecture

```
┌─────────────────┐
│   Web Frontend  │
│   (HTML/JS)     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  ASP.NET Core   │
│      API        │
│  (SignalR Hub)  │
└────────┬────────┘
         │
    ┌────┴────┐
    │         │
    ▼         ▼
┌────────┐ ┌──────────┐
│ ML.NET │ │ Decision │
│ Model  │ │ Engine   │
└────────┘ └────┬─────┘
                │
                ▼
         ┌──────────┐
         │   IoT    │
         │ Simulator│
         └──────────┘
```

## Technology Stack

- **.NET 8.0**: Framework
- **C#**: Programming language
- **ML.NET 5.0**: Machine learning framework
- **SignalR**: Real-time communication
- **ASP.NET Core**: Web framework
- **HTML/CSS/JavaScript**: Frontend

## Project Structure

```
NeuroSync/
├── NeuroSync.Api/          # Web API and SignalR hub
│   ├── Controllers/        # API controllers
│   ├── Hubs/              # SignalR hubs
│   ├── Services/          # Business logic services
│   └── wwwroot/           # Static files (frontend)
├── NeuroSync.Core/         # Core domain models
├── NeuroSync.ML/           # ML.NET models and training
└── NeuroSync.IoT/          # IoT device simulation
```

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Visual Studio 2022 or VS Code (recommended)

### Installation

1. **Clone or navigate to the project directory**
   ```bash
   cd "E:\human ai"
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run the application**
   ```bash
   cd NeuroSync.Api
   dotnet run
   ```

5. **Access the application**
   - Web Interface: `https://localhost:5001` or `http://localhost:5000`
   - Swagger API: `https://localhost:5001/swagger`

### Running from Visual Studio

1. Open `NeuroSync.sln` in Visual Studio
2. Set `NeuroSync.Api` as the startup project
3. Press F5 to run

## Usage

### Web Interface

1. Open the web interface in your browser
2. Type how you're feeling in the text area (e.g., "I'm feeling great today!" or "This is really frustrating")
3. Click "Detect Emotion" or press Enter
4. View the detected emotion, adaptive response, and IoT device actions in real-time

### API Endpoints

#### Detect Emotion
```http
POST /api/emotion/detect
Content-Type: application/json

{
  "text": "I'm feeling really happy today!",
  "userId": "optional-user-id"
}
```

**Response:**
```json
{
  "emotion": {
    "emotion": "Happy",
    "confidence": 0.85,
    "timestamp": "2026-01-03T02:30:00Z",
    "originalText": "I'm feeling really happy today!"
  },
  "adaptiveResponse": {
    "emotion": "Happy",
    "action": "enhance_environment",
    "message": "Great to see you're feeling happy!",
    "parameters": { ... },
    "timestamp": "2026-01-03T02:30:00Z"
  },
  "iotActions": [
    {
      "deviceId": "light-1",
      "actionType": "setColor",
      "parameters": {
        "color": "warm_yellow",
        "brightness": 80
      },
      "triggeredByEmotion": "Happy"
    }
  ]
}
```

#### Get Emotion Types
```http
GET /api/emotion/types
```

### SignalR Hub

The SignalR hub is available at `/emotionHub` and broadcasts the following events:

- **EmotionDetected**: Sent when an emotion is detected
- **AdaptiveResponse**: Sent when an adaptive response is generated
- **IoTAction**: Sent when IoT actions are triggered

## Emotion Types

The system can detect the following emotions:

- **Happy**: Positive, joyful feelings
- **Sad**: Negative, down feelings
- **Angry**: Frustrated, irritated feelings
- **Anxious**: Worried, stressed feelings
- **Calm**: Peaceful, relaxed feelings
- **Excited**: Enthusiastic, energetic feelings
- **Frustrated**: Annoyed, blocked feelings
- **Neutral**: Balanced, no strong emotion

## IoT Device Simulation

The system simulates IoT device responses based on emotions:

- **Lights**: Change color and brightness based on emotion
- **Notifications**: Show supportive messages for negative emotions
- **Breathing/Pulse Effects**: Provide calming or energizing light patterns

## Machine Learning Model

The ML model is trained using sample data included in the project. The model:

- Uses text featurization to convert text to numerical features
- Employs SDCA (Stochastic Dual Coordinate Ascent) for multiclass classification
- Supports 8 emotion categories
- Can be retrained with additional data

### Training Data

Sample training data is provided in `NeuroSync.ML/TrainingDataGenerator.cs`. You can extend this with more examples to improve accuracy.

### Model Storage

Trained models are stored in `NeuroSync.Api/Models/emotion-model.zip`. The model is automatically created on first run if it doesn't exist.

## Development

### Adding New Emotions

1. Update `EmotionType` enum in `NeuroSync.Core/EmotionType.cs`
2. Add training data in `TrainingDataGenerator.cs`
3. Update `DecisionEngine.cs` to handle the new emotion
4. Update `IoTDeviceSimulator.cs` for IoT responses
5. Retrain the model

### Extending IoT Devices

Modify `IoTDeviceSimulator.cs` to add new devices or actions:

```csharp
public List<IoTAction> ProcessEmotion(EmotionType emotion)
{
    // Add your custom logic here
}
```

## Testing

### Manual Testing

1. Run the application
2. Use the web interface to test various emotion inputs
3. Verify SignalR real-time updates
4. Check API responses using Swagger

### Sample Test Cases

- Happy: "I'm so happy today!"
- Sad: "I feel really down"
- Angry: "This makes me furious!"
- Anxious: "I'm feeling really worried"
- Calm: "I feel peaceful and relaxed"
- Excited: "I'm thrilled about this!"
- Frustrated: "This is so frustrating!"
- Neutral: "I see, that makes sense"

## Ethical Considerations

- No personal data is stored without consent
- Emotion data is processed in real-time and not persisted
- The system is designed for demonstration and research purposes
- Not intended for medical diagnosis or clinical use

## Future Enhancements

Potential improvements for future versions:

- Support for more emotion types
- Integration with real IoT devices
- User authentication and emotion history
- More sophisticated ML models (deep learning)
- Voice input support
- Multi-language support
- Emotion trend analysis
- Personalized adaptation based on user history

## Troubleshooting

### Model Not Loading

- Ensure the `Models` directory exists in the API project
- Check file permissions
- The model will be automatically created on first run

### SignalR Connection Issues

- Verify CORS settings in `Program.cs`
- Check that the hub URL matches in `app.js`
- Ensure HTTPS is properly configured

### Build Errors

- Run `dotnet restore` to restore packages
- Ensure .NET 8.0 SDK is installed
- Check that all project references are correct

## License

This project is provided as-is for educational and research purposes.

## Author

Developed as part of an academic project exploring emotion-aware intelligent systems.

## Acknowledgments

- ML.NET team for the machine learning framework
- ASP.NET Core team for the web framework
- SignalR team for real-time communication capabilities

---

**Note**: This is a prototype system designed for demonstration purposes. For production use, additional security, scalability, and reliability features would need to be implemented.


