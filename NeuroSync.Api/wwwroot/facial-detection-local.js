// Alternative version that loads face-api.js models from local files
// Use this if you've downloaded models using download-face-api-models.ps1

// Initialize face-api.js models from LOCAL files
async function loadFaceAPIModelsLocal() {
    try {
        // Check if face-api is loaded
        if (typeof faceapi === 'undefined') {
            console.error('face-api.js is not loaded');
            return false;
        }
        
        console.log('Loading face-api.js models from LOCAL files...');
        
        // Load from local project folder (must be in wwwroot/models/face-api/)
        const LOCAL_MODEL_URL = '/models/face-api';
        
        try {
            await Promise.all([
                faceapi.nets.tinyFaceDetector.loadFromUri(LOCAL_MODEL_URL),
                faceapi.nets.faceLandmark68Net.loadFromUri(LOCAL_MODEL_URL),
                faceapi.nets.faceRecognitionNet.loadFromUri(LOCAL_MODEL_URL),
                faceapi.nets.faceExpressionNet.loadFromUri(LOCAL_MODEL_URL)
            ]);
            
            modelsLoaded = true;
            console.log('Face-api.js models loaded successfully from LOCAL files');
            return true;
        } catch (error) {
            console.error('Failed to load local models, falling back to internet...', error);
            // Fallback to internet loading
            return await loadFaceAPIModels(); // Use original function
        }
    } catch (error) {
        console.error('Error loading face-api.js models:', error);
        alert('Failed to load facial detection models. Please check your internet connection.');
        return false;
    }
}

// Original function (loads from internet)
async function loadFaceAPIModels() {
    try {
        // Check if face-api is loaded
        if (typeof faceapi === 'undefined') {
            console.error('face-api.js is not loaded');
            return false;
        }
        
        console.log('Loading face-api.js models from internet...');
        
        // Load models from GitHub raw (most reliable source)
        const MODEL_URL = 'https://raw.githubusercontent.com/justadudewhohacks/face-api.js/master/weights';
        
        try {
            await Promise.all([
                faceapi.nets.tinyFaceDetector.loadFromUri(MODEL_URL),
                faceapi.nets.faceLandmark68Net.loadFromUri(MODEL_URL),
                faceapi.nets.faceRecognitionNet.loadFromUri(MODEL_URL),
                faceapi.nets.faceExpressionNet.loadFromUri(MODEL_URL)
            ]);
            
            modelsLoaded = true;
            console.log('Face-api.js models loaded successfully');
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
                console.log('Face-api.js models loaded successfully from alternative source');
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

