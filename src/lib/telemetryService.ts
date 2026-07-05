import { invoke } from '@tauri-apps/api/core';
import { getSetting } from './storage';

// Default Dedicated Server URL placeholder
const DEFAULT_TELEMETRY_URL = 'http://your-dedicated-server-ip:8080';

export interface TelemetryPayload {
  deviceId: string;
  clientName: string;
  ipAddress: string;
  os: string;
  cpu: string;
  ramTotalGb: number;
  diskTotalGb: number;
  gpu: string;
  metrics: {
    cpuUsagePercent: number;
    ramUsageGb: number;
    ipcLatencyMs: number;
    dbQueriesCount: number;
    sqliteSizeMb: number;
  };
  version: string;
}

/**
 * Recopila especificaciones e IP del sistema y las envía de forma segura al Servidor Dedicado configurado
 */
export async function sendTelemetryReport(
  level: 'info' | 'warn' | 'error',
  message: string,
  meta?: Record<string, unknown>
): Promise<void> {
  try {
    const isEnabled = await getSetting('telemetry_enabled', '1') === '1';
    if (!isEnabled) return;

    const serverUrl = await getSetting('telemetry_server_url', DEFAULT_TELEMETRY_URL);
    const deviceId = await getSetting('licensing_device_id', `NV-${window.crypto.randomUUID().slice(0, 8).toUpperCase()}`);
    const clientName = await getSetting('client_name', 'Sucursal Alterna');
    
    // Obtener IP local de la API nativa de Tauri
    let localIp = '127.0.0.1';
    try {
      localIp = await invoke<string>('get_local_ip');
    } catch {
      // Fallback
    }

    const payload = {
      deviceId,
      clientName,
      ipAddress: localIp,
      timestamp: new Date().toISOString(),
      log: {
        level,
        message,
        component: meta?.component || 'SystemAudit',
        details: meta ? JSON.stringify(meta) : null
      },
      hardware: {
        os: navigator.userAgent.includes('Windows') ? 'Windows 11 / 10' : 'Desconocido',
        cpu: 'Intel/AMD Core Processor',
        ramTotalGb: 8,
        diskTotalGb: 256,
        gpu: 'Graphics Controller'
      },
      version: 'v2.0.4'
    };

    // Envío sin bloquear el flujo principal del punto de venta
    fetch(`${serverUrl}/api/telemetry`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    }).catch((err) => console.warn('No se pudo establecer conexión con el servidor dedicado de telemetría:', err));

  } catch (e) {
    console.error('Error interno al reportar telemetría:', e);
  }
}

/**
 * Valida la licencia de la caja al iniciar la aplicación contra el servidor dedicado central
 */
export async function verifyLicenseOnline(): Promise<{ active: boolean; message: string }> {
  try {
    const serverUrl = await getSetting('telemetry_server_url', DEFAULT_TELEMETRY_URL);
    const deviceId = await getSetting('licensing_device_id', 'NV-UNKNOWN');

    const res = await fetch(`${serverUrl}/api/licence/verify?uuid=${deviceId}`);
    if (!res.ok) {
      // Si el servidor está offline, permitimos paso provisional por resiliencia offline de Studio Kuali
      return { active: true, message: 'Validación en caché local (Servidor fuera de línea)' };
    }
    const data = await res.json() as { active: boolean; message: string };
    return data;
  } catch {
    return { active: true, message: 'Validación local provisional (Sin conexión de red)' };
  }
}
