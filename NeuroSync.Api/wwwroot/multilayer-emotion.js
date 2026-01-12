// Multi-layer emotion detection functionality

const API_BASE_URL = window.API_BASE_URL || '';

// Store user consent preferences
let userConsent = {
    emotionSensing: false,
    visualLayer: false,
    audioLayer: false,
    biometricLayer: false,
    dataStorage: false
};

// Initialize multi-layer emotion detection
function initMultiLayerEmotion() {
    loadUserConsent();
    setupMultiLayerUI();
}

// Load user consent from localStorage
function loadUserConsent() {
    const saved = localStorage.getItem('neuroSync_consent');
    if (saved) {
        userConsent = { ...userConsent, ...JSON.parse(saved) };
    }
    updateConsentUI();
}

// Save user consent to localStorage
function saveUserConsent() {
    localStorage.setItem('neuroSync_consent', JSON.stringify(userConsent));
    updateConsentUI();
}

// Setup multi-layer UI elements
function setupMultiLayerUI() {
    // Ensure consent panel exists
    const consentPanel = document.getElementById('consentPanel');
    if (consentPanel) {
        updateConsentUI();
    }
}

// Update consent UI to reflect current state
function updateConsentUI() {
    const checkboxes = {
        emotionSensing: document.getElementById('consentEmotionSensing'),
        visualLayer: document.getElementById('consentVisualLayer'),
        audioLayer: document.getElementById('consentAudioLayer'),
        biometricLayer: document.getElementById('consentBiometricLayer'),
        dataStorage: document.getElementById('consentDataStorage')
    };

    for (const [key, checkbox] of Object.entries(checkboxes)) {
        if (checkbox) {
            checkbox.checked = userConsent[key] || false;
        }
    }
}

// Toggle consent
function toggleConsent(type) {
    if (userConsent.hasOwnProperty(type)) {
        userConsent[type] = !userConsent[type];
        saveUserConsent();
        sendConsentToServer();
    }
}

// Send consent to server
async function sendConsentToServer() {
    try {
        const userId = sessionStorage.getItem('neuroSync_userId') || localStorage.getItem('userId') || 'default';
        const response = await fetch(`${API_BASE_URL}/api/ethical/consent`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                userId: userId,
                ...userConsent
            })
        });
        if (response.ok) {
            console.log('Consent updated on server');
        }
    } catch (error) {
        console.error('Error sending consent to server:', error);
    }
}

// Detect multi-layer emotion
async function detectMultiLayerEmotion() {
    const userId = sessionStorage.getItem('neuroSync_userId') || 'default';

    // Check basic consent
    if (!userConsent.emotionSensing) {
        alert('Please provide consent for emotion sensing first.');
        showConsentPanel();
        return;
    }

    // Collect data from all layers
    const request = {
        userId: userId,
        text: document.getElementById('emotionInput')?.value || ''
    };

    // Layer 1: Visual (from facial detection if available)
    const visualEmotion = getVisualEmotion();
    if (visualEmotion && userConsent.visualLayer) {
        request.VisualEmotion = visualEmotion.emotion;
        request.VisualConfidence = visualEmotion.confidence;
    }

    // Layer 2: Audio (simulated or from voice recording)
    if (userConsent.audioLayer) {
        const audioData = getAudioData();
        if (audioData) {
            request.AudioTranscript = audioData.transcript;
            request.AudioPitch = audioData.pitch;
            request.AudioVolume = audioData.volume;
            request.AudioSpeechRate = audioData.speechRate;
        }
    }

    // Layer 3: Biometric (simulated - can be replaced with real device data)
    if (userConsent.biometricLayer) {
        const biometricData = getBiometricData();
        if (biometricData) {
            request.HeartRate = biometricData.heartRate;
            request.HRV = biometricData.hrv;
            request.SkinConductivity = biometricData.skinConductivity;
            request.Temperature = biometricData.temperature;
        }
    }

    // Layer 4: Contextual (activity and task data)
    const contextualData = getContextualData();
    if (contextualData) {
        request.ActivityType = contextualData.activityType;
        request.ActivityIntensity = contextualData.activityIntensity;
        request.TaskIntensity = contextualData.taskIntensity;
        request.TaskComplexity = contextualData.taskComplexity;
    }

    try {
        showLoading('Detecting emotion from multiple layers...');
        
        const response = await fetch(`${API_BASE_URL}/api/emotion/multilayer`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(request)
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to detect emotion');
        }

        const result = await response.json();
        displayMultiLayerResult(result);
        hideLoading();
    } catch (error) {
        console.error('Error detecting multi-layer emotion:', error);
        hideLoading();
        alert(`Error: ${error.message}`);
    }
}

// Get visual emotion (from facial detection)
function getVisualEmotion() {
    // Check if facial detection is active and has detected emotion
    const facialEmotionDisplay = document.querySelector('.facial-emotion-display');
    if (facialEmotionDisplay && window.lastFacialEmotion) {
        return {
            emotion: window.lastFacialEmotion.emotion,
            confidence: window.lastFacialEmotion.confidence
        };
    }
    return null;
}

// Get audio data (simulated - can be replaced with real audio analysis)
function getAudioData() {
    // Placeholder - would analyze voice recording if available
    return {
        transcript: document.getElementById('emotionInput')?.value || '',
        pitch: 150 + Math.random() * 50, // Simulated pitch
        volume: 0.5 + Math.random() * 0.3, // Simulated volume
        speechRate: 120 + Math.random() * 60 // Simulated speech rate (words/min)
    };
}

// Get biometric data (simulated - can be replaced with real device data)
function getBiometricData() {
    const heartRateInput = document.getElementById('biometricHeartRate');
    const gsrInput = document.getElementById('biometricGSR');
    const tempInput = document.getElementById('biometricTemperature');

    return {
        heartRate: heartRateInput?.value ? parseFloat(heartRateInput.value) : null,
        hrv: null, // Would come from device
        skinConductivity: gsrInput?.value ? parseFloat(gsrInput.value) : null,
        temperature: tempInput?.value ? parseFloat(tempInput.value) : null
    };
}

// Get contextual data
function getContextualData() {
    const activityType = document.getElementById('activityType')?.value || '';
    const activityIntensity = document.getElementById('activityIntensity')?.value;
    const taskIntensity = document.getElementById('taskIntensity')?.value;
    const taskComplexity = document.getElementById('taskComplexity')?.value;

    return {
        activityType: activityType || undefined,
        activityIntensity: activityIntensity ? parseFloat(activityIntensity) : undefined,
        taskIntensity: taskIntensity ? parseFloat(taskIntensity) : undefined,
        taskComplexity: taskComplexity ? parseFloat(taskComplexity) : undefined
    };
}

// Display multi-layer result
function displayMultiLayerResult(result) {
    const container = document.getElementById('multilayerResultContainer') || createMultiLayerResultContainer();
    
    // Clear previous results
    container.innerHTML = '';

    // Display fused emotion
    if (result.fusedEmotion) {
        const fusedSection = createFusedEmotionSection(result.fusedEmotion);
        container.appendChild(fusedSection);
    }

    // Display individual layers
    const layersSection = createLayersSection(result.fusedEmotion);
    container.appendChild(layersSection);

    // Display adaptive response
    if (result.adaptiveResponse) {
        const responseSection = createResponseSection(result.adaptiveResponse);
        container.appendChild(responseSection);
    }

    // Display IoT actions
    if (result.iotActions && result.iotActions.length > 0) {
        const actionsSection = createActionsSection(result.iotActions);
        container.appendChild(actionsSection);
    }

    // Scroll to results
    container.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

// Create multi-layer result container
function createMultiLayerResultContainer() {
    const container = document.createElement('div');
    container.id = 'multilayerResultContainer';
    container.className = 'multilayer-result-container';
    
    const mainContainer = document.querySelector('.container') || document.body;
    mainContainer.appendChild(container);
    
    return container;
}

// Create fused emotion section
function createFusedEmotionSection(fusedEmotion) {
    const section = document.createElement('div');
    section.className = 'multilayer-section fused-emotion-section';
    
    section.innerHTML = `
        <h3>🎯 Fused Emotion Result</h3>
        <div class="fused-emotion-display">
            <div class="emotion-badge large">${fusedEmotion.primaryEmotion}</div>
            <div class="confidence-score">Confidence: ${(fusedEmotion.overallConfidence * 100).toFixed(1)}%</div>
            <div class="layer-summary">
                <span class="layer-indicator ${fusedEmotion.visualLayer ? 'active' : 'inactive'}">Visual</span>
                <span class="layer-indicator ${fusedEmotion.audioLayer ? 'active' : 'inactive'}">Audio</span>
                <span class="layer-indicator ${fusedEmotion.biometricLayer ? 'active' : 'inactive'}">Biometric</span>
                <span class="layer-indicator ${fusedEmotion.contextualLayer ? 'active' : 'inactive'}">Contextual</span>
            </div>
        </div>
    `;
    
    return section;
}

// Create layers section
function createLayersSection(fusedEmotion) {
    const section = document.createElement('div');
    section.className = 'multilayer-section layers-section';
    
    section.innerHTML = `
        <h3>📊 Layer Details</h3>
        <div class="layers-grid">
            ${createLayerCard('Visual', fusedEmotion.visualLayer, '👁️')}
            ${createLayerCard('Audio', fusedEmotion.audioLayer, '🎤')}
            ${createLayerCard('Biometric', fusedEmotion.biometricLayer, '💓')}
            ${createLayerCard('Contextual', fusedEmotion.contextualLayer, '📱')}
        </div>
    `;
    
    return section;
}

// Create layer card
function createLayerCard(layerName, layerData, icon) {
    if (!layerData) {
        return `
            <div class="layer-card inactive">
                <div class="layer-icon">${icon}</div>
                <div class="layer-name">${layerName}</div>
                <div class="layer-status">Not Available</div>
            </div>
        `;
    }

    const emotion = layerData.emotion || 'Unknown';
    const confidence = layerData.confidence ? (layerData.confidence * 100).toFixed(1) : 'N/A';
    
    return `
        <div class="layer-card active">
            <div class="layer-icon">${icon}</div>
            <div class="layer-name">${layerName}</div>
            <div class="layer-emotion">${emotion}</div>
            <div class="layer-confidence">${confidence}%</div>
        </div>
    `;
}

// Create response section
function createResponseSection(adaptiveResponse) {
    const section = document.createElement('div');
    section.className = 'multilayer-section response-section';
    
    section.innerHTML = `
        <h3>💬 Adaptive Response</h3>
        <div class="adaptive-response">
            <p class="response-message">${adaptiveResponse.message || ''}</p>
            ${adaptiveResponse.followUpQuestion ? `<p class="follow-up">${adaptiveResponse.followUpQuestion}</p>` : ''}
        </div>
    `;
    
    return section;
}

// Create actions section
function createActionsSection(actions) {
    const section = document.createElement('div');
    section.className = 'multilayer-section actions-section';
    
    const actionsHtml = actions.map(action => `
        <div class="iot-action">
            <span class="action-device">${action.deviceId}</span>
            <span class="action-type">${action.actionType}</span>
            <span class="action-params">${JSON.stringify(action.parameters || {})}</span>
        </div>
    `).join('');
    
    section.innerHTML = `
        <h3>🎛️ IoT Actions</h3>
        <div class="iot-actions-list">
            ${actionsHtml}
        </div>
    `;
    
    return section;
}

// Show consent panel
function showConsentPanel() {
    const panel = document.getElementById('consentPanel');
    if (panel) {
        panel.style.display = 'block';
        panel.scrollIntoView({ behavior: 'smooth' });
    }
}

// Show loading indicator
function showLoading(message) {
    let loading = document.getElementById('multilayerLoading');
    if (!loading) {
        loading = document.createElement('div');
        loading.id = 'multilayerLoading';
        loading.className = 'loading-indicator';
        document.body.appendChild(loading);
    }
    loading.innerHTML = `<div class="loading-spinner"></div><p>${message || 'Processing...'}</p>`;
    loading.style.display = 'flex';
}

// Hide loading indicator
function hideLoading() {
    const loading = document.getElementById('multilayerLoading');
    if (loading) {
        loading.style.display = 'none';
    }
}

// Toggle multi-layer input section
function toggleMultiLayerInput() {
    const section = document.getElementById('multilayerInputSection');
    if (section) {
        section.style.display = section.style.display === 'none' ? 'block' : 'none';
    }
}

// Expose functions globally
window.initMultiLayerEmotion = initMultiLayerEmotion;
window.toggleConsent = toggleConsent;
window.detectMultiLayerEmotion = detectMultiLayerEmotion;
window.showConsentPanel = showConsentPanel;
window.displayMultiLayerResult = displayMultiLayerResult;
window.toggleMultiLayerInput = toggleMultiLayerInput;
