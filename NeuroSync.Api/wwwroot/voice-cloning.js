// Voice Cloning functionality

// Handle voice clone file upload
window.handleVoiceCloneFile = async function(event) {
    const file = event.target.files[0];
    if (!file) return;

    const personName = document.getElementById('voiceClonePersonName').value.trim();
    if (!personName) {
        alert('Please enter the person\'s name first');
        event.target.value = ''; // Reset file input
        return;
    }

    const statusDiv = document.getElementById('voiceCloneStatus');
    statusDiv.textContent = 'Uploading and cloning voice...';
    statusDiv.style.color = '#3b82f6';

    try {
        const userId = sessionStorage.getItem('neuroSync_userId') || 'default';
        const formData = new FormData();
        formData.append('userId', userId);
        formData.append('personName', personName);
        formData.append('audioFile', file);

        const response = await fetch(`${API_BASE_URL}/api/voice/clone`, {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            const result = await response.json();
            statusDiv.textContent = `✅ ${result.message || 'Voice cloned successfully!'}`;
            statusDiv.style.color = '#10b981';
            
            // Clear inputs
            event.target.value = '';
            document.getElementById('voiceClonePersonName').value = '';
            
            // Reload cloned voices list
            loadClonedVoices();
        } else {
            const error = await response.json();
            statusDiv.textContent = `❌ Error: ${error.error || 'Failed to clone voice'}`;
            statusDiv.style.color = '#ef4444';
        }
    } catch (error) {
        console.error('Error cloning voice:', error);
        statusDiv.textContent = `❌ Error: ${error.message}`;
        statusDiv.style.color = '#ef4444';
    }
};

// Load cloned voices for user
window.loadClonedVoices = async function() {
    const userId = sessionStorage.getItem('neuroSync_userId') || 'default';
    
    try {
        const response = await fetch(`${API_BASE_URL}/api/voice/clones?userId=${userId}`);
        if (response.ok) {
            const data = await response.json();
            displayClonedVoices(data.voices || []);
        }
    } catch (error) {
        console.error('Error loading cloned voices:', error);
    }
};

// Display cloned voices
function displayClonedVoices(voices) {
    const list = document.getElementById('clonedVoicesList');
    if (!list) return;

    if (!voices || voices.length === 0) {
        list.innerHTML = '<p style="color: var(--text-secondary); font-size: 0.9rem;">No cloned voices yet. Upload a voice sample to get started!</p>';
        return;
    }

    list.innerHTML = voices.map(voice => `
        <div class="voice-note-item" style="padding: 10px; border: 1px solid var(--border-color); border-radius: 5px; margin-bottom: 10px;">
            <strong>${voice.personName}</strong>
            <small style="display: block; color: var(--text-secondary); margin-top: 5px;">
                Cloned on ${new Date(voice.createdAt).toLocaleDateString()}
            </small>
            <button onclick="useClonedVoice('${voice.voiceId}', '${voice.personName}')" class="play-voice-btn" style="margin-top: 5px;">
                🎭 Use This Voice
            </button>
        </div>
    `).join('');
}

// Use cloned voice for TTS
window.useClonedVoice = function(voiceId, personName) {
    localStorage.setItem('neuroSync_selected_voice_id', voiceId);
    localStorage.setItem('neuroSync_selected_voice_person', personName);
    
    alert(`Voice set to ${personName}'s voice! The AI will now speak in this voice when responding.`);
};

// Load cloned voices on page load
window.addEventListener('load', () => {
    if (typeof loadClonedVoices === 'function') {
        loadClonedVoices();
    } else if (typeof window.loadClonedVoices === 'function') {
        window.loadClonedVoices();
    }
});
