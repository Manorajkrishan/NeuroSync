// Human OS Dashboard JavaScript

const API_BASE_URL = window.location.origin;
let currentUserId = sessionStorage.getItem('neuroSync_userId') || 'default';

// Initialize dashboard
document.addEventListener('DOMContentLoaded', function() {
    console.log('🧠 Human OS Dashboard Initializing...');
    
    // Set user ID
    document.getElementById('userId').textContent = `User: ${currentUserId}`;
    
    // Load dashboard data
    loadDashboardData();
    
    // Set up refresh button
    document.getElementById('refreshBtn').addEventListener('click', () => {
        loadDashboardData();
    });
    
    // Set up SignalR connection
    initializeSignalR();
    
    // Auto-refresh every 30 seconds
    setInterval(loadDashboardData, 30000);
});

// Load all dashboard data
async function loadDashboardData() {
    try {
        console.log('📊 Loading dashboard data...');
        
        // Load daily summary
        await loadDailySummary();
        
        // Load domain health report
        await loadDomainHealth();
        
        // Load burnout risk
        await loadBurnoutRisk();
        
        // Load growth metrics
        await loadGrowthMetrics();
        
        // Load mental load
        await loadMentalLoad();
        
        // Load life story
        await loadLifeStory();
        
        updateConnectionStatus(true, 'Connected');
        console.log('✅ Dashboard data loaded');
    } catch (error) {
        console.error('❌ Error loading dashboard:', error);
        updateConnectionStatus(false, 'Error loading data');
    }
}

// Load daily emotional summary
async function loadDailySummary() {
    try {
        const response = await fetch(`${API_BASE_URL}/api/dashboard/summary?userId=${currentUserId}`);
        if (!response.ok) throw new Error('Failed to load summary');
        
        const summary = await response.json();
        
        // Update emotional state
        const emotionBadge = document.getElementById('currentEmotion');
        const emotion = summary.currentEmotion?.toLowerCase() || 'neutral';
        emotionBadge.textContent = summary.currentEmotion || 'Neutral';
        emotionBadge.className = `emotion-badge ${emotion}`;
        
        // Update confidence
        document.getElementById('emotionConfidence').textContent = 
            `Confidence: ${(summary.currentEmotionConfidence * 100).toFixed(0)}%`;
        
        // Update trend
        const trend = summary.emotionalTrend || 'Stable';
        document.getElementById('emotionalTrend').textContent = `Trend: ${trend}`;
        const trendIndicator = document.getElementById('trendIndicator');
        trendIndicator.textContent = trend === 'Improving' ? '↑' : trend === 'Declining' ? '↓' : '→';
        trendIndicator.style.color = trend === 'Improving' ? '#10b981' : trend === 'Declining' ? '#ef4444' : '#666';
        
    } catch (error) {
        console.error('Error loading daily summary:', error);
    }
}

// Load domain health report
async function loadDomainHealth() {
    try {
        const response = await fetch(`${API_BASE_URL}/api/domains/health-report?userId=${currentUserId}`);
        if (!response.ok) throw new Error('Failed to load domain health');
        
        const report = await response.json();
        
        // Update each domain card
        report.domains?.forEach(domain => {
            const card = document.querySelector(`[data-domain="${domain.domain}"]`);
            if (card) {
                card.querySelector('.domain-score').textContent = `${domain.emotionalScore.toFixed(0)}`;
                
                const statusEl = card.querySelector('.domain-status');
                statusEl.textContent = domain.riskLevel;
                
                // Update card style based on risk level
                card.className = 'domain-card';
                if (domain.riskLevel === 'Healthy') {
                    card.classList.add('healthy');
                } else if (domain.riskLevel === 'AtRisk') {
                    card.classList.add('at-risk');
                } else if (domain.riskLevel === 'Unhealthy' || domain.riskLevel === 'Crisis') {
                    card.classList.add('unhealthy');
                }
            }
        });
        
        // Update insights
        if (report.recommendations && report.recommendations.length > 0) {
            const insightsList = document.getElementById('keyInsights');
            insightsList.innerHTML = '<ul>' + report.recommendations.map(rec => 
                `<li>${rec}</li>`
            ).join('') + '</ul>';
        }
        
        // Update actions from domain recommendations
        if (report.recommendations && report.recommendations.length > 0) {
            const actionsList = document.getElementById('recommendedActions');
            actionsList.innerHTML = '<ul>' + report.recommendations.slice(0, 5).map(rec => 
                `<li>${rec}</li>`
            ).join('') + '</ul>';
        }
        
    } catch (error) {
        console.error('Error loading domain health:', error);
    }
}

// Load burnout risk
async function loadBurnoutRisk() {
    try {
        const response = await fetch(`${API_BASE_URL}/api/dashboard/burnout-risk?userId=${currentUserId}`);
        if (!response.ok) throw new Error('Failed to load burnout risk');
        
        const risk = await response.json();
        
        // Update risk score
        document.getElementById('burnoutRiskScore').textContent = `${risk.score.toFixed(0)}%`;
        
        // Update risk level
        const levelEl = document.getElementById('burnoutRiskLevel');
        const level = risk.level?.toString().toLowerCase() || 'low';
        levelEl.textContent = risk.level || 'Low';
        levelEl.className = `risk-level ${level}`;
        
        // Update contributing factors
        if (risk.contributingFactors && risk.contributingFactors.length > 0) {
            const factorsEl = document.getElementById('burnoutContributingFactors');
            factorsEl.innerHTML = '<ul>' + risk.contributingFactors.map(factor => 
                `<li>${factor}</li>`
            ).join('') + '</ul>';
        }
        
    } catch (error) {
        console.error('Error loading burnout risk:', error);
    }
}

// Load growth metrics
async function loadGrowthMetrics() {
    try {
        const response = await fetch(`${API_BASE_URL}/api/growth/report?userId=${currentUserId}&months=6`);
        if (!response.ok) throw new Error('Failed to load growth metrics');
        
        const report = await response.json();
        
        // Update maturity score
        document.getElementById('maturityScore').textContent = `${report.maturityScore.toFixed(0)}`;
        
        // Update resilience score
        document.getElementById('resilienceScore').textContent = `${report.resilienceScore.toFixed(0)}`;
        
        // Update trend
        document.getElementById('growthTrend').textContent = 
            `Trend: ${report.maturityTrend || 'Stable'}`;
        
    } catch (error) {
        console.error('Error loading growth metrics:', error);
    }
}

// Load mental load
async function loadMentalLoad() {
    try {
        const response = await fetch(`${API_BASE_URL}/api/dashboard/mental-load?userId=${currentUserId}`);
        if (!response.ok) throw new Error('Failed to load mental load');
        
        const analysis = await response.json();
        
        // Update mental load bar
        const loadBar = document.getElementById('mentalLoadBar');
        const loadValue = analysis.totalMentalLoad || 50;
        loadBar.style.width = `${loadValue}%`;
        document.getElementById('mentalLoadValue').textContent = `${loadValue.toFixed(0)}%`;
        
        // Update overload indicators
        if (analysis.overloadIndicators && analysis.overloadIndicators.length > 0) {
            const indicatorsEl = document.getElementById('overloadIndicators');
            indicatorsEl.innerHTML = '<small style="color: #ef4444;">⚠️ ' + 
                analysis.overloadIndicators.join(' ') + '</small>';
        }
        
    } catch (error) {
        console.error('Error loading mental load:', error);
    }
}

// Load stress and energy from summary
async function loadStressAndEnergy() {
    try {
        const response = await fetch(`${API_BASE_URL}/api/dashboard/summary?userId=${currentUserId}`);
        if (!response.ok) throw new Error('Failed to load summary');
        
        const summary = await response.json();
        
        // Update stress bar
        const stressBar = document.getElementById('stressBar');
        const stressLevel = summary.stressLevel || 50;
        stressBar.style.width = `${stressLevel}%`;
        document.getElementById('stressLevel').textContent = `${stressLevel.toFixed(0)}%`;
        
        // Update energy
        document.getElementById('energyLevel').textContent = `${summary.energyLevel?.toFixed(0) || 50}%`;
        
    } catch (error) {
        console.error('Error loading stress and energy:', error);
    }
}

// Load life story
async function loadLifeStory() {
    try {
        const response = await fetch(`${API_BASE_URL}/api/memory/story?userId=${currentUserId}&months=12`);
        if (!response.ok) throw new Error('Failed to load life story');
        
        const data = await response.json();
        
        if (data.story) {
            document.getElementById('lifeStory').innerHTML = `<p>${data.story}</p>`;
        }
        
    } catch (error) {
        console.error('Error loading life story:', error);
    }
}

// Update connection status
function updateConnectionStatus(connected, message) {
    const statusDot = document.getElementById('connectionStatus');
    const statusText = document.getElementById('connectionText');
    
    if (connected) {
        statusDot.classList.add('connected');
        statusDot.classList.remove('offline');
        statusText.textContent = message || 'Connected';
    } else {
        statusDot.classList.remove('connected');
        statusDot.classList.add('offline');
        statusText.textContent = message || 'Disconnected';
    }
}

// Initialize SignalR
function initializeSignalR() {
    if (typeof signalR === 'undefined') {
        console.warn('SignalR not loaded');
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(`${API_BASE_URL}/emotionHub`)
        .withAutomaticReconnect()
        .build();

    connection.onreconnecting(() => {
        updateConnectionStatus(false, 'Reconnecting...');
    });

    connection.onreconnected(() => {
        updateConnectionStatus(true, 'Connected');
    });

    connection.onclose(() => {
        updateConnectionStatus(false, 'Disconnected');
    });

    connection.start()
        .then(() => {
            updateConnectionStatus(true, 'Connected');
            console.log('✅ SignalR connected');
        })
        .catch(err => {
            console.error('❌ SignalR connection failed:', err);
            updateConnectionStatus(false, 'Connection failed');
        });
}

// Also load stress/energy when loading summary
const originalLoadDailySummary = loadDailySummary;
loadDailySummary = async function() {
    await originalLoadDailySummary();
    await loadStressAndEnergy();
};
