// Cross-device sync: same owner memory on phone, PC, and any browser
(function () {
    const API = window.location.origin;
    const LS_USER = 'neuroSync_userId';
    const LS_DEVICE = 'neuroSync_deviceId';

    function detectPlatform() {
        const ua = navigator.userAgent || '';
        if (/iPhone|iPad|iPod/i.test(ua)) return 'ios';
        if (/Android/i.test(ua)) return 'android';
        if (/Windows/i.test(ua)) return 'windows';
        if (/Mac/i.test(ua)) return 'mac';
        return 'web';
    }

    function deviceName() {
        const p = detectPlatform();
        return ({ ios: 'iPhone', android: 'Android', windows: 'Windows PC', mac: 'Mac', web: 'Web' })[p] || 'Device';
    }

    function getUserId() {
        let id = localStorage.getItem(LS_USER) || sessionStorage.getItem(LS_USER);
        if (!id || id === 'default') {
            id = 'user_' + Date.now().toString(36) + '_' + Math.random().toString(36).slice(2, 8);
            localStorage.setItem(LS_USER, id);
        }
        sessionStorage.setItem(LS_USER, id);
        return id;
    }

    function setUserId(id) {
        localStorage.setItem(LS_USER, id);
        sessionStorage.setItem(LS_USER, id);
        if (typeof currentUserId !== 'undefined') currentUserId = id;
        window.currentUserId = id;
    }

    function getDeviceId() {
        let id = localStorage.getItem(LS_DEVICE);
        if (!id) {
            id = 'dev_' + Math.random().toString(36).slice(2, 10);
            localStorage.setItem(LS_DEVICE, id);
        }
        return id;
    }

    async function registerAndSync() {
        const userId = getUserId();
        const body = {
            deviceId: getDeviceId(),
            deviceName: deviceName(),
            platform: detectPlatform(),
            userAgent: navigator.userAgent
        };
        try {
            const r = await fetch(`${API}/api/devices/register?userId=${encodeURIComponent(userId)}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body)
            });
            if (!r.ok) return null;
            const data = await r.json();
            if (data.device?.deviceId) localStorage.setItem(LS_DEVICE, data.device.deviceId);
            updateSyncUi(data.sync);
            return data.sync;
        } catch (e) {
            console.warn('Device sync failed', e);
            return null;
        }
    }

    async function createPairCode() {
        const userId = getUserId();
        const r = await fetch(`${API}/api/devices/pair/create?userId=${encodeURIComponent(userId)}`, { method: 'POST' });
        if (!r.ok) throw new Error('Could not create code');
        return r.json();
    }

    async function joinPairCode(code) {
        const r = await fetch(`${API}/api/devices/pair/join`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                pairingCode: code.trim(),
                deviceId: getDeviceId(),
                deviceName: deviceName(),
                platform: detectPlatform(),
                userAgent: navigator.userAgent
            })
        });
        if (!r.ok) {
            const err = await r.json().catch(() => ({}));
            throw new Error(err.error || 'Invalid code');
        }
        const sync = await r.json();
        setUserId(sync.userId);
        if (sync.currentDeviceId) localStorage.setItem(LS_DEVICE, sync.currentDeviceId);
        updateSyncUi(sync);
        return sync;
    }

    function updateSyncUi(sync) {
        if (!sync) return;
        const status = document.getElementById('syncStatus');
        const devicesEl = document.getElementById('syncDevices');
        const knowEl = document.getElementById('syncKnow');
        if (status) {
            status.textContent = sync.message || `Synced as ${sync.preferredName || sync.userId}`;
        }
        if (devicesEl) {
            const list = (sync.devices || []).map(d =>
                `<li><strong>${d.deviceName}</strong> · ${d.platform} · ${new Date(d.lastSeenUtc).toLocaleString()}</li>`
            ).join('');
            devicesEl.innerHTML = list || '<li>This device only</li>';
        }
        if (knowEl) {
            knowEl.textContent = sync.whatIKnow || 'Still learning about you…';
        }
        const badge = document.getElementById('deviceSyncBadge');
        if (badge) {
            const n = (sync.devices || []).length || 1;
            badge.textContent = n > 1 ? `${n} devices` : '1 device';
            badge.style.display = 'inline-flex';
        }
    }

    function ensureSyncPanel() {
        if (document.getElementById('deviceSyncPanel')) return;
        const panel = document.createElement('div');
        panel.id = 'deviceSyncPanel';
        panel.className = 'device-sync-panel';
        panel.innerHTML = `
            <button type="button" id="syncToggleBtn" class="sync-toggle" title="Sync devices">📱 Sync</button>
            <div id="syncDrawer" class="sync-drawer" hidden>
                <h3>Sync across devices</h3>
                <p id="syncStatus">Connecting…</p>
                <p class="sync-know"><span>I know:</span> <em id="syncKnow">…</em></p>
                <ul id="syncDevices"></ul>
                <div class="sync-actions">
                    <button type="button" id="btnCreatePair">Show code (this device)</button>
                    <div class="sync-join">
                        <input id="pairCodeInput" maxlength="6" placeholder="6-digit code" inputmode="numeric" />
                        <button type="button" id="btnJoinPair">Join</button>
                    </div>
                </div>
                <p id="pairCodeDisplay" class="pair-code" hidden></p>
                <p class="sync-hint">On your phone: open this site → Sync → enter the code from your PC (same Wi‑Fi).</p>
            </div>`;
        document.body.appendChild(panel);

        const style = document.createElement('style');
        style.textContent = `
            .device-sync-panel { position: fixed; bottom: 16px; right: 16px; z-index: 9998; font-family: system-ui, sans-serif; }
            .sync-toggle { background: #1a1a2e; color: #fff; border: none; border-radius: 999px; padding: 10px 16px; cursor: pointer; box-shadow: 0 4px 14px rgba(0,0,0,.25); }
            .sync-drawer { margin-top: 8px; width: min(320px, 92vw); background: #fff; color: #222; border-radius: 14px; padding: 14px; box-shadow: 0 8px 28px rgba(0,0,0,.2); }
            .sync-drawer h3 { margin: 0 0 8px; font-size: 1rem; }
            .sync-drawer ul { margin: 8px 0; padding-left: 18px; font-size: .85rem; max-height: 120px; overflow: auto; }
            .sync-actions { display: flex; flex-direction: column; gap: 8px; margin-top: 10px; }
            .sync-actions button, .sync-join button { padding: 8px 12px; border-radius: 8px; border: 1px solid #ccc; background: #f4f4f8; cursor: pointer; }
            .sync-join { display: flex; gap: 6px; }
            .sync-join input { flex: 1; padding: 8px; border-radius: 8px; border: 1px solid #ccc; letter-spacing: .2em; text-align: center; font-size: 1.1rem; }
            .pair-code { font-size: 1.8rem; font-weight: 700; letter-spacing: .25em; text-align: center; margin: 10px 0 0; color: #4f46e5; }
            .sync-hint { font-size: .75rem; color: #666; margin: 10px 0 0; }
            .sync-know { font-size: .85rem; margin: 6px 0; }
            #deviceSyncBadge { margin-left: 8px; font-size: .75rem; opacity: .85; }
        `;
        document.head.appendChild(style);

        document.getElementById('syncToggleBtn').onclick = () => {
            const d = document.getElementById('syncDrawer');
            d.hidden = !d.hidden;
            if (!d.hidden) registerAndSync();
        };
        document.getElementById('btnCreatePair').onclick = async () => {
            try {
                const data = await createPairCode();
                const el = document.getElementById('pairCodeDisplay');
                el.hidden = false;
                el.textContent = data.pairingCode;
            } catch (e) {
                alert(e.message || 'Failed to create code');
            }
        };
        document.getElementById('btnJoinPair').onclick = async () => {
            const code = document.getElementById('pairCodeInput').value;
            try {
                const sync = await joinPairCode(code);
                alert(sync.message || 'Synced!');
                location.reload();
            } catch (e) {
                alert(e.message || 'Join failed');
            }
        };
    }

    // Align companion/app user id to localStorage
    window.NeuroSyncDeviceSync = { getUserId, setUserId, registerAndSync, createPairCode, joinPairCode };

    document.addEventListener('DOMContentLoaded', () => {
        getUserId();
        ensureSyncPanel();
        registerAndSync();
        setInterval(() => {
            const userId = getUserId();
            const deviceId = getDeviceId();
            fetch(`${API}/api/devices/heartbeat?userId=${encodeURIComponent(userId)}&deviceId=${encodeURIComponent(deviceId)}`, { method: 'POST' }).catch(() => {});
        }, 60000);
    });
})();
