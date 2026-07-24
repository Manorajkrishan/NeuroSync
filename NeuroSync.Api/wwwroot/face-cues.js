// Shared facial cues: eye contact + face motion (face-api.js 68 landmarks)
(function (global) {
    let prevNose = null;
    let prevTs = 0;
    let motionEma = 0;

    function mid(a, b) {
        return { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 };
    }

    function dist(a, b) {
        const dx = a.x - b.x, dy = a.y - b.y;
        return Math.sqrt(dx * dx + dy * dy);
    }

    /** Eye Aspect Ratio — low = eyes closing / looking down */
    function eyeAspectRatio(eyePts) {
        if (!eyePts || eyePts.length < 6) return 0.3;
        const v1 = dist(eyePts[1], eyePts[5]);
        const v2 = dist(eyePts[2], eyePts[4]);
        const h = dist(eyePts[0], eyePts[3]);
        if (h < 1e-6) return 0.3;
        return (v1 + v2) / (2 * h);
    }

    /**
     * Analyze landmarks for eye contact + head/face motion.
     * @returns {{ eyeContact: number, faceMotion: number, gaze: string, engagement: string, notes: string }}
     */
    function analyzeFaceCues(landmarks, box) {
        const positions = landmarks.positions || landmarks;
        if (!positions || positions.length < 68) {
            return { eyeContact: 0.5, faceMotion: 0, gaze: 'unknown', engagement: 'low', notes: 'Face unclear' };
        }

        const leftEye = positions.slice(36, 42);
        const rightEye = positions.slice(42, 48);
        const nose = positions[30];
        const leftEyeC = mid(leftEye[0], leftEye[3]);
        const rightEyeC = mid(rightEye[0], rightEye[3]);
        const eyesMid = mid(leftEyeC, rightEyeC);

        const earL = eyeAspectRatio(leftEye);
        const earR = eyeAspectRatio(rightEye);
        const ear = (earL + earR) / 2;

        // Yaw proxy: nose offset from eye midline relative to face width
        const faceW = box ? box.width : dist(positions[0], positions[16]);
        const noseOffset = (nose.x - eyesMid.x) / Math.max(faceW, 1);
        const absYaw = Math.abs(noseOffset);

        // Looking at camera ≈ eyes open + face facing forward
        let eyeContact = 1.0;
        if (ear < 0.18) eyeContact -= 0.55;      // eyes mostly closed
        else if (ear < 0.22) eyeContact -= 0.25;
        if (absYaw > 0.18) eyeContact -= 0.45;   // looking aside
        else if (absYaw > 0.10) eyeContact -= 0.2;
        eyeContact = Math.max(0, Math.min(1, eyeContact));

        // Face / head motion from nose travel
        const now = Date.now();
        let faceMotion = 0;
        if (prevNose && prevTs) {
            const dt = Math.max(0.05, (now - prevTs) / 1000);
            const speed = dist(nose, prevNose) / dt;
            const norm = Math.min(1, speed / (faceW * 2.5));
            motionEma = motionEma * 0.7 + norm * 0.3;
            faceMotion = motionEma;
        }
        prevNose = { x: nose.x, y: nose.y };
        prevTs = now;

        let gaze = 'looking_at_you';
        if (ear < 0.18) gaze = 'eyes_closed_or_down';
        else if (absYaw > 0.15) gaze = noseOffset > 0 ? 'looking_right' : 'looking_left';
        else if (eyeContact > 0.7) gaze = 'looking_at_you';
        else gaze = 'partial_attention';

        let engagement = 'steady';
        if (faceMotion > 0.55) engagement = 'restless';
        else if (faceMotion < 0.08 && eyeContact > 0.75) engagement = 'calm_focused';
        else if (eyeContact < 0.35) engagement = 'disengaged';

        const notes = [
            gaze === 'looking_at_you' ? 'Eye contact feels present' : `Gaze: ${gaze.replace(/_/g, ' ')}`,
            faceMotion > 0.45 ? 'A lot of face/head movement' : faceMotion < 0.1 ? 'Face is quite still' : 'Natural movement',
            ear < 0.2 ? 'Eyes look heavy or closed' : null
        ].filter(Boolean).join(' · ');

        return {
            eyeContact: Math.round(eyeContact * 100) / 100,
            faceMotion: Math.round(faceMotion * 100) / 100,
            gaze,
            engagement,
            notes,
            ear: Math.round(ear * 100) / 100
        };
    }

    global.NeuroSyncFaceCues = { analyzeFaceCues };
})(typeof window !== 'undefined' ? window : globalThis);
