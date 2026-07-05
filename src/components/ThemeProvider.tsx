'use client';

import React, { createContext, useContext, useEffect, useState, ReactNode, useCallback } from 'react';
import { usePathname } from 'next/navigation';
import { getSetting } from '../lib/storage';

// ─── Contrast Utility ──────────────────────────────────────
function getContrastText(hex: string): string {
  const c = hex.replace('#', '');
  const r = parseInt(c.substring(0, 2), 16);
  const g = parseInt(c.substring(2, 4), 16);
  const b = parseInt(c.substring(4, 6), 16);
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
  return luminance > 0.5 ? '#111827' : '#FFFFFF';
}

type Theme = 'light' | 'dark';

interface ThemeContextProps {
  theme: Theme;
  toggleTheme: () => void;
  logo: string;
  storeName: string;
  refreshSettings: () => Promise<void>;
  accentPreset: string;
  sidebarColor: string;
  fontSizePreset: string;
  fontFamily: string;
  sidebarPosition: string;
  ticketPanelWidth: string;
  setPreviewOverrides: (overrides: Partial<{
    accentPreset: string;
    sidebarColor: string;
    fontSizePreset: string;
    fontFamily: string;
    sidebarPosition: string;
    ticketPanelWidth: string;
  }> | null) => void;
}

const ThemeContext = createContext<ThemeContextProps | undefined>(undefined);

// Preset mapping details synced with SettingsModal
const ACCENT_PRESETS = [
  { id: 'royal-blue', primary: '#0F52BA', hover: '#083C90' },
  { id: 'midnight', primary: '#1E3A5F', hover: '#162C4A' },
  { id: 'emerald', primary: '#047857', hover: '#065F46' },
  { id: 'deep-amber', primary: '#B45309', hover: '#92400E' },
  { id: 'crimson', primary: '#B91C1C', hover: '#991B1B' },
  { id: 'indigo', primary: '#4338CA', hover: '#3730A3' },
  { id: 'slate', primary: '#475569', hover: '#334155' },
  { id: 'teal', primary: '#0D9488', hover: '#0F766E' },
  { id: 'amethyst', primary: '#9966CC', hover: '#804DB3' },
  { id: 'rose', primary: '#E11D48', hover: '#BE123C' },
  { id: 'pumpkin', primary: '#D97706', hover: '#B45309' },
  { id: 'olive', primary: '#65A30D', hover: '#4D7C0F' },
  { id: 'lime', primary: '#84CC16', hover: '#65A30D' },
  { id: 'cyan', primary: '#06B6D4', hover: '#0891B2' },
];

const SIDEBAR_COLORS = [
  { id: 'corporate-blue', color: '#083C90' },
  { id: 'navy', color: '#0F172A' },
  { id: 'charcoal', color: '#1C1C1E' },
  { id: 'dark-slate', color: '#1E293B' },
  { id: 'forest', color: '#14532D' },
  { id: 'wine', color: '#4C0519' },
  { id: 'deep-purple', color: '#3B0764' },
  { id: 'graphite', color: '#27272A' },
  { id: 'burgundy', color: '#881337' },
  { id: 'chocolate', color: '#451A03' },
  { id: 'obsidian', color: '#020617' },
  { id: 'nord', color: '#2E3440' },
  { id: 'royal-purple', color: '#581C87' },
  { id: 'dark-emerald', color: '#064E3B' },
];

const FONT_SIZE_PRESETS = [
  { id: 'compact', base: '12px' },
  { id: 'normal', base: '14px' },
  { id: 'comfortable', base: '15px' },
  { id: 'large', base: '16px' },
  { id: 'xl', base: '18px' },
  { id: 'xxl', base: '20px' },
];

const FONT_FAMILIES: Record<string, string> = {
  inter: "'Inter', sans-serif",
  outfit: "'Outfit', sans-serif",
  roboto: "'Roboto', sans-serif",
  jetbrains: "'JetBrains Mono', monospace",
  montserrat: "'Montserrat', sans-serif",
  playfair: "'Playfair Display', serif",
};

export const ThemeProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [theme, setTheme] = useState<Theme>('light');
  const [logo, setLogo] = useState<string>('');
  const [storeName, setStoreName] = useState<string>('NEXT VENT POS');
  const [accentPreset, setAccentPreset] = useState<string>('royal-blue');
  const [sidebarColor, setSidebarColor] = useState<string>('corporate-blue');
  const [fontSizePreset, setFontSizePreset] = useState<string>('normal');
  const [fontFamily, setFontFamily] = useState<string>('inter');
  const [sidebarPosition, setSidebarPosition] = useState<string>('left');
  const [ticketPanelWidth, setTicketPanelWidth] = useState<string>('400');
  const pathname = usePathname();

  // Temporary live preview state overrides
  const [previewOverrides, setPreviewOverridesState] = useState<Partial<{
    accentPreset: string;
    sidebarColor: string;
    fontSizePreset: string;
    fontFamily: string;
    sidebarPosition: string;
    ticketPanelWidth: string;
  }> | null>(null);

  const setPreviewOverrides = useCallback((overrides: typeof previewOverrides) => {
    setPreviewOverridesState(overrides);
  }, []);

  // Load and apply settings from SQLite
  const refreshSettings = useCallback(async () => {
    try {
      // 1. Dark/Light Theme
      const storedTheme = await getSetting('theme', 'light');
      const validTheme = storedTheme === 'dark' ? 'dark' : 'light';
      setTheme(validTheme);
      document.documentElement.classList.toggle('dark', validTheme === 'dark');

      // 2. Company Details & Logo
      const storedLogo = await getSetting('pos_store_logo', '');
      const storedStoreName = await getSetting('pos_store_name', 'Mi Tienda POS');
      setLogo(storedLogo);
      setStoreName(storedStoreName);

      // 3. UI Presets
      const storedAccent = await getSetting('ui_accent_preset', 'royal-blue');
      const storedSidebar = await getSetting('ui_sidebar_color', 'corporate-blue');
      const storedFontSize = await getSetting('ui_font_size_preset', 'normal');
      const storedFontFam = await getSetting('ui_font_family', 'inter');
      const storedSidebarPos = await getSetting('ui_sidebar_position', 'left');
      const storedTicketWidth = await getSetting('ui_ticket_panel_width', '400');

      setAccentPreset(storedAccent);
      setSidebarColor(storedSidebar);
      setFontSizePreset(storedFontSize);
      setFontFamily(storedFontFam);
      setSidebarPosition(storedSidebarPos);
      setTicketPanelWidth(storedTicketWidth);

      // Clear preview overrides on manual sync/load
      setPreviewOverridesState(null);

    } catch (e) {
      console.error('Settings load error', e);
    }
  }, []);

  // Load preferences on mount
  useEffect(() => {
    refreshSettings();
  }, [refreshSettings]);

  // Sync theme with document.documentElement on path changes
  useEffect(() => {
    document.documentElement.classList.toggle('dark', theme === 'dark');
  }, [theme, pathname]);

  const toggleTheme = async () => {
    const newTheme = theme === 'dark' ? 'light' : 'dark';
    setTheme(newTheme);
    document.documentElement.classList.toggle('dark', newTheme === 'dark');
    try {
      await getSetting('theme', 'light'); // Probe DB connection
      const { setSetting } = await import('../lib/storage');
      await setSetting('theme', newTheme);
    } catch (e) {
      console.error('Failed to save theme', e);
    }
  };

  // Compute values using overrides if present
  const activeAccentPreset = previewOverrides?.accentPreset ?? accentPreset;
  const activeSidebarColor = previewOverrides?.sidebarColor ?? sidebarColor;
  const activeFontSizePreset = previewOverrides?.fontSizePreset ?? fontSizePreset;
  const activeFontFamily = previewOverrides?.fontFamily ?? fontFamily;
  const activeSidebarPosition = previewOverrides?.sidebarPosition ?? sidebarPosition;
  const activeTicketPanelWidth = previewOverrides?.ticketPanelWidth ?? ticketPanelWidth;

  const accent = ACCENT_PRESETS.find(p => p.id === activeAccentPreset) || ACCENT_PRESETS[0];
  const sbg = SIDEBAR_COLORS.find(c => c.id === activeSidebarColor) || SIDEBAR_COLORS[0];
  const fontSz = FONT_SIZE_PRESETS.find(f => f.id === activeFontSizePreset) || FONT_SIZE_PRESETS[1];
  const font = FONT_FAMILIES[activeFontFamily] || FONT_FAMILIES.inter;

  let appFlexDirection = 'row';
  let sidebarWidth = '80px';
  let sidebarHeight = '100%';
  let sidebarFlexDirection = 'column';
  let sidebarBorderRight = '1px solid rgba(0, 0, 0, 0.1)';
  let sidebarBorderLeft = 'none';
  let sidebarBorderBottom = 'none';
  let sidebarBorderTop = 'none';
  let sidebarPadding = '20px 0';
  let sidebarGap = '20px';
  let sidebarItemMarginTop = 'auto';
  let sidebarItemMarginLeft = '0';

  if (activeSidebarPosition === 'right') {
    appFlexDirection = 'row-reverse';
    sidebarWidth = '80px';
    sidebarHeight = '100%';
    sidebarFlexDirection = 'column';
    sidebarBorderRight = 'none';
    sidebarBorderLeft = '1px solid rgba(0, 0, 0, 0.1)';
    sidebarBorderBottom = 'none';
    sidebarBorderTop = 'none';
    sidebarPadding = '20px 0';
    sidebarGap = '20px';
    sidebarItemMarginTop = 'auto';
    sidebarItemMarginLeft = '0';
  } else if (activeSidebarPosition === 'top') {
    appFlexDirection = 'column';
    sidebarWidth = '100%';
    sidebarHeight = '64px';
    sidebarFlexDirection = 'row';
    sidebarBorderRight = 'none';
    sidebarBorderLeft = 'none';
    sidebarBorderBottom = '1px solid rgba(0, 0, 0, 0.1)';
    sidebarBorderTop = 'none';
    sidebarPadding = '0 24px';
    sidebarGap = '16px';
    sidebarItemMarginTop = '0';
    sidebarItemMarginLeft = 'auto';
  } else if (activeSidebarPosition === 'bottom') {
    appFlexDirection = 'column-reverse';
    sidebarWidth = '100%';
    sidebarHeight = '64px';
    sidebarFlexDirection = 'row';
    sidebarBorderRight = 'none';
    sidebarBorderLeft = 'none';
    sidebarBorderBottom = 'none';
    sidebarBorderTop = '1px solid rgba(0, 0, 0, 0.1)';
    sidebarPadding = '0 24px';
    sidebarGap = '16px';
    sidebarItemMarginTop = '0';
    sidebarItemMarginLeft = 'auto';
  }

  return (
    <ThemeContext.Provider value={{ 
      theme, 
      toggleTheme, 
      logo, 
      storeName, 
      refreshSettings,
      accentPreset: activeAccentPreset,
      sidebarColor: activeSidebarColor,
      fontSizePreset: activeFontSizePreset,
      fontFamily: activeFontFamily,
      sidebarPosition: activeSidebarPosition,
      ticketPanelWidth: activeTicketPanelWidth,
      setPreviewOverrides
    }}>
      <style dangerouslySetInnerHTML={{ __html: `
        :root {
          --accent-primary: ${accent.primary} !important;
          --accent-hover: ${accent.hover} !important;
          --text-on-accent: ${getContrastText(accent.primary)} !important;
          --text-on-success: ${getContrastText('#10B981')} !important;
          --text-on-danger: ${getContrastText('#EF4444')} !important;
          --text-on-warning: ${getContrastText('#F59E0B')} !important;
          --sidebar-bg-color: ${sbg.color} !important;
          --base-font-size: ${fontSz.base} !important;
          --font-family: ${font} !important;
          --ticket-panel-width: ${activeTicketPanelWidth}px !important;
          --app-flex-direction: ${appFlexDirection} !important;
          --sidebar-width: ${sidebarWidth} !important;
          --sidebar-height: ${sidebarHeight} !important;
          --sidebar-flex-direction: ${sidebarFlexDirection} !important;
          --sidebar-border-right: ${sidebarBorderRight} !important;
          --sidebar-border-left: ${sidebarBorderLeft} !important;
          --sidebar-border-bottom: ${sidebarBorderBottom} !important;
          --sidebar-border-top: ${sidebarBorderTop} !important;
          --sidebar-padding: ${sidebarPadding} !important;
          --sidebar-gap: ${sidebarGap} !important;
          --sidebar-item-margin-top: ${sidebarItemMarginTop} !important;
          --sidebar-item-margin-left: ${sidebarItemMarginLeft} !important;
        }
        html {
          font-size: ${fontSz.base} !important;
        }
      ` }} />
      {children}
    </ThemeContext.Provider>
  );
};

export const useTheme = () => {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error('useTheme must be used within ThemeProvider');
  }
  return context;
};

