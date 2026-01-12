// Facial Expression Detection using face-api.js
let isDetecting = false;
let detectionInterval = null;
let modelsLoaded = false;
let videoStream = null;

// Map face-api.js expressions to our emotion types
const expressionToEmotion = {
    'happy': 'happy',
    'sad': 'sad',
    'angry': 'angry',
    'fearful': 'anxious',
    'disgusted': 'frustrated',
    'surprised': 'excited',
    'neutral': 'neutral'
};

// Initialize face-api.js models
// Tries to load from local files first, falls back to internet
async function loadFaceAPIModels() {
    try {
        // Check if face-api is loaded
        if (typeof faceapi === 'undefined') {
            console.error('face-api.js is not loaded');
            return false;
        }
        
        // Try local models first (if downloaded)
        const LOCAL_MODEL_URL = '/models/face-api';
        console.log('Attempting to load face-api.js models from local files...');
        
        try {
            // Test if local models exist by trying to load one
            await faceapi.nets.tinyFaceDetector.loadFromUri(LOCAL_MODEL_URL);
            
            // If successful, load all from local
            console.log('Local models found! Loading from project folder...');
            await Promise.all([
                faceapi.nets.tinyFaceDetector.loadFromUri(LOCAL_MODEL_URL),
                faceapi.nets.faceLandmark68Net.loadFromUri(LOCAL_MODEL_URL),
                faceapi.nets.faceRecognitionNet.loadFromUri(LOCAL_MODEL_URL),
                faceapi.nets.faceExpressionNet.loadFromUri(LOCAL_MODEL_URL)
            ]);
            
            modelsLoaded = true;
            console.log('✅ Face-api.js models loaded successfully from LOCAL files');
            return true;
        } catch (localError) {
            console.log('Local models not found, loading from internet...');
            // Fall through to internet loading
        }
        
        // Load models from GitHub raw (fallback to internet)
        // These are the official model files from the face-api.js repository
        const MODEL_URL = 'https://raw.githubusercontent.com/justadudewhohacks/face-api.js/master/weights';
        
        try {
            await Promise.all([
                faceapi.nets.tinyFaceDetector.loadFromUri(MODEL_URL),
                faceapi.nets.faceLandmark68Net.loadFromUri(MODEL_URL),
                faceapi.nets.faceRecognitionNet.loadFromUri(MODEL_URL),
                faceapi.nets.faceExpressionNet.loadFromUri(MODEL_URL)
            ]);
            
            modelsLoaded = true;
            console.log('✅ Face-api.js models loaded successfully from internet');
            return true;
        } catch (error) {
            // Try alternative CDN if GitHub fails
            console.warn('GitHub source failed, trying alternative...');
            const ALT_MODEL_URL = 'https://cdn.jsdelivr.net/gh/justadudewhohacks/face-api.js@master/weights';
            
            try {
                await Promise.all([
                    faceapi.nets.tinyFaceDetector.loadFromUri(ALT_MODEL_URL),
                    faceapi.nets.faceLandmark68Net.loadFromUri(ALT_MODEL_URL),
                    faceapi.nets.faceRecognitionNet.loadFromUri(ALT_MODEL_URL),
                    faceapi.nets.faceExpressionNet.loadFromUri(ALT_MODEL_URL)
                ]);
                
                modelsLoaded = true;
                console.log('✅ Face-api.js models loaded successfully from alternative CDN');
                return true;
            } catch (altError) {
                console.error('Both model sources failed:', altError);
                throw altError;
            }
        }
    } catch (error) {
        console.error('Error loading face-api.js models:', error);
        alert('Failed to load facial detection models. Please check your internet connection.');
        return false;
    }
}

// Start facial expression detection
async function startFacialDetection() {
    const video = document.getElementById('videoElement');
    const canvas = document.getElementById('canvasElement');
    const placeholder = document.getElementById('cameraPlaceholder');
    const emotionDisplay = document.getElementById('facialEmotionDisplay');
    
    try {
        // Load models if not already loaded
        if (!modelsLoaded) {
            const loaded = await loadFaceAPIModels();
            if (!loaded) {
                return;
            }
        }
        
        // Request camera access
        videoStream = await navigator.mediaDevices.getUserMedia({ 
            video: { 
                width: 640, 
                height: 480,
                facingMode: 'user' 
            } 
        });
        
        video.srcObject = videoStream;
        video.style.display = 'block';
        placeholder.style.display = 'none';
        emotionDisplay.style.display = 'block';
        
        // Wait for video to be ready
        await new Promise((resolve) => {
            video.onloadedmetadata = () => {
                // Set canvas dimensions to match video
                canvas.width = video.videoWidth || 640;
                canvas.height = video.videoHeight || 480;
                canvas.style.display = 'block';
                resolve();
            };
            if (video.readyState >= 2) {
                canvas.width = video.videoWidth || 640;
                canvas.height = video.videoHeight || 480;
                canvas.style.display = 'block';
                resolve();
            }
        });
        
        isDetecting = true;
        
        // Reset emotion tracking for new session
        lastEmotionSent = null;
        lastEmotionTime = 0;
        
        // Show initial message
        updateFacialEmotionDisplay({
            emotion: 'neutral',
            confidence: 0,
            expression: 'neutral'
        });
        
        console.log('Real-time facial detection started - system will automatically respond to your emotions');
        
        // Reset emotion tracking for new session
        lastEmotionSent = null;
        lastEmotionTime = 0;
        
        // Show initial message
        updateFacialEmotionDisplay({
            emotion: 'neutral',
            confidence: 0,
            expression: 'neutral'
        });
        
        console.log('Real-time facial detection started - system will automatically respond to your emotions');
        
        // Start detection loop - more frequent for real-time response
        detectionInterval = setInterval(async () => {
            if (!isDetecting) return;
            
            try {
                await detectFacialExpression(video, canvas);
            } catch (error) {
                console.error('Error in facial detection:', error);
            }
        }, 300); // Detect every 300ms for more real-time response
        
        console.log('Facial detection started');
    } catch (error) {
        console.error('Error starting camera:', error);
        alert('Could not access camera. Please check permissions and try again.');
    }
}

// Detect facial expression from video frame
async function detectFacialExpression(video, canvas) {
    if (!modelsLoaded || !video || video.readyState !== 4) {
        return;
    }
    
    try {
        // Detect faces and expressions
        const detections = await faceapi
            .detectAllFaces(video, new faceapi.TinyFaceDetectorOptions())
            .withFaceLandmarks()
            .withFaceExpressions();
        
        if (detections.length === 0) {
            updateFacialEmotionDisplay(null);
            return;
        }
        
        // Get the first face (most prominent)
        const detection = detections[0];
        const expressions = detection.expressions;
        
        // Find the expression with highest confidence
        let maxExpression = null;
        let maxConfidence = 0;
        
        for (const [expression, confidence] of Object.entries(expressions)) {
            if (confidence > maxConfidence) {
                maxConfidence = confidence;
                maxExpression = expression;
            }
        }
        
        // Map to our emotion type
        const emotion = expressionToEmotion[maxExpression] || 'neutral';
        
        // Update display in real-time
        updateFacialEmotionDisplay({
            emotion: emotion,
            confidence: maxConfidence,
            expression: maxExpression
        });
        
        // Send to server for automatic comfort response
        // Lower threshold (0.5) for more responsive detection
        // System will automatically provide comfort without user typing
        if (maxConfidence > 0.5) {
            sendFacialEmotionToServer(emotion, maxConfidence);
        }
        
        // Draw on canvas (optional visualization)
        if (canvas && video) {
            const ctx = canvas.getContext('2d');
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            
            // Draw face detection box and landmarks
            const resizedDetections = faceapi.resizeResults(detection, {
                width: video.videoWidth || canvas.width,
                height: video.videoHeight || canvas.height
            });
            
            faceapi.draw.drawDetections(canvas, resizedDetections);
            faceapi.draw.drawFaceLandmarks(canvas, resizedDetections);
            faceapi.draw.drawFaceExpressions(canvas, resizedDetections);
        }
        
    } catch (error) {
        console.error('Error detecting facial expression:', error);
    }
}

// Update the facial emotion display
function updateFacialEmotionDisplay(result) {
    const badge = document.getElementById('facialEmotionBadge');
    const confidence = document.getElementById('facialConfidence');
    
    if (!result || result.confidence < 0.3) {
        badge.textContent = 'Looking for face...';
        badge.className = 'facial-emotion-badge neutral';
        confidence.textContent = 'Position your face in front of the camera';
        return;
    }
    
    const emotionName = result.emotion.charAt(0).toUpperCase() + result.emotion.slice(1);
    badge.textContent = emotionName;
    badge.className = `facial-emotion-badge ${result.emotion}`;
    
    const confidencePercent = (result.confidence * 100).toFixed(1);
    confidence.textContent = `Confidence: ${confidencePercent}%`;
    
    // Show real-time indicator for active detection
    if (isDetecting && result.confidence > 0.5) {
        confidence.textContent += ' • Real-time monitoring active';
    }
}

// Track last emotion sent to avoid spam
let lastEmotionSent = null;
let lastEmotionTime = 0;

// Send facial emotion to server for processing - real-time automatic response
async function sendFacialEmotionToServer(emotion, confidence) {
    const now = Date.now();
    
    // Only send if confidence is reasonable
    if (confidence < 0.5) {
        return;
    }
    
    // Smart throttling: 
    // - Same emotion: only send every 2 seconds (to avoid spam)
    // - Different emotion: send immediately (emotional change detected)
    // - Negative emotions (sad, anxious, angry): send more frequently (every 1.5 seconds)
    const negativeEmotions = ['sad', 'anxious', 'angry', 'frustrated'];
    const isNegative = negativeEmotions.includes(emotion);
    const isEmotionChange = lastEmotionSent !== emotion;
    
    let throttleTime = 2000; // Default: 2 seconds
    if (isNegative) {
        throttleTime = 1500; // Negative emotions: 1.5 seconds (more responsive)
    }
    if (isEmotionChange) {
        throttleTime = 500; // Emotion change: 500ms (immediate response)
    }
    
    // Check if we should send
    if (!isEmotionChange && (now - lastEmotionTime) < throttleTime) {
        return; // Same emotion, too soon
    }
    
    // Update tracking
    lastEmotionSent = emotion;
    lastEmotionTime = now;
    
    try {
        const userId = sessionStorage.getItem('neuroSync_userId') || 'default';
        const apiBaseUrl = window.API_BASE_URL || window.location.origin;
        
        console.log(`Real-time emotion detected: ${emotion} (${(confidence * 100).toFixed(1)}%) - Automatically sending for comfort response`);
        
        const response = await fetch(`${apiBaseUrl}/api/emotion/facial`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                emotion: emotion,
                confidence: confidence,
                userId: userId,
                source: 'facial_expression',
                realTime: true // Flag for real-time automatic response
            })
        });
        
        if (response.ok) {
            const data = await response.json();
            
            console.log('Real-time comfort response received:', data);
            
            // Always display adaptive response for real-time comfort
            if (data.adaptiveResponse && typeof displayAdaptiveResponse === 'function') {
                displayAdaptiveResponse(data.adaptiveResponse);
                
                // For negative emotions, make response more prominent
                const negativeEmotions = ['sad', 'anxious', 'angry', 'frustrated'];
                if (negativeEmotions.includes(emotion)) {
                    // Scroll to response to make it visible
                    const responseCard = document.getElementById('adaptiveResponse');
                    if (responseCard) {
                        responseCard.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    }
                }
            }
            
            // Always display IoT actions for real-time response
            if (data.iotActions && typeof displayIoTActions === 'function') {
                displayIoTActions(data.iotActions);
            }
            
            // Always show emotion result for real-time feedback
            if (data.emotion && typeof displayEmotionResult === 'function') {
                displayEmotionResult(data.emotion);
            }
        } else {
            const errorData = await response.json();
            console.error('Error sending facial emotion to server:', errorData);
        }
    } catch (error) {
        console.error('Error sending facial emotion to server:', error);
    }
}

// Stop facial detection
function stopFacialDetection() {
    isDetecting = false;
    
    if (detectionInterval) {
        clearInterval(detectionInterval);
        detectionInterval = null;
    }
    
    if (videoStream) {
        videoStream.getTracks().forEach(track => track.stop());
        videoStream = null;
    }
    
    const video = document.getElementById('videoElement');
    const canvas = document.getElementById('canvasElement');
    const placeholder = document.getElementById('cameraPlaceholder');
    const emotionDisplay = document.getElementById('facialEmotionDisplay');
    
    if (video) {
        video.srcObject = null;
        video.style.display = 'none';
    }
    
    if (canvas) {
        const ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        canvas.style.display = 'none';
    }
    
    placeholder.style.display = 'block';
    emotionDisplay.style.display = 'none';
    
    // Reset tracking
    lastEmotionSent = null;
    lastEmotionTime = 0;
    
    // Reset tracking
    lastEmotionSent = null;
    lastEmotionTime = 0;
    
    updateFacialEmotionDisplay(null);
    console.log('Facial detection stopped');
}

// Make functions globally available
window.startFacialDetection = startFacialDetection;
window.stopFacialDetection = stopFacialDetection;

// Load models when page loads
window.addEventListener('load', () => {
    // Wait for face-api.js to load, then preload models
    if (typeof faceapi !== 'undefined') {
        loadFaceAPIModels().catch(err => console.error('Failed to preload models:', err));
    } else {
        // Retry after a delay if face-api.js hasn't loaded yet
        setTimeout(() => {
            if (typeof faceapi !== 'undefined') {
                loadFaceAPIModels().catch(err => console.error('Failed to preload models:', err));
            } else {
                console.warn('face-api.js not loaded. Facial detection may not work.');
            }
        }, 1000);
    }
});

