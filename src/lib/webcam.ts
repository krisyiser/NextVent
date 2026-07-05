import { invoke } from '@tauri-apps/api/core';

export const captureAuditImage = async (eventId: string): Promise<boolean> => {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ video: true });
        const video = document.createElement('video');
        video.srcObject = stream;
        await video.play();

        const canvas = document.createElement('canvas');
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        
        const ctx = canvas.getContext('2d');
        if (ctx) {
            ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
            const base64Image = canvas.toDataURL('image/png');
            
            // Stop tracks
            stream.getTracks().forEach(track => track.stop());
            
            // Save to FS via Tauri IPC
            await invoke('save_audit_image', { eventId, base64Image });
            return true;
        }
        return false;
    } catch (e) {
        console.error("No webcam available for audit", e);
        return false; // Silently fail if no webcam to avoid blocking ops
    }
};
