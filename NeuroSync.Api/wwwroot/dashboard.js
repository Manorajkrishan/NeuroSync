// Human OS Dashboard JavaScript

const API_BASE_URL = window.location.origin;
let currentUserId = localStorage.getItem('neuroSync_userId') || sessionStorage.getItem('neuroSync_userId') || 'default';
if (currentUserId && currentUserId !== 'default') {
    localStorage.setItem('neuroSync_userId', currentUserId);
    sessionStorage.setItem('neuroSync_userId', currentUserId);
}

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

    // Add decision
    var addDec = document.getElementById('addDecisionBtn');
    var newDec = document.getElementById('newDecisionText');
    if (addDec && newDec) {
        addDec.addEventListener('click', async function () {
            var t = (newDec.value || '').trim();
            if (!t) return;
            addDec.disabled = true;
            try {
                var r = await fetch(API_BASE_URL + '/api/decisions/frame', {
                    method: 'POST', headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ userId: currentUserId, decisionText: t })
                });
                if (r.ok) { newDec.value = ''; await loadRecentDecisions(); await loadDomainHealth(); }
                else console.error('Failed to add decision', await r.text());
            } finally { addDec.disabled = false; }
        });
    }

    // Add life event
    var addEvt = document.getElementById('addEventBtn');
    var newEvtDesc = document.getElementById('newEventDesc');
    var newEvtType = document.getElementById('newEventType');
    if (addEvt && newEvtDesc && newEvtType) {
        addEvt.addEventListener('click', async function () {
            var d = (newEvtDesc.value || '').trim();
            if (!d) return;
            addEvt.disabled = true;
            try {
                var r = await fetch(API_BASE_URL + '/api/memory/event', {
                    method: 'POST', headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ userId: currentUserId, description: d, eventType: parseInt(newEvtType.value, 10) || 1, emotionalSignificance: 50, lifeImpact: 1 })
                });
                if (r.ok) { newEvtDesc.value = ''; await loadLifeStory(); }
                else console.error('Failed to add event', await r.text());
            } finally { addEvt.disabled = false; }
        });
    }

    // Adjust domain
    var adjBtn = document.getElementById('adjustDomainBtn');
    var adjDom = document.getElementById('adjustDomain');
    var adjScore = document.getElementById('adjustScore');
    var adjStress = document.getElementById('adjustStress');
    if (adjBtn && adjDom && adjScore && adjStress) {
        adjBtn.addEventListener('click', async function () {
            var dom = adjDom.value;
            var sc = parseInt(adjScore.value, 10); var st = parseInt(adjStress.value, 10);
            if (isNaN(sc)) sc = 50; if (isNaN(st)) st = 30;
            adjBtn.disabled = true;
            try {
                var r = await fetch(API_BASE_URL + '/api/domains/state/' + dom + '?userId=' + encodeURIComponent(currentUserId), {
                    method: 'PUT', headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ emotionalScore: sc, stressLevel: st })
                });
                if (r.ok) { await loadDomainHealth(); await loadDailySummary(); await loadStressAndEnergy(); }
                else console.error('Failed to update domain', await r.text());
            } finally { adjBtn.disabled = false; }
        });
    }
    
    // Set up SignalR connection
    initializeSignalR();
    
    // Auto-refresh every 30 seconds
    setInterval(loadDashboardData, 30000);
});

// Show loading state for dashboard sections
function setDashboardLoading(loading) {
    const main = document.querySelector('.dashboard-main');
    if (main) main.classList.toggle('dashboard-loading', !!loading);
    const statusText = document.getElementById('connectionText');
    if (statusText && loading) statusText.textContent = 'Loading...';
    document.body.classList.toggle('dashboard-loading', !!loading);
}

// Show user-visible error message (toast or inline)
function showDashboardError(message) {
    const existing = document.getElementById('dashboardErrorMessage');
    if (existing) existing.remove();
    const el = document.createElement('div');
    el.id = 'dashboardErrorMessage';
    el.setAttribute('role', 'alert');
    el.className = 'dashboard-error-message';
    el.textContent = message || 'Failed to load dashboard. Please refresh.';
    document.body.prepend(el);
    setTimeout(() => el.remove(), 6000);
}

// Load all dashboard data
async function loadDashboardData() {
    setDashboardLoading(true);
    const errEl = document.getElementById('dashboardErrorMessage');
    if (errEl) errEl.remove();
    try {
        console.log('📊 Loading dashboard data...');
        
        await loadDailySummary();
        await loadDomainHealth();
        await loadBurnoutRisk();
        await loadGrowthMetrics();
        await loadMentalLoad();
        await loadLifeStory();
        await loadRecentDecisions();
        
        updateConnectionStatus(true, 'Connected');
        console.log('✅ Dashboard data loaded');
    } catch (error) {
        console.error('❌ Error loading dashboard:', error);
        updateConnectionStatus(false, 'Error loading data');
        showDashboardError('Could not load dashboard. Check connection and try again.');
    } finally {
        setDashboardLoading(false);
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

// Load recent decisions
async function loadRecentDecisions() {
    try {
        const response = await fetch(`${API_BASE_URL}/api/decisions/recent?userId=${currentUserId}&limit=5`);
        if (!response.ok) return;
        const list = await response.json();
        const el = document.getElementById('recentDecisions');
        if (!el) return;
        if (!list || list.length === 0) {
            el.innerHTML = '<p class="empty">No recent decisions</p>';
            return;
        }
        el.innerHTML = '<ul>' + list.map(d => {
            var t = (d.decisionText || '').toString();
            return '<li><strong>' + t.substring(0, 60) + (t.length > 60 ? '...' : '') + '</strong> <small>(' + (d.decisionType || '') + ', ' + (d.createdAt ? new Date(d.createdAt).toLocaleDateString() : '') + ')</small></li>';
        }).join('') + '</ul>';
    } catch (e) { console.error('Error loading recent decisions', e); }
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

    connection.on('EmotionDetected', function (data) {
        if (data && data.emotion != null) {
            var em = (typeof data.emotion === 'number' ? ['Happy','Sad','Angry','Anxious','Calm','Excited','Frustrated','Neutral'][data.emotion] : data.emotion) || 'Neutral';
            var conf = (data.confidence != null ? (data.confidence * 100).toFixed(0) : '--');
            var badge = document.getElementById('currentEmotion');
            var cf = document.getElementById('emotionConfidence');
            if (badge) { badge.textContent = em; badge.className = 'emotion-badge ' + (em.toLowerCase()); }
            if (cf) cf.textContent = 'Confidence: ' + conf + '%';
        }
    });

    connection.on('AdaptiveResponse', function (data) {
        if (data && data.message) { /* optional: show a brief "Last: ..." tooltip; for now we rely on EmotionDetected and poll */ }
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
