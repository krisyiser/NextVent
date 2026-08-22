/**
 * Valcore Desktop Download Architecture & Version Detector v1.1
 * Protocol Valcore v4.0 - Dynamic Release Sync for valcore.cloud
 */
(function () {
  'use strict';

  // 1. Fetch latest release info from releases.json dynamically
  async function fetchLatestReleaseInfo() {
    try {
      const res = await fetch('https://valcore.cloud/downloads/releases.json', { cache: 'no-cache' });
      if (res.ok) {
        const data = await res.json();
        return data; // { version: "3.0.39", downloads: { x64: "...", x86: "..." } }
      }
    } catch (e) {
      console.warn('Valcore Release Sync: Could not fetch releases.json, using fallback', e);
    }
    return {
      version: '3.0.39',
      downloads: {
        x64: 'Ticketfy-Setup-v3.0.39-x64.exe',
        x86: 'Ticketfy-Setup-v3.0.39-x86.exe',
        default: 'Ticketfy-Setup-v3.0.39.exe'
      }
    };
  }

  // 2. Detect 64-bit vs 32-bit architecture
  async function detectWindowsArchitecture() {
    let is64Bit = true;
    try {
      if (navigator.userAgentData && typeof navigator.userAgentData.getHighEntropyValues === 'function') {
        const uaValues = await navigator.userAgentData.getHighEntropyValues(['architecture', 'bitness']);
        if (uaValues.bitness === '64' || uaValues.architecture === 'x86_64' || uaValues.architecture === 'arm64') {
          is64Bit = true;
        } else if (uaValues.bitness === '32' || uaValues.architecture === 'x86') {
          is64Bit = false;
        }
      } else {
        const ua = (navigator.userAgent || '').toLowerCase();
        const platform = (navigator.platform || '').toLowerCase();
        if (ua.includes('win64') || ua.includes('x64') || ua.includes('wow64') || platform.includes('win64') || platform.includes('x64')) {
          is64Bit = true;
        } else if (ua.includes('win32') || ua.includes('x86') || platform === 'win32') {
          is64Bit = false;
        }
      }
    } catch (e) {
      console.warn('Valcore Detector: userAgentData query failed, using x64', e);
    }
    return is64Bit ? 'x64' : 'x86';
  }

  // 3. Auto-update button text and href on valcore.cloud
  async function syncValcoreWebsite() {
    const release = await fetchLatestReleaseInfo();
    const arch = await detectWindowsArchitecture();
    const versionStr = release.version || '3.0.39';

    const filename = release.downloads[arch] || `Ticketfy-Setup-v${versionStr}-${arch}.exe`;
    const downloadUrl = `https://valcore.cloud/downloads/${filename}`;

    // Update any download button on valcore.cloud
    const downloadButtons = document.querySelectorAll('a[href*="Ticketfy-Setup"], #valcore-main-download-btn, .btn-download, a.download-btn');
    
    downloadButtons.forEach(btn => {
      btn.href = downloadUrl;
      
      // Update text dynamically to current version
      if (btn.innerText.includes('DESCARGAR') || btn.innerText.includes('V3.') || btn.innerText.includes('v3.')) {
        btn.innerHTML = `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" style="vertical-align:middle;margin-right:6px;"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg> DESCARGAR YA (V${versionStr})`;
      }
    });

    const badgeText = document.getElementById('valcore-arch-badge');
    if (badgeText) {
      badgeText.textContent = `✓ Detectado: Windows ${arch === 'x64' ? '64-bit (x64)' : '32-bit (x86)'} — Instalador v${versionStr} 100% Compatible`;
      badgeText.className = `arch-badge badge-${arch}`;
    }

    const linkX64 = document.getElementById('valcore-link-x64');
    const linkX86 = document.getElementById('valcore-link-x86');
    if (linkX64) linkX64.href = `https://valcore.cloud/downloads/Ticketfy-Setup-v${versionStr}-x64.exe`;
    if (linkX86) linkX86.href = `https://valcore.cloud/downloads/Ticketfy-Setup-v${versionStr}-x86.exe`;
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', syncValcoreWebsite);
  } else {
    syncValcoreWebsite();
  }

  window.ValcoreDownload = {
    syncValcoreWebsite,
    detectWindowsArchitecture
  };
})();
