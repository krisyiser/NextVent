/**
 * Valcore Desktop Download Architecture Detector v1.0
 * Protocol Valcore v4.0 - Automatic OS & Architecture Detection Script for valcore.cloud
 */
(function () {
  'use strict';

  const DEFAULT_VERSION = '3.0.18';

  const DOWNLOAD_URLS = {
    x64: `https://valcore.cloud/downloads/Ticketfy-Setup-v${DEFAULT_VERSION}-x64.exe`,
    x86: `https://valcore.cloud/downloads/Ticketfy-Setup-v${DEFAULT_VERSION}-x86.exe`
  };

  /**
   * Determines if current Windows environment is 64-bit or 32-bit.
   * Uses UserAgentData API with fallback to userAgent/platform string matching.
   */
  async function detectWindowsArchitecture() {
    let is64Bit = true; // Default fallback to 64-bit

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
      console.warn('Valcore Detector: High entropy userAgentData query failed, falling back to x64', e);
    }

    return is64Bit ? 'x64' : 'x86';
  }

  /**
   * Initializes the auto-detection on the page elements.
   */
  async function initValcoreDownload() {
    const arch = await detectWindowsArchitecture();
    const targetUrl = DOWNLOAD_URLS[arch];

    const mainBtn = document.getElementById('valcore-main-download-btn');
    const badgeText = document.getElementById('valcore-arch-badge');
    const linkX64 = document.getElementById('valcore-link-x64');
    const linkX86 = document.getElementById('valcore-link-x86');

    if (mainBtn) {
      mainBtn.href = targetUrl;
      mainBtn.setAttribute('data-arch', arch);
    }

    if (badgeText) {
      if (arch === 'x64') {
        badgeText.textContent = '✓ Detectado: Windows 64-bit (x64) — Instalador 100% Compatible';
        badgeText.className = 'arch-badge badge-x64';
      } else {
        badgeText.textContent = '✓ Detectado: Windows 32-bit (x86) — Instalador 100% Compatible';
        badgeText.className = 'arch-badge badge-x86';
      }
    }

    if (linkX64) linkX64.href = DOWNLOAD_URLS.x64;
    if (linkX86) linkX86.href = DOWNLOAD_URLS.x86;
  }

  // Auto initialize on DOM ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initValcoreDownload);
  } else {
    initValcoreDownload();
  }

  // Export for global access if needed
  window.ValcoreDownload = {
    detectWindowsArchitecture,
    DOWNLOAD_URLS
  };
})();
