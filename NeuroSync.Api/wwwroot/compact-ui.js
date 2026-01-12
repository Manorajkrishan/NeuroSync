// Compact UI JavaScript

// Toggle collapsible sections
window.toggleCollapsible = function(sectionId) {
    const content = document.getElementById(sectionId);
    const arrow = document.getElementById(sectionId + 'Arrow');
    
    if (!content) return;
    
    content.classList.toggle('expanded');
    
    if (arrow) {
        arrow.textContent = content.classList.contains('expanded') ? '▼' : '▶';
    }
};

// Update avatar based on emotion
window.updateAvatar = function(emotion) {
    const avatar = document.getElementById('mainAvatar');
    if (!avatar) return;
    
    // Remove all emotion classes
    avatar.className = 'avatar-display';
    
    // Add emotion class
    const emotionClass = emotion?.toLowerCase() || 'neutral';
    avatar.classList.add(emotionClass);
    
    // Set avatar emoji based on emotion
    const emojiMap = {
        'happy': '😊',
        'sad': '😢',
        'calm': '😌',
        'excited': '😄',
        'anxious': '😰',
        'angry': '😠',
        'frustrated': '😤',
        'neutral': '👶'
    };
    
    const emoji = emojiMap[emotionClass] || '👶';
    avatar.textContent = emoji;
};

// Add message to chat
window.addChatMessage = function(message, isAI = true, emotion = null) {
    const chatMessages = document.getElementById('chatMessages');
    if (!chatMessages) return;
    
    const messageDiv = document.createElement('div');
    messageDiv.className = `chat-message ${isAI ? 'ai-message' : 'user-message'}`;
    
    const avatar = document.createElement('div');
    avatar.className = 'message-avatar';
    avatar.textContent = isAI ? (emotion ? getEmotionEmoji(emotion) : '👶') : '👤';
    
    const bubble = document.createElement('div');
    bubble.className = 'message-bubble';
    
    const messageText = document.createElement('div');
    messageText.textContent = message;
    bubble.appendChild(messageText);
    
    const time = document.createElement('div');
    time.className = 'message-time';
    time.textContent = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    bubble.appendChild(time);
    
    messageDiv.appendChild(avatar);
    messageDiv.appendChild(bubble);
    
    chatMessages.appendChild(messageDiv);
    
    // Scroll to bottom
    chatMessages.scrollTop = chatMessages.scrollHeight;
    
    // Update main avatar if AI message
    if (isAI && emotion) {
        updateAvatar(emotion);
    }
};

// Get emoji for emotion
function getEmotionEmoji(emotion) {
    const emojiMap = {
        'happy': '😊',
        'sad': '😢',
        'calm': '😌',
        'excited': '😄',
        'anxious': '😰',
        'angry': '😠',
        'frustrated': '😤',
        'neutral': '👶'
    };
    
    return emojiMap[emotion?.toLowerCase()] || '👶';
}

// Update status indicator
window.updateStatusIndicator = function(connected) {
    const indicator = document.getElementById('statusIndicator');
    if (indicator) {
        indicator.style.background = connected ? '#10b981' : '#ef4444';
    }
};

// Auto-resize textarea
window.addEventListener('DOMContentLoaded', function() {
    const input = document.getElementById('emotionInput');
    if (input) {
        input.addEventListener('input', function() {
            this.style.height = 'auto';
            this.style.height = Math.min(this.scrollHeight, 120) + 'px';
        });
        
        // Enter to send, Shift+Enter for new line
        input.addEventListener('keydown', function(e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                detectEmotion();
            }
        });
    }
    
    // Initialize avatar
    updateAvatar('neutral');
    
    // Load voice notes
    if (typeof loadVoiceNotes === 'function') {
        loadVoiceNotes();
    } else if (typeof window.loadVoiceNotes === 'function') {
        window.loadVoiceNotes();
    }
});
