// NeuroSync Companion UI - Clean, Modern Interface

// Note: isRecording, mediaRecorder, and audioChunks are declared in app.js
// We'll use the global versions to avoid conflicts
let companionMediaRecorder = null;
let companionAudioChunks = [];
let currentUserId = sessionStorage.getItem('neuroSync_userId') || sessionStorage.getItem('userId') || 'default';

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
    
    // Set up voice button
    const voiceBtn = document.getElementById('voiceBtn');
    if (voiceBtn) {
        voiceBtn.addEventListener('click', function() {
            toggleRecording();
        });
    }
    
    // Set up settings button
    const settingsBtn = document.getElementById('settingsBtn');
    if (settingsBtn) {
        settingsBtn.addEventListener('click', function() {
            toggleSettings();
        });
    }
    
    // Set up camera button
    const cameraBtn = document.getElementById('cameraBtn');
    if (cameraBtn) {
        cameraBtn.addEventListener('click', function() {
            toggleCamera();
        });
    }
    
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
    
    // Set up camera buttons
    const startCameraBtn = document.getElementById('startCameraBtn');
    if (startCameraBtn) {
        startCameraBtn.addEventListener('click', function() {
            startFacialDetection();
        });
    }
    
    const stopCameraBtn = document.getElementById('stopCameraBtn');
    if (stopCameraBtn) {
        stopCameraBtn.addEventListener('click', function() {
            stopFacialDetection();
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

// Removed initializeSignalR from companion-ui.js - app.js handles SignalR initialization
// This function is kept for backward compatibility but does nothing
function initializeSignalR() {
    // SignalR is initialized by app.js, not here
    // Just set up handlers if connection exists
    if (window.connection) {
        setupSignalRHandlers();
    }
}

function setupSignalRHandlers() {
    if (!window.connection) return;

    const connection = window.connection;

    // Only set up handlers if not already set up
    if (!connection._companionUIHandlersSet) {
        connection.on("EmotionDetected", (result) => {
            displayEmotionResult(result);
        });

        connection.on("AdaptiveResponse", (response) => {
            displayAdaptiveResponse(response);
        });

        connection.on("MultiLayerEmotionDetected", (result) => {
            displayMultiLayerResult(result);
        });

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
    if (typeof detectEmotion === 'function') {
        detectEmotion(text);
    } else {
        // Fallback: direct API call
        fetch(`${API_BASE_URL}/api/emotion/detect`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ text, userId: currentUserId })
        })
        .then(res => res.json())
        .then(data => {
            displayEmotionResult(data);
            if (data.response) {
                displayAdaptiveResponse(data.response);
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

function displayEmotionResult(result) {
    if (!result || !result.emotion) return;

    const emotion = result.emotion.toLowerCase();
    updateCompanionAvatar(emotion);
    
    // Update body background based on emotion
    document.body.className = `emotion-${emotion}`;

    // Add AI response message
    if (result.confidence) {
        const message = `I sense you're feeling ${emotion} (${Math.round(result.confidence * 100)}% confidence).`;
        addChatMessage(message, 'ai');
    }
}

function displayAdaptiveResponse(response) {
    if (!response || !response.message) return;

    // Add AI response to chat
    addChatMessage(response.message, 'ai');

    // Update avatar if emotion changed
    if (response.emotion) {
        updateCompanionAvatar(response.emotion.toLowerCase());
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

// Camera management
function toggleCamera() {
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

// Voice recording (reuse from app.js if available)
function toggleRecording() {
    // Use the function from app.js if available
    if (typeof window.toggleRecording === 'function') {
        window.toggleRecording();
        return;
    }

    // Fallback: use local implementation
    const isRecording = companionMediaRecorder?.state === 'recording';
    if (!isRecording) {
        startRecording();
    } else {
        stopRecording();
    }
}

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
function toggleTTS() {
    const externalToggleTTS = window.toggleTTS;
    if (typeof externalToggleTTS === 'function' && externalToggleTTS !== toggleTTS) {
        externalToggleTTS();
        return;
    }
    // Fallback: do nothing if external function doesn't exist
    console.log('toggleTTS: No external function found');
}

// Facial detection (reuse from facial-detection.js)
function startFacialDetection() {
    // Check if facial-detection.js has loaded and exposed the function
    const externalStartFacial = window.startFacialDetection;
    
    // If window.startFacialDetection exists and is different from this function, use it
    if (externalStartFacial && typeof externalStartFacial === 'function' && externalStartFacial !== startFacialDetection) {
        externalStartFacial();
        document.getElementById('startCameraBtn')?.style.setProperty('display', 'none');
        document.getElementById('stopCameraBtn')?.style.setProperty('display', 'block');
        document.getElementById('facialEmotionDisplay')?.style.setProperty('display', 'block');
        return;
    }
    
    // If we reach here, either the function doesn't exist or it's this function itself
    // Wait a bit for facial-detection.js to load
    if (typeof window.startFacialDetection === 'undefined' || window.startFacialDetection === startFacialDetection) {
        console.log('startFacialDetection: Waiting for facial-detection.js to load...');
        setTimeout(() => {
            const loadedFunction = window.startFacialDetection;
            if (loadedFunction && typeof loadedFunction === 'function' && loadedFunction !== startFacialDetection) {
                loadedFunction();
                document.getElementById('startCameraBtn')?.style.setProperty('display', 'none');
                document.getElementById('stopCameraBtn')?.style.setProperty('display', 'block');
                document.getElementById('facialEmotionDisplay')?.style.setProperty('display', 'block');
            } else {
                console.warn('startFacialDetection: facial-detection.js function not available');
            }
        }, 1000);
    }
}

function stopFacialDetection() {
    const externalStopFacial = window.stopFacialDetection;
    if (typeof externalStopFacial === 'function' && externalStopFacial !== stopFacialDetection) {
        externalStopFacial();
        document.getElementById('startCameraBtn')?.style.setProperty('display', 'block');
        document.getElementById('stopCameraBtn')?.style.setProperty('display', 'none');
        document.getElementById('facialEmotionDisplay')?.style.setProperty('display', 'none');
        return;
    }
    // Fallback: do nothing if external function doesn't exist
    console.log('stopFacialDetection: No external function found');
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
    
    // Fallback: do nothing if function doesn't exist
    console.log('loadClonedVoices: No external function found');
}

function initMultiLayerEmotion() {
    // Check if multilayer-emotion.js has the function and it's not this one
    const appInitMultiLayer = window.initMultiLayerEmotion;
    if (typeof appInitMultiLayer === 'function' && appInitMultiLayer !== initMultiLayerEmotion) {
        appInitMultiLayer();
        return;
    }
    
    // Fallback: do nothing if function doesn't exist
    console.log('initMultiLayerEmotion: No external function found');
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
window.sendMessage = sendMessage;
window.quickEmotion = quickEmotion;
window.toggleSettings = toggleSettings;
window.closeSidebar = closeSidebar;
window.toggleCamera = toggleCamera;
window.handleInputKeydown = handleInputKeydown;
window.autoResizeTextarea = autoResizeTextarea;
window.updateCompanionAvatar = updateCompanionAvatar;
