const API_BASE_URL = window.location.origin;
const HUB_URL = `${API_BASE_URL}/emotionHub`;

// Make API_BASE_URL available globally for facial-detection.js
window.API_BASE_URL = API_BASE_URL;

let connection = null;

// Initialize SignalR connection
function initializeSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl(HUB_URL)
        .withAutomaticReconnect()
        .build();

    connection.on("Connected", (connectionId) => {
        console.log("Connected to SignalR:", connectionId);
        updateConnectionStatus(true);
    });

    connection.on("EmotionDetected", (result) => {
        console.log("Emotion detected:", result);
        displayEmotionResult(result);
    });

    connection.on("AdaptiveResponse", (response) => {
        console.log("Adaptive response:", response);
        displayAdaptiveResponse(response);
    });

    connection.on("IoTAction", (action) => {
        console.log("IoT Action:", action);
        displayIoTAction(action);
    });

    connection.onreconnecting(() => {
        updateConnectionStatus(false);
    });

    connection.onreconnected(() => {
        updateConnectionStatus(true);
    });

    connection.start()
        .then(() => {
            console.log("SignalR connection started");
            updateConnectionStatus(true);
        })
        .catch(err => {
            console.error("Error starting SignalR connection:", err);
            updateConnectionStatus(false);
        });
}

function updateConnectionStatus(connected) {
    const statusDot = document.querySelector('.status-dot');
    const statusText = document.getElementById('statusText');
    
    if (connected) {
        statusDot.classList.add('connected');
        statusText.textContent = 'Connected';
    } else {
        statusDot.classList.remove('connected');
        statusText.textContent = 'Disconnected';
    }
}

async function detectEmotion() {
    const input = document.getElementById('emotionInput');
    const text = input.value.trim();

    if (!text) {
        alert('Please enter some text to analyze');
        return;
    }

    const button = document.getElementById('detectButton');
    button.disabled = true;
    button.textContent = 'Analyzing...';

    try {
        // Get or create user ID
        let userId = sessionStorage.getItem('neuroSync_userId');
        if (!userId) {
            userId = 'user_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
            sessionStorage.setItem('neuroSync_userId', userId);
        }

        const response = await fetch(`${API_BASE_URL}/api/emotion/detect`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ 
                text: text,
                userId: userId
            })
        });

        if (!response.ok) {
            // Clone response to read it without consuming the stream
            const clonedResponse = response.clone();
            let errorMessage = `HTTP error! status: ${response.status}`;
            try {
                const errorData = await clonedResponse.json();
                errorMessage += ` - ${errorData.error || JSON.stringify(errorData)}`;
                if (errorData.details) {
                    console.error('Server error details:', errorData.details);
                }
            } catch (e) {
                // If response is not JSON, try text
                try {
                    const text = await response.text();
                    errorMessage += ` - ${text}`;
                } catch (textError) {
                    errorMessage += ` - Could not read error response`;
                }
            }
            throw new Error(errorMessage);
        }

        const data = await response.json();
        console.log('Emotion detection result:', data);

        // Handle action results (like voice note playback, person memory, etc.)
        if (data.actionResult) {
            displayActionResult(data.actionResult);
        }

        // Display results (SignalR will also update via real-time events)
        if (data.emotion) {
            try {
                displayEmotionResult(data.emotion);
            } catch (error) {
                console.error('Error displaying emotion result:', error);
                // Fallback display - don't throw, just show basic info
                const emotionType = document.getElementById('emotionType');
                const emotionConfidence = document.getElementById('emotionConfidence');
                const resultCard = document.getElementById('emotionResult');
                if (emotionType) {
                    emotionType.textContent = getEmotionName(data.emotion.emotion || data.emotion);
                }
                if (emotionConfidence && data.emotion.confidence !== undefined) {
                    const confidencePercent = (data.emotion.confidence * 100).toFixed(1);
                    emotionConfidence.textContent = `Confidence: ${confidencePercent}%`;
                }
                if (resultCard) {
                    resultCard.style.display = 'block';
                }
            }
        }
        if (data.adaptiveResponse) {
            try {
                displayAdaptiveResponse(data.adaptiveResponse);
            } catch (error) {
                console.error('Error displaying adaptive response:', error);
                // Silent fail for display errors
            }
        }
        if (data.iotActions) {
            try {
                displayIoTActions(data.iotActions);
            } catch (error) {
                console.error('Error displaying IoT actions:', error);
                // Silent fail for display errors
            }
        }
    } catch (error) {
        console.error('Error detecting emotion:', error);
        // Only show alert for actual API errors, not display errors
        // Display errors are already handled with fallback displays
        if (error.message && error.message.includes('HTTP error')) {
            alert('Error detecting emotion. Please try again.\n\n' + error.message);
        } else {
            // For other errors (like display errors), just log them
            console.error('Display error (emotion was detected successfully):', error);
        }
    } finally {
        button.disabled = false;
        button.textContent = 'Detect Emotion';
    }
}

// Map emotion enum values to names
const emotionNames = {
    0: 'Happy',
    1: 'Sad',
    2: 'Angry',
    3: 'Anxious',
    4: 'Calm',
    5: 'Excited',
    6: 'Frustrated',
    7: 'Neutral'
};

function getEmotionName(emotion) {
    // Handle null/undefined
    if (emotion === null || emotion === undefined) {
        return 'Unknown';
    }
    
    // Handle number (enum value)
    if (typeof emotion === 'number') {
        return emotionNames[emotion] || `Emotion${emotion}`;
    }
    
    // Handle string (already a name)
    if (typeof emotion === 'string') {
        return emotion;
    }
    
    // Fallback - convert to string
    return String(emotion);
}

function displayEmotionResult(result) {
    const resultCard = document.getElementById('emotionResult');
    const emotionType = document.getElementById('emotionType');
    const emotionConfidence = document.getElementById('emotionConfidence');

    // Handle emotion enum (can be number or string)
    const emotionValue = result.emotion;
    const emotionName = getEmotionName(emotionValue);
    emotionType.textContent = emotionName;
    
    // Ensure emotionName is a string before calling toLowerCase
    const emotionNameString = String(emotionName);
    emotionType.className = `emotion-badge ${emotionNameString.toLowerCase()}`;
    
    const confidencePercent = (result.confidence * 100).toFixed(1);
    emotionConfidence.textContent = `Confidence: ${confidencePercent}%`;

    resultCard.style.display = 'block';
}

// TTS (Text-to-Speech) functionality
let speechSynthesis = null;
let currentUtterance = null;

// Initialize speech synthesis
function initTTS() {
    try {
        if ('speechSynthesis' in window && window.speechSynthesis) {
            speechSynthesis = window.speechSynthesis;
            console.log('Text-to-Speech initialized');
            return true;
        } else {
            console.warn('Speech synthesis not supported in this browser');
            return false;
        }
    } catch (error) {
        console.warn('Speech synthesis initialization failed:', error);
        return false;
    }
}

// Speak text using browser TTS
function speakText(text, voiceId = null, personName = null) {
    try {
        if (!speechSynthesis || !window.speechSynthesis) {
            // TTS not available - fail silently (not an error, just not supported)
            return;
        }

        // Stop any current speech
        if (currentUtterance && speechSynthesis.speaking) {
            speechSynthesis.cancel();
        }

        // Get user's preferred voice or cloned voice
        const utterance = new SpeechSynthesisUtterance(text);
        
        // Set voice properties
        utterance.rate = 1.0; // Speed (0.1 to 10)
        utterance.pitch = 1.0; // Pitch (0 to 2)
        utterance.volume = 1.0; // Volume (0 to 1)
        
        // If voice cloning is requested, try to use it (fallback to default for now)
        // TODO: Implement voice cloning via API
        if (voiceId && personName) {
            // For now, use default voice - will be enhanced with cloned voices
            utterance.rate = 0.95; // Slightly slower for more natural feel
        }

        // Set language
        utterance.lang = 'en-US';

        utterance.onend = () => {
            currentUtterance = null;
        };

        utterance.onerror = (event) => {
            // Silently handle TTS errors (browser might not support it)
            currentUtterance = null;
        };

        currentUtterance = utterance;
        speechSynthesis.speak(utterance);
    } catch (error) {
        // TTS not available or failed - fail silently
        // This is expected in some browsers/environments
        return;
    }
}

// Stop speaking
function stopSpeaking() {
    try {
        if (speechSynthesis && currentUtterance && speechSynthesis.speaking) {
            speechSynthesis.cancel();
            currentUtterance = null;
        }
    } catch (error) {
        // Silently handle errors
        currentUtterance = null;
    }
}

// Check if user wants TTS enabled (stored in localStorage)
function isTTSEnabled() {
    return localStorage.getItem('neuroSync_tts_enabled') === 'true';
}

// Toggle TTS (exposed globally for onclick handler)
window.toggleTTS = function() {
    const enabled = !isTTSEnabled();
    localStorage.setItem('neuroSync_tts_enabled', enabled.toString());
    updateTTSToggleButton();
    return enabled;
};

// Update TTS toggle button
function updateTTSToggleButton() {
    const ttsButton = document.getElementById('ttsToggleBtn');
    if (ttsButton) {
        const enabled = isTTSEnabled();
        const available = speechSynthesis != null;
        
        if (available) {
            ttsButton.textContent = enabled ? '🔊 TTS On' : '🔇 TTS Off';
            ttsButton.classList.toggle('active', enabled);
            ttsButton.title = enabled ? 'Text-to-Speech: On' : 'Text-to-Speech: Off';
            ttsButton.disabled = false;
        } else {
            ttsButton.textContent = '🔇 TTS Unavailable';
            ttsButton.classList.remove('active');
            ttsButton.title = 'Text-to-Speech: Not available in this browser';
            ttsButton.disabled = true;
        }
    }
}

function displayAdaptiveResponse(response) {
    const responseCard = document.getElementById('adaptiveResponse');
    const responseMessage = document.getElementById('responseMessage');
    const responseSuggestion = document.getElementById('responseSuggestion');

    responseMessage.textContent = response.message || 'No message available';
    
    // Speak the response if TTS is enabled
    if (isTTSEnabled() && response.message) {
        // Check if a cloned voice should be used
        const personName = response.parameters?.personName;
        const voiceId = response.parameters?.voiceId;
        
        speakText(response.message, voiceId, personName);
    }
    
    let suggestionHtml = '';
    if (response.parameters) {
        // Show encouragement if available
        if (response.parameters.encouragement) {
            suggestionHtml += `<div class="encouragement-item">💚 ${response.parameters.encouragement}</div>`;
        }
        
        // Show insight if available
        if (response.parameters.insight) {
            suggestionHtml += `<div class="insight-item">💭 ${response.parameters.insight}</div>`;
        }
        
        // Show support note for concerning patterns
        if (response.parameters.support_note) {
            suggestionHtml += `<div class="support-note">🤗 ${response.parameters.support_note}</div>`;
        }
        
        if (response.parameters.suggestion) {
            suggestionHtml += `<div class="suggestion-item">💡 ${response.parameters.suggestion}</div>`;
        }
        
        // Show follow-up question
        if (response.parameters.followUpQuestion) {
            suggestionHtml += `<div class="follow-up-question">❓ ${response.parameters.followUpQuestion}</div>`;
        }
        
        if (response.parameters.activities && Array.isArray(response.parameters.activities)) {
            suggestionHtml += '<div class="activities-list"><strong>Suggested Activities:</strong><ul>';
            response.parameters.activities.forEach(activity => {
                suggestionHtml += `<li>${activity}</li>`;
            });
            suggestionHtml += '</ul></div>';
        }
        if (response.parameters.music) {
            suggestionHtml += `<div class="music-suggestion">🎵 ${response.parameters.music}</div>`;
        }
    }
    
    if (suggestionHtml) {
        responseSuggestion.innerHTML = suggestionHtml;
        responseSuggestion.style.display = 'block';
    } else {
        responseSuggestion.style.display = 'none';
    }

    responseCard.style.display = 'block';
}

function displayIoTAction(action) {
    const actionsCard = document.getElementById('iotActions');
    const actionsList = document.getElementById('iotActionsList');

    // Add new action to the list
    const actionItem = document.createElement('div');
    actionItem.className = 'iot-action-item';
    
    let paramsHtml = '';
    if (action.parameters) {
        const params = Object.entries(action.parameters);
        if (action.actionType === 'playMusic') {
            // Format music action nicely
            paramsHtml = `
                <div class="music-action">
                    <div><strong>Genre:</strong> ${action.parameters.genre || 'N/A'}</div>
                    <div><strong>Playlist:</strong> ${action.parameters.playlist || 'N/A'}</div>
                    <div><strong>Volume:</strong> ${action.parameters.volume || 'N/A'}%</div>
                    ${action.parameters.suggestion ? `<div class="music-suggestion-text">${action.parameters.suggestion}</div>` : ''}
                    ${action.parameters.playlistUrl ? `<div class="playlist-url"><a href="${action.parameters.playlistUrl}" target="_blank" class="play-button">▶️ Play on YouTube Music</a></div>` : ''}
                </div>
            `;
        } else {
            paramsHtml = params.map(([key, value]) => 
                `<div><strong>${key}:</strong> ${value}</div>`
            ).join('');
        }
    }

    const deviceIcon = action.deviceId === 'speaker' ? '🔊' : 
                       action.deviceId.startsWith('light') ? '💡' : 
                       action.deviceId === 'notification' ? '🔔' : '📱';

    actionItem.innerHTML = `
        <h4>${deviceIcon} ${action.deviceId} - ${action.actionType}</h4>
        ${paramsHtml || '<p>No parameters</p>'}
    `;

    actionsList.appendChild(actionItem);
    actionsCard.style.display = 'block';
}

function displayIoTActions(actions) {
    const actionsCard = document.getElementById('iotActions');
    const actionsList = document.getElementById('iotActionsList');

    // Clear existing actions
    actionsList.innerHTML = '';

    // Add all actions
    actions.forEach(action => {
        const actionItem = document.createElement('div');
        actionItem.className = 'iot-action-item';
        
        let paramsHtml = '';
        if (action.parameters) {
            const params = Object.entries(action.parameters);
            if (action.actionType === 'playMusic') {
                // Format music action nicely
                paramsHtml = `
                    <div class="music-action">
                        <div><strong>Genre:</strong> ${action.parameters.genre || 'N/A'}</div>
                        <div><strong>Playlist:</strong> ${action.parameters.playlist || 'N/A'}</div>
                        <div><strong>Volume:</strong> ${action.parameters.volume || 'N/A'}%</div>
                        ${action.parameters.suggestion ? `<div class="music-suggestion-text">${action.parameters.suggestion}</div>` : ''}
                        ${action.parameters.playlistUrl ? `<div class="playlist-url"><a href="${action.parameters.playlistUrl}" target="_blank" class="play-button">▶️ Play on YouTube Music</a></div>` : ''}
                    </div>
                `;
            } else {
                paramsHtml = params.map(([key, value]) => 
                    `<div><strong>${key}:</strong> ${value}</div>`
                ).join('');
            }
        }

        const deviceIcon = action.deviceId === 'speaker' ? '🔊' : 
                           action.deviceId.startsWith('light') ? '💡' : 
                           action.deviceId === 'notification' ? '🔔' : '📱';

        actionItem.innerHTML = `
            <h4>${deviceIcon} ${action.deviceId} - ${action.actionType}</h4>
            ${paramsHtml || '<p>No parameters</p>'}
        `;

        actionsList.appendChild(actionItem);
    });

    actionsCard.style.display = 'block';
}

// Voice recording functionality
let mediaRecorder = null;
let audioChunks = [];
let isRecording = false;

async function toggleRecording() {
    const recordButton = document.getElementById('recordButton');
    const recordingStatus = document.getElementById('recordingStatus');
    
    if (!isRecording) {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            mediaRecorder = new MediaRecorder(stream);
            audioChunks = [];
            
            mediaRecorder.ondataavailable = (event) => {
                audioChunks.push(event.data);
            };
            
            mediaRecorder.onstop = async () => {
                const audioBlob = new Blob(audioChunks, { type: 'audio/webm' });
                await uploadVoiceNote(audioBlob);
                stream.getTracks().forEach(track => track.stop());
            };
            
            mediaRecorder.start();
            isRecording = true;
            recordButton.textContent = '⏹️ Stop Recording';
            recordButton.style.backgroundColor = '#ef4444';
            recordingStatus.style.display = 'block';
        } catch (error) {
            console.error('Error accessing microphone:', error);
            alert('Could not access microphone. Please check permissions.');
        }
    } else {
        if (mediaRecorder && mediaRecorder.state !== 'inactive') {
            mediaRecorder.stop();
            isRecording = false;
            recordButton.textContent = '🎤 Record Voice';
            recordButton.style.backgroundColor = '';
            recordingStatus.style.display = 'none';
        }
    }
}

async function uploadVoiceNote(audioBlob) {
    const userId = sessionStorage.getItem('neuroSync_userId') || 'default';
    const personName = prompt('Who is this voice note from? (Enter name):');
    if (!personName) return;
    
    const description = prompt('Description (optional):') || '';
    
    const formData = new FormData();
    formData.append('userId', userId);
    formData.append('personName', personName);
    formData.append('audioFile', audioBlob, `voice_${Date.now()}.webm`);
    formData.append('description', description);
    
    try {
        const response = await fetch(`${API_BASE_URL}/api/voicenote/upload`, {
            method: 'POST',
            body: formData
        });
        
        if (response.ok) {
            const result = await response.json();
            alert(`Voice note saved for ${personName}!`);
            loadVoiceNotes();
        } else {
            alert('Failed to upload voice note');
        }
    } catch (error) {
        console.error('Error uploading voice note:', error);
        alert('Error uploading voice note');
    }
}

function displayActionResult(actionResult) {
    const responseCard = document.getElementById('adaptiveResponse');
    const responseMessage = document.getElementById('responseMessage');
    const responseSuggestion = document.getElementById('responseSuggestion');
    
    // Show action result in adaptive response area
    responseMessage.textContent = actionResult.message || '';
    
    let suggestionHtml = '';
    if (actionResult.parameters) {
        // Handle voice note playback (auto-play if autoPlay is true)
        if (actionResult.actionType === 'play_voice' && actionResult.parameters.voiceNoteId) {
            const audioPlayer = document.getElementById('audioPlayer');
            const userId = sessionStorage.getItem('neuroSync_userId') || 'default';
            audioPlayer.src = `${API_BASE_URL}/api/voicenote/play/${actionResult.parameters.voiceNoteId}?userId=${userId}`;
            audioPlayer.style.display = 'block';
            
            // Auto-play if requested
            if (actionResult.parameters.autoPlay) {
                audioPlayer.play().catch(err => console.error('Error playing audio:', err));
            }
            
            const relationship = actionResult.parameters.relationship ? ` (your ${actionResult.parameters.relationship})` : '';
            suggestionHtml += `<div class="action-result">🎵 ${actionResult.message || `Playing voice note from ${actionResult.parameters.personName}${relationship}...`}</div>`;
            
            // Show all available voice notes if there are multiple
            if (actionResult.parameters.totalNotes > 1 && actionResult.parameters.allVoiceNotes) {
                suggestionHtml += '<div class="voice-notes-quick-list" style="margin-top: 10px;">';
                actionResult.parameters.allVoiceNotes.forEach(note => {
                    suggestionHtml += `<button onclick="playVoiceNote('${note.id}')" class="play-voice-btn" style="margin: 5px;">▶️ Play ${note.description || 'Voice Note'}</button>`;
                });
                suggestionHtml += '</div>';
            }
        }
        
        // Handle missing person (auto-play if available)
        if (actionResult.actionType === 'missing_person') {
            suggestionHtml += `<div class="action-result">💚 ${actionResult.message}</div>`;
            
            if (actionResult.parameters.autoPlay && actionResult.parameters.voiceNoteId) {
                // Auto-play the voice note
                const audioPlayer = document.getElementById('audioPlayer');
                const userId = sessionStorage.getItem('neuroSync_userId') || 'default';
                audioPlayer.src = `${API_BASE_URL}/api/voicenote/play/${actionResult.parameters.voiceNoteId}?userId=${userId}`;
                audioPlayer.style.display = 'block';
                audioPlayer.play().catch(err => console.error('Error playing audio:', err));
            }
            
            if (actionResult.parameters.hasVoiceNotes && actionResult.parameters.voiceNotes && actionResult.parameters.voiceNotes.length > 0) {
                suggestionHtml += '<div class="voice-notes-quick-list" style="margin-top: 10px;">';
                actionResult.parameters.voiceNotes.forEach(note => {
                    suggestionHtml += `<button onclick="playVoiceNote('${note.id}')" class="play-voice-btn" style="margin: 5px;">▶️ Play ${note.description || 'Voice Note'}</button>`;
                });
                suggestionHtml += '</div>';
            }
        }
        
        // Handle tell about person (show voice notes if available)
        if (actionResult.actionType === 'tell_about_person' && actionResult.parameters.hasVoiceNotes) {
            if (actionResult.parameters.voiceNotes && actionResult.parameters.voiceNotes.length > 0) {
                suggestionHtml += '<div class="voice-notes-quick-list" style="margin-top: 10px;">';
                actionResult.parameters.voiceNotes.forEach(note => {
                    suggestionHtml += `<button onclick="playVoiceNote('${note.id}')" class="play-voice-btn" style="margin: 5px;">▶️ Play ${note.description || 'Voice Note'}</button>`;
                });
                suggestionHtml += '</div>';
            }
        }
    }
    
    if (suggestionHtml) {
        responseSuggestion.innerHTML = suggestionHtml;
        responseSuggestion.style.display = 'block';
    }
    
    responseCard.style.display = 'block';
}

async function playVoiceNote(voiceNoteId) {
    const userId = sessionStorage.getItem('neuroSync_userId') || 'default';
    const audioPlayer = document.getElementById('audioPlayer');
    audioPlayer.src = `${API_BASE_URL}/api/voicenote/play/${voiceNoteId}?userId=${userId}`;
    audioPlayer.style.display = 'block';
    audioPlayer.play();
}

async function loadVoiceNotes() {
    const userId = sessionStorage.getItem('neuroSync_userId') || 'default';
    try {
        const response = await fetch(`${API_BASE_URL}/api/voicenote/list?userId=${userId}`);
        if (response.ok) {
            const data = await response.json();
            displayVoiceNotes(data.voiceNotes);
        }
    } catch (error) {
        console.error('Error loading voice notes:', error);
    }
};

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

// Load voice notes on page load
window.addEventListener('load', () => {
    loadVoiceNotes();
});

// Allow Enter key to submit (Shift+Enter for new line)
document.getElementById('emotionInput').addEventListener('keydown', function(e) {
    if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        detectEmotion();
    }
});

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    initializeSignalR();
});

