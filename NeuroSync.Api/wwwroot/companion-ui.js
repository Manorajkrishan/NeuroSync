// NeuroSync Companion UI - Clean, Modern Interface

// Note: isRecording, mediaRecorder, and audioChunks are declared in app.js
// We'll use the global versions to avoid conflicts
let companionMediaRecorder = null;
let companionAudioChunks = [];
let currentUserId = sessionStorage.getItem('neuroSync_userId') || sessionStorage.getItem('userId') || 'default';

// IMPORTANT: Use lazy loading to get real functions when needed
// This prevents issues with script loading order
// Helper to safely get real function (with recursion guard)
let _callingToggleTTS = false;
let _callingStartFacialDetection = false;
let _callingStopFacialDetection = false;
let _callingToggleRecording = false;

// Store references to real functions (captured after scripts load)
// These will be set when the scripts finish loading
let _storedRealToggleTTS = null;
let _storedRealStartFacialDetection = null;
let _storedRealStopFacialDetection = null;
let _storedRealToggleRecording = null;

// Don't capture immediately - wait for scripts to load
// We'll capture them in the polling mechanism below

// Helper functions to get real functions (use stored reference first, then fallback to window)
function getRealToggleTTS() {
    // First try stored reference (captured before wrapper was defined)
    if (_storedRealToggleTTS && typeof _storedRealToggleTTS === 'function') {
        return _storedRealToggleTTS;
    }
    // Fallback: try window (but check it's not our wrapper)
    const ref = window.toggleTTS;
    if (ref && typeof ref === 'function' && ref !== toggleTTS) {
        _storedRealToggleTTS = ref; // Update stored reference
        return ref;
    }
    return null;
}

function getRealStartFacialDetection() {
    // First try stored reference (captured before wrapper was defined)
    if (_storedRealStartFacialDetection && typeof _storedRealStartFacialDetection === 'function') {
        return _storedRealStartFacialDetection;
    }
    // Fallback: try window (but check it's not our wrapper)
    const ref = window.startFacialDetection;
    if (ref && typeof ref === 'function' && ref !== startFacialDetection) {
        _storedRealStartFacialDetection = ref; // Update stored reference
        return ref;
    }
    return null;
}

function getRealStopFacialDetection() {
    // First try stored reference (captured before wrapper was defined)
    if (_storedRealStopFacialDetection && typeof _storedRealStopFacialDetection === 'function') {
        return _storedRealStopFacialDetection;
    }
    // Fallback: try window (but check it's not our wrapper)
    const ref = window.stopFacialDetection;
    if (ref && typeof ref === 'function' && ref !== stopFacialDetection) {
        _storedRealStopFacialDetection = ref; // Update stored reference
        return ref;
    }
    return null;
}

// Poll for real functions and capture them (scripts load before us)
// We need to identify the REAL functions, not our wrappers
if (typeof window !== 'undefined') {
    let pollCount = 0;
    const maxPolls = 50; // Increased to wait longer
    const pollInterval = setInterval(() => {
        pollCount++;
        if (pollCount > maxPolls) {
            clearInterval(pollInterval);
            // Silently complete - functions may not be needed (optional features)
            return;
        }
        
        // Capture real functions - but only if they're NOT our wrappers
        // We can identify our wrappers by checking if they have the recursion guard variables
        if (!_storedRealToggleTTS && window.toggleTTS && typeof window.toggleTTS === 'function') {
            // Check if it's NOT our wrapper (our wrapper will be defined later, so this should be the real one)
            // Actually, we can't check this easily, so we'll capture it before our wrapper is defined
            // But since scripts load before us, window.toggleTTS should be the real one
            _storedRealToggleTTS = window.toggleTTS;
            console.log('companion-ui: Captured real toggleTTS');
        }
        if (!_storedRealStartFacialDetection && window.startFacialDetection && typeof window.startFacialDetection === 'function') {
            // Check if it's the real function from facial-detection.js
            // The real function will have been set by facial-detection.js before companion-ui.js loads
            // So if it exists now, it should be the real one (our wrapper isn't defined yet)
            _storedRealStartFacialDetection = window.startFacialDetection;
            console.log('companion-ui: Captured real startFacialDetection');
        }
        if (!_storedRealStopFacialDetection && window.stopFacialDetection && typeof window.stopFacialDetection === 'function') {
            _storedRealStopFacialDetection = window.stopFacialDetection;
            console.log('companion-ui: Captured real stopFacialDetection');
        }
        if (!_storedRealToggleRecording && window.toggleRecording && typeof window.toggleRecording === 'function') {
            _storedRealToggleRecording = window.toggleRecording;
            console.log('companion-ui: Captured real toggleRecording');
        }
        
        // Stop polling once we have all references (or timeout)
    }, 50); // Check more frequently
}

// Mark our wrapper functions so we can identify them
// This helps prevent calling ourselves
let _isCompanionUIWrapper = true;

// Initialize companion UI
if (typeof window !== 'undefined') {
    window.addEventListener('load', function() {
        window.companionUIInitialized = true; // Mark as initialized
        
        if (typeof initializeCompanionUI === 'function') {
            initializeCompanionUI();
        }
        
        // Wait for app.js to initialize SignalR first
        // Don't call initializeSignalR here - let app.js handle it
        // Just set up handlers once connection is ready
        let retryCount = 0;
        const maxRetries = 10;
        
        const checkSignalRConnection = () => {
            if (window.connection) {
                const state = window.connection.state;
                console.log('companion-ui: SignalR connection found, state:', state);
                
                // Check if connection is in a valid state (not just undefined)
                if (state !== undefined && (state === signalR.HubConnectionState.Connected || 
                    state === signalR.HubConnectionState.Connecting || 
                    state === signalR.HubConnectionState.Reconnecting)) {
                    console.log('companion-ui: SignalR connection is active, setting up handlers');
                    setupSignalRHandlers();
                    return;
                } else if (state === signalR.HubConnectionState.Disconnected) {
                    console.log('companion-ui: SignalR connection exists but is disconnected, waiting...');
                }
            } else {
                console.log('companion-ui: window.connection not found, retry', retryCount + 1, 'of', maxRetries);
            }
            
            if (retryCount < maxRetries) {
                retryCount++;
                setTimeout(checkSignalRConnection, 500);
            } else {
                console.warn('companion-ui: SignalR connection not available after max retries');
                console.warn('companion-ui: window.connection =', window.connection);
                console.warn('companion-ui: Check if app.js initialized SignalR properly');
            }
        };
        
        // Start checking after a short delay
        setTimeout(checkSignalRConnection, 500);
        
        // Load data functions - with guards to prevent recursion
        setTimeout(() => {
            if (typeof loadVoiceNotes === 'function') {
                loadVoiceNotes();
            }
            if (typeof loadClonedVoices === 'function') {
                loadClonedVoices();
            }
            if (typeof initMultiLayerEmotion === 'function') {
                initMultiLayerEmotion();
            }
        }, 300);
        
        // Auto-start corner camera (wait for scripts to load)
        setTimeout(() => {
            if (typeof startCornerCamera === 'function') {
                startCornerCamera();
            }
        }, 1500);
        
        if (typeof setupInputHandlers === 'function') {
            setupInputHandlers();
        }
    });
}

function initializeCompanionUI() {
    // Set up message input
    const messageInput = document.getElementById('messageInput');
    if (messageInput) {
        messageInput.addEventListener('focus', () => {
            document.getElementById('quickEmotions')?.style.setProperty('display', 'flex');
        });
        
        // Set up keydown handler
        messageInput.addEventListener('keydown', function(event) {
            handleInputKeydown(event);
        });
        
        // Set up input handler for auto-resize
        messageInput.addEventListener('input', function() {
            autoResizeTextarea(this);
        });
    }
    
    // Set up send button
    const sendBtn = document.getElementById('sendBtn');
    if (sendBtn) {
        sendBtn.addEventListener('click', function() {
            sendMessage();
        });
    }
    
    // Set up voice button - use real function from app.js directly
    const voiceBtn = document.getElementById('voiceBtn');
    if (voiceBtn) {
        voiceBtn.addEventListener('click', function() {
            // Use real function from app.js directly (it's exposed on window)
            if (typeof window.toggleRecording === 'function') {
                window.toggleRecording();
            } else {
                console.warn('toggleRecording not available yet');
            }
        });
    }
    
    // Set up settings button
    const settingsBtn = document.getElementById('settingsBtn');
    if (settingsBtn) {
        settingsBtn.addEventListener('click', function() {
            toggleSettings();
        });
    }
    
    // Camera button removed - facial detection is always-on now
    
    // Set up close sidebar button
    const closeSidebarBtn = document.getElementById('closeSidebarBtn');
    if (closeSidebarBtn) {
        closeSidebarBtn.addEventListener('click', function() {
            closeSidebar();
        });
    }
    
    // Set up close camera button
    const closeCameraBtn = document.getElementById('closeCameraBtn');
    if (closeCameraBtn) {
        closeCameraBtn.addEventListener('click', function() {
            toggleCamera();
        });
    }
    
    // Set up overlay click
    const overlay = document.getElementById('overlay');
    if (overlay) {
        overlay.addEventListener('click', function() {
            closeSidebar();
            toggleCamera();
        });
    }
    
    // Set up quick emotion buttons
    const quickEmotionBtns = document.querySelectorAll('.emotion-quick-btn');
    quickEmotionBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            const emotion = this.getAttribute('data-emotion');
            if (emotion) {
                quickEmotion(emotion);
            }
        });
    });
    
    // Set up TTS toggle
    const ttsToggle = document.getElementById('ttsToggle');
    if (ttsToggle) {
        ttsToggle.addEventListener('change', function() {
            toggleTTS();
        });
    }
    
    // Set up voice clone file input
    const voiceCloneFile = document.getElementById('voiceCloneFile');
    const uploadVoiceBtn = document.getElementById('uploadVoiceBtn');
    if (voiceCloneFile && uploadVoiceBtn) {
        uploadVoiceBtn.addEventListener('click', function() {
            voiceCloneFile.click();
        });
        voiceCloneFile.addEventListener('change', function(event) {
            handleVoiceCloneFile(event);
        });
    }
    
    // Set up consent checkboxes
    const consentEmotionSensing = document.getElementById('consentEmotionSensing');
    if (consentEmotionSensing) {
        consentEmotionSensing.addEventListener('change', function() {
            toggleConsent('emotionSensing');
        });
    }
    
    const consentVisualLayer = document.getElementById('consentVisualLayer');
    if (consentVisualLayer) {
        consentVisualLayer.addEventListener('change', function() {
            toggleConsent('visualLayer');
        });
    }
    
    const consentAudioLayer = document.getElementById('consentAudioLayer');
    if (consentAudioLayer) {
        consentAudioLayer.addEventListener('change', function() {
            toggleConsent('audioLayer');
        });
    }
    
        // Set up corner camera toggle
        const cornerCameraToggle = document.getElementById('cornerCameraToggle');
        if (cornerCameraToggle) {
            cornerCameraToggle.addEventListener('click', function() {
                toggleCornerCamera();
            });
        }

    // Update avatar based on emotion
    updateCompanionAvatar('neutral');
    
    // Initialize TTS
    if (typeof initTTS === 'function') {
        initTTS();
        updateTTSToggleButton();
    }
}

function setupInputHandlers() {
    // Additional setup if needed
}

// DO NOT define initializeSignalR here - it's defined in app.js
// If we define it here, it will overwrite the one in app.js!
// Just use setupSignalRHandlers() when connection is ready

function setupSignalRHandlers() {
    if (!window.connection) return;

    const connection = window.connection;

    // NOTE: We don't set up duplicate SignalR handlers here because app.js already handles them.
    // Setting up handlers here would cause duplicate messages.
    // The SignalR events are already handled in app.js, which calls the display functions.
    // We only need to ensure our display functions are available globally (which they are).
    
    // Only set up handlers that app.js doesn't handle
    if (!connection._companionUIHandlersSet) {
        connection.on("EthicalConsentUpdated", (consent) => {
            console.log('Consent updated:', consent);
        });

        connection._companionUIHandlersSet = true;
    }
}

// Expose setupSignalRHandlers globally
window.setupSignalRHandlers = setupSignalRHandlers;

// Message handling
function sendMessage() {
    const messageInput = document.getElementById('messageInput');
    const text = messageInput?.value.trim();
    
    if (!text) return;

    // Add user message to chat
    addChatMessage(text, 'user');

    // Clear input
    messageInput.value = '';
    autoResizeTextarea(messageInput);

    // Hide quick emotions
    document.getElementById('quickEmotions')?.style.setProperty('display', 'none');

    // Send to backend
    // Use global detectEmotion from app.js - it handles both API response and SignalR events
    // This prevents duplicate messages
    if (typeof window.detectEmotion === 'function') {
        window.detectEmotion(text);
    } else if (typeof detectEmotion === 'function') {
        detectEmotion(text);
    } else {
        // Fallback: direct API call (should not be reached if app.js loads correctly)
        console.warn('detectEmotion not available, using fallback API call');
        const apiBaseUrl = window.API_BASE_URL || API_BASE_URL || window.location.origin;
        fetch(`${apiBaseUrl}/api/emotion/detect`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ text, userId: currentUserId })
        })
        .then(res => res.json())
        .then(data => {
            // Only display if not already displayed by SignalR
            if (data.emotion) {
                displayEmotionResult(data.emotion);
            }
            if (data.adaptiveResponse) {
                displayAdaptiveResponse(data.adaptiveResponse);
            }
        })
        .catch(err => console.error('Error:', err));
    }
}

function quickEmotion(emotion) {
    const emotionTexts = {
        happy: "I'm feeling happy today!",
        sad: "I'm feeling sad...",
        anxious: "I'm feeling anxious...",
        calm: "I'm feeling calm and peaceful."
    };

    const messageInput = document.getElementById('messageInput');
    if (messageInput) {
        messageInput.value = emotionTexts[emotion] || `I'm feeling ${emotion}.`;
        sendMessage();
    }
}

// Helper function for direct API calls
function sendEmotionDetectionRequest(text, userId, apiBaseUrl) {
    fetch(`${apiBaseUrl}/api/emotion/detect`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ text, userId })
    })
    .then(res => res.json())
    .then(data => {
        if (data.emotion) {
            displayEmotionResult(data.emotion);
        }
        if (data.adaptiveResponse) {
            displayAdaptiveResponse(data.adaptiveResponse);
        }
        if (data.response) {
            displayAdaptiveResponse(data.response);
        }
    })
    .catch(err => console.error('Error:', err));
}

function addChatMessage(text, type) {
    const chatMessages = document.getElementById('chatMessages');
    if (!chatMessages) return;

    const messageDiv = document.createElement('div');
    messageDiv.className = `chat-message ${type}-message`;

    const avatar = document.createElement('div');
    avatar.className = 'message-avatar';
    avatar.textContent = type === 'user' ? '👤' : '👶';

    const bubble = document.createElement('div');
    bubble.className = `message-bubble ${type}-message`;

    const textDiv = document.createElement('div');
    textDiv.className = 'message-text';
    textDiv.textContent = text;

    const timeDiv = document.createElement('div');
    timeDiv.className = 'message-time';
    timeDiv.textContent = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    bubble.appendChild(textDiv);
    bubble.appendChild(timeDiv);
    messageDiv.appendChild(avatar);
    messageDiv.appendChild(bubble);
    chatMessages.appendChild(messageDiv);

    // Scroll to bottom
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

// Helper function to convert emotion (number or string) to lowercase string
function getEmotionString(emotion) {
    if (emotion === null || emotion === undefined) {
        return 'neutral';
    }
    
    // If it's already a string, return lowercase
    if (typeof emotion === 'string') {
        return emotion.toLowerCase();
    }
    
    // If it's a number (enum), map it to string
    if (typeof emotion === 'number') {
        const emotionMap = {
            0: 'happy',
            1: 'sad',
            2: 'angry',
            3: 'anxious',
            4: 'calm',
            5: 'excited',
            6: 'frustrated',
            7: 'neutral'
        };
        return emotionMap[emotion] || 'neutral';
    }
    
    // Fallback
    return 'neutral';
}

// Track last displayed emotion result to prevent duplicates
let _lastEmotionResult = null;
let _lastEmotionTime = 0;

function displayEmotionResult(result) {
    if (!result || result.emotion === undefined) return;

    // Deduplication: Don't display the same emotion result within 2 seconds
    const now = Date.now();
    const resultKey = `${result.emotion}_${result.originalText || ''}_${Math.round(result.confidence * 100)}`;
    
    if (_lastEmotionResult === resultKey && (now - _lastEmotionTime) < 2000) {
        console.log('Skipping duplicate emotion result:', result.emotion);
        return;
    }
    
    _lastEmotionResult = resultKey;
    _lastEmotionTime = now;

    const emotion = getEmotionString(result.emotion);
    updateCompanionAvatar(emotion);
    
    // Update body background based on emotion
    document.body.className = `emotion-${emotion}`;

    // Add AI response message
    if (result.confidence) {
        const message = `I sense you're feeling ${emotion} (${Math.round(result.confidence * 100)}% confidence).`;
        addChatMessage(message, 'ai');
    }
}

// Track last displayed messages to prevent duplicates (use a Set to track multiple recent messages)
let _recentMessages = new Set();
let _messageTimestamps = new Map();

function displayAdaptiveResponse(response) {
    if (!response || !response.message) return;

    // Deduplication: Don't display the same message within 3 seconds
    const now = Date.now();
    const messageKey = response.message; // Use just the message text as key
    
    // Check if this exact message was displayed recently
    if (_recentMessages.has(messageKey)) {
        const lastTime = _messageTimestamps.get(messageKey);
        if (lastTime && (now - lastTime) < 3000) {
            console.log('Skipping duplicate adaptive response:', response.message);
            return;
        }
    }
    
    // Clean up old messages (older than 5 seconds)
    _messageTimestamps.forEach((time, msg) => {
        if (now - time > 5000) {
            _recentMessages.delete(msg);
            _messageTimestamps.delete(msg);
        }
    });
    
    // Track this message
    _recentMessages.add(messageKey);
    _messageTimestamps.set(messageKey, now);

    // Add AI response to chat
    addChatMessage(response.message, 'ai');

    // Update avatar if emotion changed
    if (response.emotion !== undefined) {
        const emotion = getEmotionString(response.emotion);
        updateCompanionAvatar(emotion);
    }

    // Speak if TTS is enabled
    if (typeof speakText === 'function' && typeof isTTSEnabled === 'function' && isTTSEnabled()) {
        speakText(response.message);
        updateCompanionAvatarSpeaking(true);
        setTimeout(() => updateCompanionAvatarSpeaking(false), response.message.length * 50); // Estimate speaking time
    }
}

function displayMultiLayerResult(result) {
    if (!result) return;

    const message = `Multi-layer emotion detected: ${result.primaryEmotion} (${Math.round(result.overallConfidence * 100)}% confidence)`;
    addChatMessage(message, 'ai');
    updateCompanionAvatar(result.primaryEmotion?.toLowerCase() || 'neutral');
}

// Avatar management
function updateCompanionAvatar(emotion) {
    const avatar = document.getElementById('companionAvatar');
    if (!avatar) return;

    // Remove all emotion classes
    avatar.className = 'companion-avatar';
    avatar.classList.add(emotion || 'neutral');

    // Update emoji
    const emojiMap = {
        happy: '😊',
        sad: '😢',
        calm: '😌',
        excited: '😄',
        anxious: '😰',
        angry: '😠',
        frustrated: '😤',
        neutral: '👶'
    };

    const emoji = emojiMap[emotion] || '👶';
    const emojiDiv = avatar.querySelector('.avatar-emoji');
    if (emojiDiv) {
        emojiDiv.textContent = emoji;
    }
}

function updateCompanionAvatarSpeaking(isSpeaking) {
    const avatar = document.getElementById('companionAvatar');
    if (!avatar) return;

    if (isSpeaking) {
        avatar.classList.add('speaking');
    } else {
        avatar.classList.remove('speaking');
    }
}

// Sidebar management
function toggleSettings() {
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('overlay');
    
    if (sidebar && overlay) {
        const isVisible = sidebar.style.display !== 'none';
        sidebar.style.display = isVisible ? 'none' : 'flex';
        overlay.style.display = isVisible ? 'none' : 'block';
    }
}

function closeSidebar() {
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('overlay');
    
    if (sidebar) sidebar.style.display = 'none';
    if (overlay) overlay.style.display = 'none';
}

// Always-on Corner Camera Management
let cornerCameraActive = false;
let cornerVideoStream = null;

async function startCornerCamera() {
    if (cornerCameraActive) return;
    
    const cornerVideo = document.getElementById('cornerVideoElement');
    const cornerCanvas = document.getElementById('cornerCanvasElement');
    const cornerEmotionBadge = document.getElementById('cornerEmotionBadge');
    const cornerEmotionConfidence = document.getElementById('cornerEmotionConfidence');
    
    if (!cornerVideo) {
        console.warn('Corner camera elements not found');
        return;
    }
    
    try {
        // Request camera access
        cornerVideoStream = await navigator.mediaDevices.getUserMedia({ 
            video: { 
                width: 200, 
                height: 150,
                facingMode: 'user' 
            } 
        });
        
        cornerVideo.srcObject = cornerVideoStream;
        cornerCameraActive = true;
        
        // Wait for video to be ready before starting detection
        await new Promise((resolve) => {
            const onReady = () => {
                if (cornerVideo.readyState >= 2) { // HAVE_CURRENT_DATA or higher
                    cornerVideo.removeEventListener('loadedmetadata', onReady);
                    cornerVideo.removeEventListener('canplay', onReady);
                    resolve();
                }
            };
            
            cornerVideo.addEventListener('loadedmetadata', onReady);
            cornerVideo.addEventListener('canplay', onReady);
            
            // Check if already ready
            if (cornerVideo.readyState >= 2) {
                cornerVideo.removeEventListener('loadedmetadata', onReady);
                cornerVideo.removeEventListener('canplay', onReady);
                resolve();
            }
        });
        
        // Start facial detection for corner camera (uses its own detection loop)
        startCornerFacialDetection(cornerVideo, cornerCanvas, cornerEmotionBadge, cornerEmotionConfidence);
        
        console.log('✅ Corner camera started - always-on facial detection active');
        console.log('Video readyState:', cornerVideo.readyState, 'videoWidth:', cornerVideo.videoWidth, 'videoHeight:', cornerVideo.videoHeight);
    } catch (error) {
        console.error('Error starting corner camera:', error);
        console.error('Error details:', error.message, error.stack);
        if (cornerEmotionBadge) {
            cornerEmotionBadge.textContent = 'Camera Error';
        }
        if (cornerEmotionConfidence) {
            cornerEmotionConfidence.textContent = error.message || 'Check permissions';
        }
    }
}

// Corner camera toggle
function toggleCornerCamera() {
    const cornerCamera = document.getElementById('cornerCamera');
    if (cornerCamera) {
        cornerCamera.classList.toggle('collapsed');
    }
}

// Start facial detection for corner camera (simplified version)
async function startCornerFacialDetection(video, canvas, badge, confidence) {
    // Check if face-api is loaded
    if (typeof faceapi === 'undefined') {
        console.warn('face-api.js not loaded');
        return;
    }
    
    // Load models if needed (check if already loaded from facial-detection.js)
    if (!window.faceApiModelsLoaded) {
        try {
            console.log('Loading face-api models for corner camera...');
            await Promise.all([
                faceapi.nets.tinyFaceDetector.loadFromUri('https://raw.githubusercontent.com/justadudewhohacks/face-api.js/master/weights'),
                faceapi.nets.faceLandmark68Net.loadFromUri('https://raw.githubusercontent.com/justadudewhohacks/face-api.js/master/weights'),
                faceapi.nets.faceExpressionNet.loadFromUri('https://raw.githubusercontent.com/justadudewhohacks/face-api.js/master/weights')
            ]);
            window.faceApiModelsLoaded = true;
            console.log('✅ Face-api models loaded for corner camera');
        } catch (error) {
            console.error('Error loading face-api models:', error);
            if (badge) badge.textContent = 'Model Error';
            if (confidence) confidence.textContent = 'Check internet';
            return;
        }
    } else {
        console.log('Face-api models already loaded, reusing...');
    }
    
    // Wait for video dimensions to be available
    if (video.videoWidth === 0 || video.videoHeight === 0) {
        await new Promise((resolve) => {
            const onLoadedMetadata = () => {
                video.removeEventListener('loadedmetadata', onLoadedMetadata);
                resolve();
            };
            video.addEventListener('loadedmetadata', onLoadedMetadata);
            // If already loaded, resolve immediately
            if (video.readyState >= 1) {
                video.removeEventListener('loadedmetadata', onLoadedMetadata);
                resolve();
            }
        });
    }
    
    // Set canvas size
    canvas.width = video.videoWidth || 200;
    canvas.height = video.videoHeight || 150;
    
    // Detection loop
    const detectLoop = async () => {
        if (!cornerCameraActive) return;
        
        // Check if video is ready (has enough data for detection)
        if (!video || video.readyState < 2) {
            // Video not ready yet, wait a bit and try again
            if (cornerCameraActive) {
                setTimeout(detectLoop, 200);
            }
            return;
        }
        
        try {
            const detections = await faceapi
                .detectAllFaces(video, new faceapi.TinyFaceDetectorOptions())
                .withFaceLandmarks()
                .withFaceExpressions();
            
            if (detections.length > 0) {
                const detection = detections[0];
                const expressions = detection.expressions;
                
                let maxExpression = null;
                let maxConfidence = 0;
                
                for (const [expression, conf] of Object.entries(expressions)) {
                    if (conf > maxConfidence) {
                        maxConfidence = conf;
                        maxExpression = expression;
                    }
                }
                
                // Map to emotion
                const emotionMap = {
                    'happy': 'Happy',
                    'sad': 'Sad',
                    'angry': 'Angry',
                    'fearful': 'Anxious',
                    'disgusted': 'Frustrated',
                    'surprised': 'Excited',
                    'neutral': 'Neutral'
                };
                
                const emotion = emotionMap[maxExpression] || 'Neutral';
                
                if (badge) badge.textContent = emotion;
                if (confidence) confidence.textContent = `${Math.round(maxConfidence * 100)}%`;
                
                // Send to server for processing (only if confidence is high enough)
                // Higher threshold to avoid false positives
                if (maxConfidence > 0.7) {
                    sendCornerEmotionToServer(emotion.toLowerCase(), maxConfidence);
                }
            } else {
                if (badge) badge.textContent = 'No Face';
                if (confidence) confidence.textContent = '--';
            }
        } catch (error) {
            console.error('Error in corner facial detection:', error);
            // Don't stop the loop on error, just log it
            if (badge) badge.textContent = 'Error';
            if (confidence) confidence.textContent = 'Retrying...';
        }
        
        // Continue loop
        if (cornerCameraActive) {
            setTimeout(detectLoop, 500); // Detect every 500ms
        }
    };
    
    detectLoop();
}

// Track last emotion sent to avoid spam
let lastCornerEmotionSent = null;
let lastCornerEmotionTime = 0;

async function sendCornerEmotionToServer(emotion, confidence) {
    const now = Date.now();
    
    // Smart throttling: 
    // - Same emotion: only send every 5 seconds (reduced frequency)
    // - Different emotion: send immediately (emotional change detected)
    // - Negative emotions: send more frequently (every 4 seconds)
    // - Neutral: send less frequently (every 8 seconds) to avoid spam
    const negativeEmotions = ['sad', 'anxious', 'angry', 'frustrated'];
    const isNegative = negativeEmotions.includes(emotion);
    const isNeutral = emotion === 'neutral';
    const isEmotionChange = lastCornerEmotionSent !== emotion;
    
    let throttleTime = 5000; // Default: 5 seconds
    if (isNeutral) {
        throttleTime = 8000; // Neutral: 8 seconds (less frequent)
    } else if (isNegative) {
        throttleTime = 4000; // Negative emotions: 4 seconds (more responsive)
    }
    if (isEmotionChange) {
        throttleTime = 2000; // Emotion change: 2 seconds (immediate response)
    }
    
    // Check if we should send
    if (!isEmotionChange && (now - lastCornerEmotionTime) < throttleTime) {
        return; // Same emotion, too soon
    }
    
    lastCornerEmotionSent = emotion;
    lastCornerEmotionTime = now;
    
    try {
        const userId = sessionStorage.getItem('neuroSync_userId') || 'default';
        const apiBaseUrl = window.API_BASE_URL || window.location.origin;
        
        const response = await fetch(`${apiBaseUrl}/api/emotion/facial`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                emotion: emotion,
                confidence: confidence,
                userId: userId,
                source: 'corner_camera',
                realTime: true
            })
        });
        
        if (response.ok) {
            const data = await response.json();
            
            // Display adaptive response if available
            if (data.adaptiveResponse && typeof displayAdaptiveResponse === 'function') {
                displayAdaptiveResponse(data.adaptiveResponse);
            }
        }
    } catch (error) {
        console.error('Error sending corner emotion to server:', error);
    }
}

// Stop corner camera
function stopCornerCamera() {
    cornerCameraActive = false;
    if (cornerVideoStream) {
        cornerVideoStream.getTracks().forEach(track => track.stop());
        cornerVideoStream = null;
    }
    const cornerVideo = document.getElementById('cornerVideoElement');
    if (cornerVideo) {
        cornerVideo.srcObject = null;
    }
}

// Camera management (old overlay - kept for compatibility but not used)
function toggleCamera() {
    // Old camera overlay - not used anymore, but keep for compatibility
    const cameraOverlay = document.getElementById('cameraOverlay');
    const overlay = document.getElementById('overlay');
    
    if (cameraOverlay && overlay) {
        const isVisible = cameraOverlay.style.display !== 'none';
        cameraOverlay.style.display = isVisible ? 'none' : 'flex';
        overlay.style.display = isVisible ? 'none' : 'block';
    }
}

// Input helpers
function handleInputKeydown(event) {
    if (event.key === 'Enter' && !event.shiftKey) {
        event.preventDefault();
        sendMessage();
    }
}

function autoResizeTextarea(textarea) {
    textarea.style.height = 'auto';
    textarea.style.height = Math.min(textarea.scrollHeight, 120) + 'px';
}

// Voice recording - use real function from app.js directly
// Don't create a wrapper - just use the real function directly
// This prevents recursion issues

function startRecording() {
    // Implementation from app.js
    if (typeof navigator.mediaDevices === 'undefined' || !navigator.mediaDevices.getUserMedia) {
        alert('Voice recording not supported in your browser');
        return;
    }

    navigator.mediaDevices.getUserMedia({ audio: true })
        .then(stream => {
            companionMediaRecorder = new MediaRecorder(stream);
            companionAudioChunks = [];

            companionMediaRecorder.ondataavailable = event => {
                companionAudioChunks.push(event.data);
            };

            companionMediaRecorder.onstop = () => {
                const audioBlob = new Blob(companionAudioChunks, { type: 'audio/webm' });
                handleRecordedAudio(audioBlob);
                stream.getTracks().forEach(track => track.stop());
            };

            companionMediaRecorder.start();
            document.getElementById('recordingStatus')?.style.setProperty('display', 'flex');
        })
        .catch(err => {
            console.error('Error accessing microphone:', err);
            alert('Could not access microphone. Please check permissions.');
        });
}

function stopRecording() {
    if (companionMediaRecorder && companionMediaRecorder.state === 'recording') {
        companionMediaRecorder.stop();
        document.getElementById('recordingStatus')?.style.setProperty('display', 'none');
    }
}

function handleRecordedAudio(audioBlob) {
    // Save voice note (implementation from app.js)
    if (typeof saveVoiceNote === 'function') {
        saveVoiceNote(audioBlob);
    }
}

// Voice cloning (reuse from voice-cloning.js)
function handleVoiceCloneFile(event) {
    if (typeof window.handleVoiceCloneFile === 'function') {
        window.handleVoiceCloneFile(event);
    }
}

// Consent management (reuse from multilayer-emotion.js)
function toggleConsent(type) {
    const externalToggleConsent = window.toggleConsent;
    if (typeof externalToggleConsent === 'function' && externalToggleConsent !== toggleConsent) {
        externalToggleConsent(type);
        return;
    }
    // Fallback: do nothing if external function doesn't exist
    console.log('toggleConsent: No external function found');
}

// TTS toggle (reuse from app.js)
// Use lazy loading to get real function when needed
function toggleTTS() {
    // Prevent recursion
    if (_callingToggleTTS) {
        console.warn('toggleTTS: Recursion detected, aborting');
        return;
    }
    
    // Get real function from window (lazy loading)
    const realFunction = getRealToggleTTS();
    
    // Check if we got our own wrapper (shouldn't happen, but be safe)
    if (realFunction === toggleTTS) {
        console.warn('toggleTTS: Got our own wrapper, this should not happen');
        return;
    }
    
    if (realFunction) {
        _callingToggleTTS = true;
        try {
            realFunction();
        } finally {
            _callingToggleTTS = false;
        }
    } else {
        console.warn('toggleTTS: app.js function not available. Make sure app.js is loaded.');
    }
}

// Facial detection (reuse from facial-detection.js)
// Use lazy loading to get real function when needed
function startFacialDetection() {
    // Prevent recursion
    if (_callingStartFacialDetection) {
        console.warn('startFacialDetection: Recursion detected, aborting');
        return;
    }
    
    // Get real function from window (lazy loading)
    const realFunction = getRealStartFacialDetection();
    
    // Check if we got our own wrapper (shouldn't happen, but be safe)
    if (realFunction === startFacialDetection) {
        console.warn('startFacialDetection: Got our own wrapper, this should not happen');
        console.warn('startFacialDetection: window.startFacialDetection =', window.startFacialDetection);
        console.warn('startFacialDetection: _storedRealStartFacialDetection =', _storedRealStartFacialDetection);
        // Try to get it directly from window one more time
        const directRef = window.startFacialDetection;
        if (directRef && typeof directRef === 'function' && directRef !== startFacialDetection) {
            console.log('startFacialDetection: Found real function on retry');
            _storedRealStartFacialDetection = directRef;
            _callingStartFacialDetection = true;
            try {
                directRef();
                document.getElementById('startCameraBtn')?.style.setProperty('display', 'none');
                document.getElementById('stopCameraBtn')?.style.setProperty('display', 'block');
                document.getElementById('facialEmotionDisplay')?.style.setProperty('display', 'block');
            } catch (error) {
                console.error('Error calling startFacialDetection on retry:', error);
            } finally {
                _callingStartFacialDetection = false;
            }
            return;
        }
        return;
    }
    
    if (realFunction) {
        _callingStartFacialDetection = true;
        try {
            realFunction();
            // Update UI elements
            document.getElementById('startCameraBtn')?.style.setProperty('display', 'none');
            document.getElementById('stopCameraBtn')?.style.setProperty('display', 'block');
            document.getElementById('facialEmotionDisplay')?.style.setProperty('display', 'block');
        } catch (error) {
            console.error('Error calling startFacialDetection:', error);
        } finally {
            _callingStartFacialDetection = false;
        }
    } else {
        console.warn('startFacialDetection: facial-detection.js function not available. Make sure facial-detection.js is loaded and has exposed startFacialDetection to window.');
        // Try to wait a bit and retry (in case script is still loading)
        setTimeout(() => {
            const retryFunction = getRealStartFacialDetection();
            if (retryFunction && retryFunction !== startFacialDetection) {
                console.log('startFacialDetection: Found function after retry, calling it');
                _callingStartFacialDetection = true;
                try {
                    retryFunction();
                    document.getElementById('startCameraBtn')?.style.setProperty('display', 'none');
                    document.getElementById('stopCameraBtn')?.style.setProperty('display', 'block');
                    document.getElementById('facialEmotionDisplay')?.style.setProperty('display', 'block');
                } catch (error) {
                    console.error('Error calling startFacialDetection on retry:', error);
                } finally {
                    _callingStartFacialDetection = false;
                }
            }
        }, 500);
    }
}

function stopFacialDetection() {
    // Prevent recursion
    if (_callingStopFacialDetection) {
        console.warn('stopFacialDetection: Recursion detected, aborting');
        return;
    }
    
    // Get real function from window (lazy loading)
    const realFunction = getRealStopFacialDetection();
    
    // Check if we got our own wrapper (shouldn't happen, but be safe)
    if (realFunction === stopFacialDetection) {
        console.warn('stopFacialDetection: Got our own wrapper, this should not happen');
        return;
    }
    
    if (realFunction) {
        _callingStopFacialDetection = true;
        try {
            realFunction();
            document.getElementById('startCameraBtn')?.style.setProperty('display', 'block');
            document.getElementById('stopCameraBtn')?.style.setProperty('display', 'none');
            document.getElementById('facialEmotionDisplay')?.style.setProperty('display', 'none');
        } finally {
            _callingStopFacialDetection = false;
        }
    } else {
        console.warn('stopFacialDetection: facial-detection.js function not available. Make sure facial-detection.js is loaded.');
    }
}

// Voice notes (reuse from app.js)
function loadVoiceNotes() {
    // Check if app.js has the function and it's not this one (avoid recursion)
    const appLoadVoiceNotes = window.loadVoiceNotes;
    if (typeof appLoadVoiceNotes === 'function' && appLoadVoiceNotes !== loadVoiceNotes) {
        appLoadVoiceNotes();
        return;
    }
    
    // Fallback: implement directly if app.js function doesn't exist
    const userId = sessionStorage.getItem('neuroSync_userId') || sessionStorage.getItem('userId') || 'default';
    const API_BASE_URL = window.API_BASE_URL || window.location.origin;
    
    fetch(`${API_BASE_URL}/api/voicenote/list?userId=${userId}`)
        .then(res => res.ok ? res.json() : null)
        .then(data => {
            if (data && data.voiceNotes) {
                displayVoiceNotes(data.voiceNotes);
            }
        })
        .catch(err => console.error('Error loading voice notes:', err));
}

function loadClonedVoices() {
    // Check if app.js or voice-cloning.js has the function and it's not this one
    const appLoadClonedVoices = window.loadClonedVoices;
    if (typeof appLoadClonedVoices === 'function' && appLoadClonedVoices !== loadClonedVoices) {
        appLoadClonedVoices();
        return;
    }
    
    // Fallback: do nothing if function doesn't exist (voice cloning is optional)
    // Silently skip - this is expected if voice cloning isn't implemented
}

function initMultiLayerEmotion() {
    // Check if multilayer-emotion.js has the function and it's not this one
    const appInitMultiLayer = window.initMultiLayerEmotion;
    if (typeof appInitMultiLayer === 'function' && appInitMultiLayer !== initMultiLayerEmotion) {
        appInitMultiLayer();
        return;
    }
    
    // Fallback: do nothing if function doesn't exist (multi-layer emotion is optional)
    // Silently skip - this is expected if multi-layer emotion isn't implemented
}

// Helper function to display voice notes
function displayVoiceNotes(voiceNotes) {
    const list = document.getElementById('voiceNotesList');
    if (!list) return;
    
    if (!voiceNotes || voiceNotes.length === 0) {
        list.innerHTML = '<p>No voice notes yet. Record one to get started!</p>';
        return;
    }
    
    list.innerHTML = voiceNotes.map(note => `
        <div class="voice-note-item">
            <strong>${note.personName}</strong> - ${note.description || 'No description'}
            <button onclick="playVoiceNote('${note.id}')" class="play-voice-btn">▶️ Play</button>
            <small>${new Date(note.recordedAt).toLocaleDateString()}</small>
        </div>
    `).join('');
}

// Expose functions globally
// Expose functions globally (for other scripts or direct console access)
window.sendMessage = sendMessage;
window.quickEmotion = quickEmotion;
window.toggleSettings = toggleSettings;
window.closeSidebar = closeSidebar;
window.toggleCamera = toggleCamera;
window.handleInputKeydown = handleInputKeydown;
window.autoResizeTextarea = autoResizeTextarea;
window.updateCompanionAvatar = updateCompanionAvatar;
window.updateCompanionAvatarSpeaking = updateCompanionAvatarSpeaking;
// DO NOT expose toggleRecording to window - it causes recursion
// The real function from app.js is already exposed
// window.toggleRecording = toggleRecording; // DO NOT EXPOSE - causes recursion
// NOTE: We do NOT expose startFacialDetection, stopFacialDetection, or toggleTTS to window
// because they are wrappers that call the real functions from other scripts.
// The real functions are already exposed by their respective scripts.
// If we expose our wrappers, they might overwrite the real functions and cause recursion.
// window.startFacialDetection = startFacialDetection; // DO NOT EXPOSE - causes recursion
// window.stopFacialDetection = stopFacialDetection; // DO NOT EXPOSE - causes recursion
// window.toggleTTS = toggleTTS; // DO NOT EXPOSE - causes recursion
window.handleVoiceCloneFile = handleVoiceCloneFile; // Exposed for app.js to use
window.toggleConsent = toggleConsent; // Exposed for app.js to use
window.loadVoiceNotes = loadVoiceNotes; // Exposed for app.js to use
window.loadClonedVoices = loadClonedVoices; // Exposed for app.js to use
window.initMultiLayerEmotion = initMultiLayerEmotion; // Exposed for app.js to use
window.startCornerCamera = startCornerCamera;
window.toggleCornerCamera = toggleCornerCamera;
window.stopCornerCamera = stopCornerCamera;
window.startCornerCamera = startCornerCamera;
window.toggleCornerCamera = toggleCornerCamera;
window.stopCornerCamera = stopCornerCamera;
