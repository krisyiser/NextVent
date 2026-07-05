'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  X, GearSix, Storefront, Printer, FloppyDisk, DownloadSimple, WifiHigh, Keyboard,
  MonitorPlay, QrCode, Palette, TextAa, Layout, ShieldCheck, Database, Bell,
  Users, Receipt, CaretRight, CaretDown, ArrowCounterClockwise, Eye, EyeSlash,
  Buildings, Phone, MapPin, CurrencyDollar, Barcode, Scales, Lightning,
  ClockCounterClockwise, Warning, Info, CheckCircle, Trash, Export,
  PaintBrush, SlidersHorizontal, Gear, DesktopTower, Megaphone
} from 'phosphor-react';
import { invoke } from '@tauri-apps/api/core';
import { getSetting, setSetting } from '@/lib/storage';
import { toast } from 'sonner';
import { useTheme } from './ThemeProvider';
import { SettingsUsers } from './SettingsUsers';

type SettingsModalProps = {
  isOpen: boolean;
  onClose: () => void;
};

type SettingsCategory =
  | 'empresa'
  | 'interfaz'
  | 'ticket'
  | 'conexiones'
  | 'seguridad'
  | 'datos'
  | 'notificaciones'
  | 'atajos'
  | 'usuarios';

type InterfaceSubTab = 'tema' | 'colores' | 'fuentes' | 'layout' | 'animaciones';
type EmpresaSubTab = 'identidad' | 'sucursal' | 'moneda' | 'fiscal';
type TicketSubTab = 'formato' | 'contenido' | 'preview';
type SeguridadSubTab = 'acceso' | 'sesion' | 'auditoria';
type DatosSubTab = 'respaldos' | 'limpieza' | 'exportar';

// ─── Accent Color Presets ──────────────────────────────────────
const ACCENT_PRESETS = [
  { id: 'royal-blue', label: 'Azul Real', primary: '#0F52BA', hover: '#083C90' },
  { id: 'midnight', label: 'Azul Marino', primary: '#1E3A5F', hover: '#162C4A' },
  { id: 'emerald', label: 'Esmeralda', primary: '#047857', hover: '#065F46' },
  { id: 'deep-amber', label: 'Ámbar Profundo', primary: '#B45309', hover: '#92400E' },
  { id: 'crimson', label: 'Carmesí', primary: '#B91C1C', hover: '#991B1B' },
  { id: 'indigo', label: 'Índigo', primary: '#4338CA', hover: '#3730A3' },
  { id: 'slate', label: 'Pizarra', primary: '#475569', hover: '#334155' },
  { id: 'teal', label: 'Teal', primary: '#0D9488', hover: '#0F766E' },
  { id: 'amethyst', label: 'Amatista', primary: '#9966CC', hover: '#804DB3' },
  { id: 'rose', label: 'Rosa Chicle', primary: '#E11D48', hover: '#BE123C' },
  { id: 'pumpkin', label: 'Calabaza', primary: '#D97706', hover: '#B45309' },
  { id: 'olive', label: 'Oliva', primary: '#65A30D', hover: '#4D7C0F' },
  { id: 'lime', label: 'Verde Lima', primary: '#84CC16', hover: '#65A30D' },
  { id: 'cyan', label: 'Cian Hielo', primary: '#06B6D4', hover: '#0891B2' },
];

const FONT_SIZE_PRESETS = [
  { id: 'compact', label: 'Compacto', base: '12px', scale: 0.85 },
  { id: 'normal', label: 'Normal', base: '14px', scale: 1.0 },
  { id: 'comfortable', label: 'Cómodo', base: '15px', scale: 1.07 },
  { id: 'large', label: 'Grande', base: '16px', scale: 1.14 },
  { id: 'xl', label: 'Extra Grande', base: '18px', scale: 1.28 },
  { id: 'xxl', label: 'Giga', base: '20px', scale: 1.43 },
];

const SIDEBAR_COLORS = [
  { id: 'corporate-blue', label: 'Azul Corporativo', color: '#083C90' },
  { id: 'navy', label: 'Azul Marino', color: '#0F172A' },
  { id: 'charcoal', label: 'Carbón', color: '#1C1C1E' },
  { id: 'dark-slate', label: 'Pizarra Oscuro', color: '#1E293B' },
  { id: 'forest', label: 'Bosque', color: '#14532D' },
  { id: 'wine', label: 'Vino', color: '#4C0519' },
  { id: 'deep-purple', label: 'Púrpura Profundo', color: '#3B0764' },
  { id: 'graphite', label: 'Grafito', color: '#27272A' },
  { id: 'burgundy', label: 'Burdeos', color: '#881337' },
  { id: 'chocolate', label: 'Cacao', color: '#451A03' },
  { id: 'obsidian', label: 'Obsidiana', color: '#020617' },
  { id: 'nord', label: 'Nord Gray', color: '#2E3440' },
  { id: 'royal-purple', label: 'Púrpura Real', color: '#581C87' },
  { id: 'dark-emerald', label: 'Esmeralda Profundo', color: '#064E3B' },
];

// ──────────────────────────────────────────────────────────────
// SETTINGS INPUT COMPONENT
// ──────────────────────────────────────────────────────────────
const SettingsInput: React.FC<{
  label: string;
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
  type?: string;
  icon?: React.ReactNode;
  hint?: string;
}> = ({ label, value, onChange, placeholder, type = 'text', icon, hint }) => (
  <div style={{ marginBottom: '16px' }}>
    <label style={{
      display: 'flex', alignItems: 'center', gap: '6px',
      fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)',
      marginBottom: '6px', textTransform: 'uppercase', letterSpacing: '0.5px'
    }}>
      {icon} {label}
    </label>
    <input
      type={type}
      value={value}
      onChange={e => onChange(e.target.value)}
      placeholder={placeholder}
      style={{
        width: '100%', padding: '10px 12px',
        backgroundColor: 'var(--bg-primary)', border: '1px solid var(--border-color)',
        borderRadius: '4px', color: 'var(--text-primary)', fontSize: '13px',
        fontFamily: 'Inter, sans-serif', outline: 'none', transition: 'border-color 0.15s'
      }}
      onFocus={e => e.target.style.borderColor = 'var(--accent-primary)'}
      onBlur={e => e.target.style.borderColor = 'var(--border-color)'}
    />
    {hint && <p style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '4px' }}>{hint}</p>}
  </div>
);

const SettingsSelect: React.FC<{
  label: string;
  value: string;
  onChange: (v: string) => void;
  options: { value: string; label: string }[];
  icon?: React.ReactNode;
}> = ({ label, value, onChange, options, icon }) => (
  <div style={{ marginBottom: '16px' }}>
    <label style={{
      display: 'flex', alignItems: 'center', gap: '6px',
      fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)',
      marginBottom: '6px', textTransform: 'uppercase', letterSpacing: '0.5px'
    }}>
      {icon} {label}
    </label>
    <select
      value={value}
      onChange={e => onChange(e.target.value)}
      style={{
        width: '100%', padding: '10px 12px',
        backgroundColor: 'var(--bg-primary)', border: '1px solid var(--border-color)',
        borderRadius: '4px', color: 'var(--text-primary)', fontSize: '13px',
        fontFamily: 'Inter, sans-serif', outline: 'none', cursor: 'pointer'
      }}
    >
      {options.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
    </select>
  </div>
);

const SettingsToggle: React.FC<{
  label: string;
  description?: string;
  checked: boolean;
  onChange: (v: boolean) => void;
}> = ({ label, description, checked, onChange }) => (
  <div style={{
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    padding: '12px 0', borderBottom: '1px solid var(--border-color)'
  }}>
    <div>
      <div style={{ fontSize: '13px', fontWeight: 500, color: 'var(--text-primary)' }}>{label}</div>
      {description && <div style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '2px' }}>{description}</div>}
    </div>
    <button
      onClick={() => onChange(!checked)}
      style={{
        width: '44px', height: '24px', borderRadius: '12px', border: 'none',
        backgroundColor: checked ? 'var(--accent-primary)' : 'var(--bg-tertiary)',
        cursor: 'pointer', position: 'relative', transition: 'background-color 0.2s',
        flexShrink: 0
      }}
    >
      <div style={{
        width: '18px', height: '18px', borderRadius: '50%',
        backgroundColor: '#fff', position: 'absolute', top: '3px',
        left: checked ? '23px' : '3px', transition: 'left 0.2s',
        boxShadow: '0 1px 3px rgba(0,0,0,0.2)'
      }} />
    </button>
  </div>
);

const SectionHeader: React.FC<{ icon: React.ReactNode; title: string; description?: string }> = ({ icon, title, description }) => (
  <div style={{ marginBottom: '20px', paddingBottom: '12px', borderBottom: '1px solid var(--border-color)' }}>
    <h3 style={{ fontSize: '16px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--text-primary)' }}>
      {icon} {title}
    </h3>
    {description && <p style={{ fontSize: '12px', color: 'var(--text-muted)', marginTop: '4px' }}>{description}</p>}
  </div>
);

// ──────────────────────────────────────────────────────────────
// MAIN COMPONENT
// ──────────────────────────────────────────────────────────────
export const SettingsModal = ({ isOpen, onClose }: SettingsModalProps) => {
  const { theme, toggleTheme, setPreviewOverrides, refreshSettings } = useTheme();
  const [activeCategory, setActiveCategory] = useState<SettingsCategory>('empresa');
  const [interfaceSubTab, setInterfaceSubTab] = useState<InterfaceSubTab>('tema');
  const [empresaSubTab, setEmpresaSubTab] = useState<EmpresaSubTab>('identidad');
  const [ticketSubTab, setTicketSubTab] = useState<TicketSubTab>('formato');
  const [seguridadSubTab, setSeguridadSubTab] = useState<SeguridadSubTab>('acceso');
  const [datosSubTab, setDatosSubTab] = useState<DatosSubTab>('respaldos');

  // ─── EMPRESA state ──────────────────────────────────────
  const [storeName, setStoreName] = useState('Mi Tienda POS');
  const [storeSlogan, setStoreSlogan] = useState('');
  const [address, setAddress] = useState('Av. Principal #123');
  const [phone, setPhone] = useState('555-123-4567');
  const [email, setEmail] = useState('');
  const [rfc, setRfc] = useState('');
  const [sucursalName, setSucursalName] = useState('Sucursal Principal');
  const [sucursalCode, setSucursalCode] = useState('SUC-001');
  const [currencySymbol, setCurrencySymbol] = useState('$');
  const [currencyCode, setCurrencyCode] = useState('MXN');
  const [taxRate, setTaxRate] = useState('16');
  const [logo, setLogo] = useState('');
  
  // ─── FISCAL state ───────────────────────────────────────
  const [facturamaUser, setFacturamaUser] = useState('');
  const [facturamaSecret, setFacturamaSecret] = useState('');

  // ─── TICKET state ───────────────────────────────────────
  const [footerMessage, setFooterMessage] = useState('¡Gracias por su compra!');
  const [paperWidth, setPaperWidth] = useState('80mm');
  const [ticketFontSize, setTicketFontSize] = useState('12px');
  const [showLogo, setShowLogo] = useState(true);
  const [showBarcode, setShowBarcode] = useState(true);
  const [showTaxBreakdown, setShowTaxBreakdown] = useState(false);
  const [autoPrint, setAutoPrint] = useState(false);
  const [printCopies, setPrintCopies] = useState('1');

  // ─── INTERFAZ state ─────────────────────────────────────
  const [accentPreset, setAccentPreset] = useState('royal-blue');
  const [sidebarColor, setSidebarColor] = useState('corporate-blue');
  const [fontSizePreset, setFontSizePreset] = useState('normal');
  const [fontFamily, setFontFamily] = useState('inter');
  const [showAnimations, setShowAnimations] = useState(true);
  const [compactMode, setCompactMode] = useState(false);
  const [showStockBadge, setShowStockBadge] = useState(true);
  const [showProductImages, setShowProductImages] = useState(true);
  const [sidebarPosition, setSidebarPosition] = useState('left');
  const [ticketPanelWidth, setTicketPanelWidth] = useState('400');
  const [productGridCols, setProductGridCols] = useState('auto');
  const [hoverEffects, setHoverEffects] = useState(true);
  const [transitionSpeed, setTransitionSpeed] = useState('normal');

  // ─── CONEXIONES state ───────────────────────────────────
  const [localIp, setLocalIp] = useState('127.0.0.1');

  // ─── SEGURIDAD state ────────────────────────────────────
  const [requirePasswordChange, setRequirePasswordChange] = useState(false);
  const [sessionTimeout, setSessionTimeout] = useState('30');
  const [autoLock, setAutoLock] = useState(true);
  const [allowMultipleSessions, setAllowMultipleSessions] = useState(false);
  const [showAuditTrail, setShowAuditTrail] = useState(true);
  const [requirePinForVoids, setRequirePinForVoids] = useState(true);
  const [requirePinForDiscounts, setRequirePinForDiscounts] = useState(false);
  const [maxDiscountPercent, setMaxDiscountPercent] = useState('50');

  // ─── NOTIFICACIONES state ───────────────────────────────
  const [soundOnScan, setSoundOnScan] = useState(true);
  const [soundOnSale, setSoundOnSale] = useState(true);
  const [lowStockAlert, setLowStockAlert] = useState(true);
  const [lowStockThreshold, setLowStockThreshold] = useState('5');
  const [shiftReminder, setShiftReminder] = useState(true);
  const [toastPosition, setToastPosition] = useState('top-right');
  const [toastDuration, setToastDuration] = useState('5');

  // ─── LOAD SETTINGS ──────────────────────────────────────
  useEffect(() => {
    if (!isOpen) return;
    (async () => {
      try {
        // Empresa
        setStoreName(await getSetting('pos_store_name', 'Mi Tienda POS'));
        setStoreSlogan(await getSetting('pos_store_slogan', ''));
        setAddress(await getSetting('pos_store_address', 'Av. Principal #123'));
        setPhone(await getSetting('pos_store_phone', '555-123-4567'));
        setEmail(await getSetting('pos_store_email', ''));
        setRfc(await getSetting('pos_store_rfc', ''));
        setSucursalName(await getSetting('pos_sucursal_name', 'Sucursal Principal'));
        setSucursalCode(await getSetting('pos_sucursal_code', 'SUC-001'));
        setCurrencySymbol(await getSetting('pos_currency_symbol', '$'));
        setCurrencyCode(await getSetting('pos_currency_code', 'MXN'));
        setTaxRate(await getSetting('pos_tax_rate', '16'));
        setLogo(await getSetting('pos_store_logo', ''));
        setFacturamaUser(await getSetting('pos_facturama_user', ''));
        setFacturamaSecret(await getSetting('pos_facturama_secret', ''));

        // Ticket
        setFooterMessage(await getSetting('pos_ticket_footer', '¡Gracias por su compra!'));
        setPaperWidth(await getSetting('pos_ticket_width', '80mm'));
        setTicketFontSize(await getSetting('pos_ticket_font', '12px'));
        setShowLogo(await getSetting('pos_ticket_show_logo', '1') === '1');
        setShowBarcode(await getSetting('pos_ticket_show_barcode', '1') === '1');
        setShowTaxBreakdown(await getSetting('pos_ticket_show_tax', '0') === '1');
        setAutoPrint(await getSetting('pos_ticket_auto_print', '0') === '1');
        setPrintCopies(await getSetting('pos_ticket_copies', '1'));

        // Interfaz
        setAccentPreset(await getSetting('ui_accent_preset', 'royal-blue'));
        setSidebarColor(await getSetting('ui_sidebar_color', 'corporate-blue'));
        setFontSizePreset(await getSetting('ui_font_size_preset', 'normal'));
        setFontFamily(await getSetting('ui_font_family', 'inter'));
        setShowAnimations(await getSetting('ui_show_animations', '1') === '1');
        setCompactMode(await getSetting('ui_compact_mode', '0') === '1');
        setShowStockBadge(await getSetting('ui_show_stock_badge', '1') === '1');
        setShowProductImages(await getSetting('ui_show_product_images', '1') === '1');
        setSidebarPosition(await getSetting('ui_sidebar_position', 'left'));
        setTicketPanelWidth(await getSetting('ui_ticket_panel_width', '400'));
        setProductGridCols(await getSetting('ui_product_grid_cols', 'auto'));
        setHoverEffects(await getSetting('ui_hover_effects', '1') === '1');
        setTransitionSpeed(await getSetting('ui_transition_speed', 'normal'));

        // Seguridad
        setRequirePasswordChange(await getSetting('sec_require_pw_change', '0') === '1');
        setSessionTimeout(await getSetting('sec_session_timeout', '30'));
        setAutoLock(await getSetting('sec_auto_lock', '1') === '1');
        setAllowMultipleSessions(await getSetting('sec_multi_sessions', '0') === '1');
        setShowAuditTrail(await getSetting('sec_show_audit', '1') === '1');
        setRequirePinForVoids(await getSetting('sec_pin_voids', '1') === '1');
        setRequirePinForDiscounts(await getSetting('sec_pin_discounts', '0') === '1');
        setMaxDiscountPercent(await getSetting('sec_max_discount', '50'));

        // Notificaciones
        setSoundOnScan(await getSetting('notif_sound_scan', '1') === '1');
        setSoundOnSale(await getSetting('notif_sound_sale', '1') === '1');
        setLowStockAlert(await getSetting('notif_low_stock', '1') === '1');
        setLowStockThreshold(await getSetting('notif_low_stock_threshold', '5'));
        setShiftReminder(await getSetting('notif_shift_reminder', '1') === '1');
        setToastPosition(await getSetting('notif_toast_position', 'top-right'));
        setToastDuration(await getSetting('notif_toast_duration', '5'));
      } catch (e) {
        console.error('Failed to load settings', e);
      }
    })();
  }, [isOpen]);

  // Sync real-time changes to ThemeProvider for instant live preview
  useEffect(() => {
    if (isOpen) {
      setPreviewOverrides({
        accentPreset,
        sidebarColor,
        fontSizePreset,
        fontFamily,
        sidebarPosition,
        ticketPanelWidth,
      });
    }
  }, [
    isOpen,
    accentPreset,
    sidebarColor,
    fontSizePreset,
    fontFamily,
    sidebarPosition,
    ticketPanelWidth,
    setPreviewOverrides,
  ]);

  const handleCloseAndReset = () => {
    setPreviewOverrides(null);
    refreshSettings();
    onClose();
  };

  // ─── APPLY ACCENT COLORS LIVE ───────────────────────────
  const applyAccentColor = useCallback((presetId: string) => {
    // Handled reactively by ThemeProvider now
  }, []);

  const applySidebarColor = useCallback((presetId: string) => {
    const preset = SIDEBAR_COLORS.find(p => p.id === presetId);
    if (preset) {
      document.documentElement.style.setProperty('--sidebar-bg-color', preset.color);
    }
  }, []);

  const applyFontSize = useCallback((presetId: string) => {
    const preset = FONT_SIZE_PRESETS.find(p => p.id === presetId);
    if (preset) {
      document.documentElement.style.setProperty('--base-font-size', preset.base);
      document.documentElement.style.fontSize = preset.base;
    }
  }, []);

  const applyTransitionSpeed = useCallback((speed: string) => {
    const map: Record<string, string> = {
      'none': 'all 0s',
      'fast': 'all 0.08s ease-in-out',
      'normal': 'all 0.12s ease-in-out',
      'slow': 'all 0.25s ease-in-out',
    };
    document.documentElement.style.setProperty('--transition', map[speed] || map['normal']);
  }, []);

  const handleLogoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      // Validate size (limit to 300KB to keep DB and memory fast)
      if (file.size > 300 * 1024) {
        toast.error('El logo no debe pesar más de 300 KB para mantener la base de datos veloz.');
        return;
      }
      const reader = new FileReader();
      reader.onloadend = () => {
        setLogo(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  // ─── SAVE ALL ───────────────────────────────────────────
  const handleSaveAll = async () => {
    try {
      // Empresa
      await setSetting('pos_store_name', storeName);
      await setSetting('pos_store_slogan', storeSlogan);
      await setSetting('pos_store_address', address);
      await setSetting('pos_store_phone', phone);
      await setSetting('pos_store_email', email);
      await setSetting('pos_store_rfc', rfc);
      await setSetting('pos_sucursal_name', sucursalName);
      await setSetting('pos_sucursal_code', sucursalCode);
      await setSetting('pos_currency_symbol', currencySymbol);
      await setSetting('pos_currency_code', currencyCode);
      await setSetting('pos_tax_rate', taxRate);
      await setSetting('pos_store_logo', logo);
      await setSetting('pos_facturama_user', facturamaUser);
      await setSetting('pos_facturama_secret', facturamaSecret);

      // Ticket
      await setSetting('pos_ticket_footer', footerMessage);
      await setSetting('pos_ticket_width', paperWidth);
      await setSetting('pos_ticket_font', ticketFontSize);
      await setSetting('pos_ticket_show_logo', showLogo ? '1' : '0');
      await setSetting('pos_ticket_show_barcode', showBarcode ? '1' : '0');
      await setSetting('pos_ticket_show_tax', showTaxBreakdown ? '1' : '0');
      await setSetting('pos_ticket_auto_print', autoPrint ? '1' : '0');
      await setSetting('pos_ticket_copies', printCopies);

      // Interfaz
      await setSetting('ui_accent_preset', accentPreset);
      await setSetting('ui_sidebar_color', sidebarColor);
      await setSetting('ui_font_size_preset', fontSizePreset);
      await setSetting('ui_font_family', fontFamily);
      await setSetting('ui_show_animations', showAnimations ? '1' : '0');
      await setSetting('ui_compact_mode', compactMode ? '1' : '0');
      await setSetting('ui_show_stock_badge', showStockBadge ? '1' : '0');
      await setSetting('ui_show_product_images', showProductImages ? '1' : '0');
      await setSetting('ui_sidebar_position', sidebarPosition);
      await setSetting('ui_ticket_panel_width', ticketPanelWidth);
      await setSetting('ui_product_grid_cols', productGridCols);
      await setSetting('ui_hover_effects', hoverEffects ? '1' : '0');
      await setSetting('ui_transition_speed', transitionSpeed);

      // Seguridad
      await setSetting('sec_require_pw_change', requirePasswordChange ? '1' : '0');
      await setSetting('sec_session_timeout', sessionTimeout);
      await setSetting('sec_auto_lock', autoLock ? '1' : '0');
      await setSetting('sec_multi_sessions', allowMultipleSessions ? '1' : '0');
      await setSetting('sec_show_audit', showAuditTrail ? '1' : '0');
      await setSetting('sec_pin_voids', requirePinForVoids ? '1' : '0');
      await setSetting('sec_pin_discounts', requirePinForDiscounts ? '1' : '0');
      await setSetting('sec_max_discount', maxDiscountPercent);

      // Notificaciones
      await setSetting('notif_sound_scan', soundOnScan ? '1' : '0');
      await setSetting('notif_sound_sale', soundOnSale ? '1' : '0');
      await setSetting('notif_low_stock', lowStockAlert ? '1' : '0');
      await setSetting('notif_low_stock_threshold', lowStockThreshold);
      await setSetting('notif_shift_reminder', shiftReminder ? '1' : '0');
      await setSetting('notif_toast_position', toastPosition);
      await setSetting('notif_toast_duration', toastDuration);

      // Apply visual changes
      applyAccentColor(accentPreset);
      applySidebarColor(sidebarColor);
      applyFontSize(fontSizePreset);
      applyTransitionSpeed(transitionSpeed);

      toast.success('Configuración guardada correctamente');
    } catch (err) {
      toast.error('Error al guardar la configuración');
      console.error(err);
    }
  };

  const handleBackup = async () => {
    try {
      const path = await invoke('backup_database');
      toast.success(`Respaldo exitoso: ${path}`);
    } catch (err) {
      toast.error(`Error al respaldar: ${err}`);
    }
  };

  if (!isOpen) return null;

  // ─── CATEGORY NAVIGATION ───────────────────────────────
  const categories: { id: SettingsCategory; label: string; icon: React.ReactNode }[] = [
    { id: 'empresa', label: 'Empresa', icon: <Buildings size={18} weight="regular" /> },
    { id: 'interfaz', label: 'Interfaz', icon: <PaintBrush size={18} weight="regular" /> },
    { id: 'ticket', label: 'Ticket', icon: <Receipt size={18} weight="regular" /> },
    { id: 'conexiones', label: 'Conexiones', icon: <WifiHigh size={18} weight="regular" /> },
    { id: 'seguridad', label: 'Seguridad', icon: <ShieldCheck size={18} weight="regular" /> },
    { id: 'datos', label: 'Datos', icon: <Database size={18} weight="regular" /> },
    { id: 'notificaciones', label: 'Alertas', icon: <Bell size={18} weight="regular" /> },
    { id: 'usuarios', label: 'Usuarios y Roles', icon: <Users size={18} weight="regular" /> },
    { id: 'atajos', label: 'Atajos', icon: <Keyboard size={18} weight="regular" /> },
  ];

  const subTabStyle = (isActive: boolean): React.CSSProperties => ({
    padding: '6px 14px', fontSize: '12px', fontWeight: isActive ? 600 : 500,
    color: isActive ? 'var(--accent-primary)' : 'var(--text-secondary)',
    backgroundColor: isActive ? 'rgba(15, 82, 186, 0.08)' : 'transparent',
    border: isActive ? '1px solid var(--accent-primary)' : '1px solid var(--border-color)',
    borderRadius: '4px', cursor: 'pointer', transition: 'all 0.12s', whiteSpace: 'nowrap' as const,
  });

  // ─── RENDER CONTENT PANELS ─────────────────────────────
  const renderContent = () => {
    switch (activeCategory) {
      // ═══════════════════════════════════════════════════
      // EMPRESA
      // ═══════════════════════════════════════════════════
      case 'empresa':
        return (
          <div>
            <div style={{ display: 'flex', gap: '8px', marginBottom: '20px', flexWrap: 'wrap' }}>
              <button style={subTabStyle(empresaSubTab === 'identidad')} onClick={() => setEmpresaSubTab('identidad')}>
                <Storefront size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Identidad
              </button>
              <button style={subTabStyle(empresaSubTab === 'sucursal')} onClick={() => setEmpresaSubTab('sucursal')}>
                <MapPin size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Sucursal
              </button>
              <button style={subTabStyle(empresaSubTab === 'moneda')} onClick={() => setEmpresaSubTab('moneda')}>
                <CurrencyDollar size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Moneda e Impuestos
              </button>
              <button style={subTabStyle(empresaSubTab === 'fiscal')} onClick={() => setEmpresaSubTab('fiscal')}>
                <Receipt size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Facturación CFDI
              </button>
            </div>

            {empresaSubTab === 'identidad' && (
              <div>
                <SectionHeader icon={<Storefront size={20} className="text-accent" />} title="Identidad del Negocio" description="Datos que aparecen en tickets, reportes y pantalla del cliente." />
                
                {/* LOGO UPLOAD SECTION */}
                <div style={{ marginBottom: '20px', border: '1px dashed var(--border-color)', padding: '16px', borderRadius: '6px', backgroundColor: 'var(--bg-primary)' }}>
                  <label style={{ display: 'block', fontSize: '11px', fontWeight: 700, color: 'var(--text-secondary)', marginBottom: '8px', textTransform: 'uppercase', letterSpacing: '0.5px' }}>Logo de la Empresa</label>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
                    {logo ? (
                      <div style={{ width: '70px', height: '70px', borderRadius: '4px', overflow: 'hidden', border: '1px solid var(--border-color)', display: 'flex', alignItems: 'center', justifyContent: 'center', backgroundColor: '#fff', flexShrink: 0 }}>
                        <img src={logo} alt="Preview" style={{ maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }} />
                      </div>
                    ) : (
                      <div style={{ width: '70px', height: '70px', borderRadius: '4px', border: '1px dashed var(--border-color)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--text-muted)', fontSize: '11px', flexShrink: 0, textAlign: 'center', padding: '4px' }}>
                        Sin Logo
                      </div>
                    )}
                    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: '8px', alignItems: 'flex-start' }}>
                      <input 
                        id="logo-file-input"
                        type="file" 
                        accept="image/*" 
                        onChange={handleLogoChange} 
                        style={{ display: 'none' }} 
                      />
                      <button
                        type="button"
                        onClick={() => document.getElementById('logo-file-input')?.click()}
                        style={{
                          padding: '8px 16px',
                          backgroundColor: 'var(--bg-secondary)',
                          border: '1px solid var(--border-color)',
                          borderRadius: '4px',
                          color: 'var(--text-primary)',
                          fontSize: '12px',
                          fontWeight: 600,
                          cursor: 'pointer',
                          transition: 'var(--transition)'
                        }}
                        onMouseEnter={e => e.currentTarget.style.borderColor = 'var(--accent-primary)'}
                        onMouseLeave={e => e.currentTarget.style.borderColor = 'var(--border-color)'}
                      >
                        Seleccionar Archivo Logo
                      </button>
                      {logo && (
                        <button 
                          onClick={() => setLogo('')} 
                          style={{ fontSize: '11px', padding: '6px 12px', backgroundColor: 'var(--accent-danger)', color: 'var(--text-on-danger)', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: 600 }}
                        >
                          Eliminar Logo
                        </button>
                      )}
                    </div>
                  </div>
                  <p style={{ fontSize: '11px', color: 'var(--text-secondary)', marginTop: '8px', lineHeight: 1.4 }}>El logo se mostrará en la barra lateral superior de la interfaz y en la cabecera de los tickets impresos.</p>
                </div>

                <SettingsInput label="Nombre del Negocio" value={storeName} onChange={setStoreName} icon={<Storefront size={14} />} placeholder="Ej: Abarrotes Don Pedro" />
                <SettingsInput label="Slogan / Lema" value={storeSlogan} onChange={setStoreSlogan} placeholder="Ej: Siempre los mejores precios" hint="Opcional. Se muestra debajo del nombre en el ticket." />
                <SettingsInput label="Teléfono" value={phone} onChange={setPhone} icon={<Phone size={14} />} placeholder="555-123-4567" type="tel" />
                <SettingsInput label="Email de Contacto" value={email} onChange={setEmail} placeholder="contacto@mitienda.com" type="email" />
                <SettingsInput label="RFC / Identificación Fiscal" value={rfc} onChange={setRfc} placeholder="XAXX010101000" hint="Se imprime en tickets con desglose fiscal." />
              </div>
            )}

            {empresaSubTab === 'sucursal' && (
              <div>
                <SectionHeader icon={<MapPin size={20} className="text-accent" />} title="Datos de Sucursal" description="Identifica esta terminal de punto de venta." />
                <SettingsInput label="Nombre de Sucursal" value={sucursalName} onChange={setSucursalName} icon={<Buildings size={14} />} placeholder="Sucursal Centro" />
                <SettingsInput label="Código de Sucursal" value={sucursalCode} onChange={setSucursalCode} placeholder="SUC-001" hint="Código interno para identificar esta terminal." />
                <SettingsInput label="Dirección" value={address} onChange={setAddress} icon={<MapPin size={14} />} placeholder="Av. Principal #123, Col. Centro" />
              </div>
            )}

            {empresaSubTab === 'moneda' && (
              <div>
                <SectionHeader icon={<CurrencyDollar size={20} className="text-accent" />} title="Moneda e Impuestos" description="Configuración regional de moneda y tasas fiscales." />
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                  <SettingsInput label="Símbolo de Moneda" value={currencySymbol} onChange={setCurrencySymbol} placeholder="$" />
                  <SettingsSelect label="Código de Moneda" value={currencyCode} onChange={setCurrencyCode} options={[
                    { value: 'MXN', label: 'MXN - Peso Mexicano' },
                    { value: 'USD', label: 'USD - Dólar Americano' },
                    { value: 'EUR', label: 'EUR - Euro' },
                    { value: 'COP', label: 'COP - Peso Colombiano' },
                    { value: 'ARS', label: 'ARS - Peso Argentino' },
                  ]} />
                </div>
                <SettingsInput label="Tasa de Impuesto (%)" value={taxRate} onChange={setTaxRate} type="number" placeholder="16" hint="IVA o impuesto general aplicable. Se usa para el desglose fiscal en el ticket." />
              </div>
            )}

            {empresaSubTab === 'fiscal' && (
              <div>
                <SectionHeader icon={<Receipt size={20} className="text-accent" />} title="Configuración de Facturama (CFDI 4.0)" description="Credenciales para el timbrado de facturas y operaciones fiscales." />
                <SettingsInput label="Usuario de Facturama (API)" value={facturamaUser} onChange={setFacturamaUser} icon={<Buildings size={14} />} placeholder="tu_usuario_api" />
                <SettingsInput label="Contraseña / Secret (API)" value={facturamaSecret} onChange={setFacturamaSecret} placeholder="tu_password_api" type="password" hint="Utilizado para la autenticación Basic hacia el SAT vía Facturama." />
              </div>
            )}
          </div>
        );

      // ═══════════════════════════════════════════════════
      // INTERFAZ (Personalización)
      // ═══════════════════════════════════════════════════
      case 'interfaz':
        return (
          <div>
            <div style={{ display: 'flex', gap: '8px', marginBottom: '20px', flexWrap: 'wrap' }}>
              <button style={subTabStyle(interfaceSubTab === 'tema')} onClick={() => setInterfaceSubTab('tema')}>
                <Palette size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Tema
              </button>
              <button style={subTabStyle(interfaceSubTab === 'colores')} onClick={() => setInterfaceSubTab('colores')}>
                <PaintBrush size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Colores
              </button>
              <button style={subTabStyle(interfaceSubTab === 'fuentes')} onClick={() => setInterfaceSubTab('fuentes')}>
                <TextAa size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Fuentes
              </button>
              <button style={subTabStyle(interfaceSubTab === 'layout')} onClick={() => setInterfaceSubTab('layout')}>
                <Layout size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Disposición
              </button>
              <button style={subTabStyle(interfaceSubTab === 'animaciones')} onClick={() => setInterfaceSubTab('animaciones')}>
                <Lightning size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Animaciones
              </button>
            </div>

            {interfaceSubTab === 'tema' && (
              <div>
                <SectionHeader icon={<Palette size={20} className="text-accent" />} title="Modo de Apariencia" description="Controla el esquema de colores general de la aplicación." />
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px', marginBottom: '20px' }}>
                  <button
                    onClick={() => { if (theme === 'dark') toggleTheme(); }}
                    style={{
                      padding: '20px', borderRadius: '6px', cursor: 'pointer', transition: 'all 0.15s',
                      border: theme === 'light' ? '2px solid var(--accent-primary)' : '1px solid var(--border-color)',
                      backgroundColor: '#F3F4F6', color: '#111827', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px'
                    }}
                  >
                    <div style={{ width: '100%', height: '60px', borderRadius: '4px', backgroundColor: '#FFFFFF', border: '1px solid #D1D5DB', display: 'flex', alignItems: 'center', padding: '8px', gap: '8px' }}>
                      <div style={{ width: '12px', height: '40px', borderRadius: '2px', backgroundColor: '#083C90' }} />
                      <div style={{ flex: 1 }}>
                        <div style={{ height: '6px', backgroundColor: '#E5E7EB', borderRadius: '2px', marginBottom: '4px', width: '80%' }} />
                        <div style={{ height: '6px', backgroundColor: '#E5E7EB', borderRadius: '2px', width: '50%' }} />
                      </div>
                    </div>
                    <span style={{ fontSize: '13px', fontWeight: 600 }}>☀️ Modo Claro</span>
                    {theme === 'light' && <span style={{ fontSize: '10px', color: '#0F52BA', fontWeight: 700, textTransform: 'uppercase' }}>Activo</span>}
                  </button>

                  <button
                    onClick={() => { if (theme === 'light') toggleTheme(); }}
                    style={{
                      padding: '20px', borderRadius: '6px', cursor: 'pointer', transition: 'all 0.15s',
                      border: theme === 'dark' ? '2px solid #3B82F6' : '1px solid var(--border-color)',
                      backgroundColor: '#0F172A', color: '#F8FAFC', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px'
                    }}
                  >
                    <div style={{ width: '100%', height: '60px', borderRadius: '4px', backgroundColor: '#1E293B', border: '1px solid #334155', display: 'flex', alignItems: 'center', padding: '8px', gap: '8px' }}>
                      <div style={{ width: '12px', height: '40px', borderRadius: '2px', backgroundColor: '#3B82F6' }} />
                      <div style={{ flex: 1 }}>
                        <div style={{ height: '6px', backgroundColor: '#334155', borderRadius: '2px', marginBottom: '4px', width: '80%' }} />
                        <div style={{ height: '6px', backgroundColor: '#334155', borderRadius: '2px', width: '50%' }} />
                      </div>
                    </div>
                    <span style={{ fontSize: '13px', fontWeight: 600 }}>🌙 Modo Oscuro</span>
                    {theme === 'dark' && <span style={{ fontSize: '10px', color: '#3B82F6', fontWeight: 700, textTransform: 'uppercase' }}>Activo</span>}
                  </button>
                </div>
              </div>
            )}

            {interfaceSubTab === 'colores' && (
              <div>
                <SectionHeader icon={<PaintBrush size={20} className="text-accent" />} title="Color de Acento" description="El color principal usado en botones, elementos activos y resaltados." />
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(130px, 1fr))', gap: '10px', marginBottom: '24px' }}>
                  {ACCENT_PRESETS.map(preset => (
                    <button
                      key={preset.id}
                      onClick={() => { setAccentPreset(preset.id); applyAccentColor(preset.id); }}
                      style={{
                        padding: '12px 8px', borderRadius: '6px', cursor: 'pointer', transition: 'all 0.12s',
                        border: accentPreset === preset.id ? `2px solid ${preset.primary}` : '1px solid var(--border-color)',
                        backgroundColor: accentPreset === preset.id ? `${preset.primary}10` : 'var(--bg-primary)',
                        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '8px'
                      }}
                    >
                      <div style={{ width: '28px', height: '28px', borderRadius: '50%', backgroundColor: preset.primary, border: '2px solid rgba(255,255,255,0.2)' }} />
                      <span style={{ fontSize: '11px', fontWeight: 500, color: 'var(--text-secondary)' }}>{preset.label}</span>
                    </button>
                  ))}
                </div>

                <SectionHeader icon={<Layout size={20} className="text-accent" />} title="Color de Barra Lateral" description="Cambia el color de fondo de la barra de navegación lateral." />
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(130px, 1fr))', gap: '10px' }}>
                  {SIDEBAR_COLORS.map(preset => (
                    <button
                      key={preset.id}
                      onClick={() => { setSidebarColor(preset.id); applySidebarColor(preset.id); }}
                      style={{
                        padding: '12px 8px', borderRadius: '6px', cursor: 'pointer', transition: 'all 0.12s',
                        border: sidebarColor === preset.id ? '2px solid var(--accent-primary)' : '1px solid var(--border-color)',
                        backgroundColor: 'var(--bg-primary)', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '8px'
                      }}
                    >
                      <div style={{ width: '28px', height: '28px', borderRadius: '4px', backgroundColor: preset.color, border: '1px solid rgba(255,255,255,0.1)' }} />
                      <span style={{ fontSize: '11px', fontWeight: 500, color: 'var(--text-secondary)' }}>{preset.label}</span>
                    </button>
                  ))}
                </div>
              </div>
            )}

            {interfaceSubTab === 'fuentes' && (
              <div>
                <SectionHeader icon={<TextAa size={20} className="text-accent" />} title="Familia Tipográfica" description="Elige el estilo de letra para todo el sistema." />
                <SettingsSelect
                  label="Tipo de Letra / Familia"
                  value={fontFamily}
                  onChange={setFontFamily}
                  icon={<TextAa size={14} />}
                  options={[
                    { value: 'inter', label: 'Inter (Sans-Serif Moderno - Estándar)' },
                    { value: 'outfit', label: 'Outfit (Geométrico Limpio - Premium)' },
                    { value: 'roboto', label: 'Roboto (Técnico e Industrial)' },
                    { value: 'jetbrains', label: 'JetBrains Mono (Monoespaciado de Consola)' },
                    { value: 'montserrat', label: 'Montserrat (Elegante y Redondeado)' },
                    { value: 'playfair', label: 'Playfair Display (Serif Formal / Boutique)' },
                  ]}
                />

                <SectionHeader icon={<TextAa size={20} className="text-accent" />} title="Tamaño de Texto Global" description="Ajusta el tamaño base del texto en toda la interfaz." />
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  {FONT_SIZE_PRESETS.map(preset => (
                    <button
                      key={preset.id}
                      onClick={() => { setFontSizePreset(preset.id); applyFontSize(preset.id); }}
                      style={{
                        padding: '14px 16px', borderRadius: '4px', cursor: 'pointer', transition: 'all 0.12s',
                        border: fontSizePreset === preset.id ? '2px solid var(--accent-primary)' : '1px solid var(--border-color)',
                        backgroundColor: fontSizePreset === preset.id ? 'rgba(15, 82, 186, 0.06)' : 'var(--bg-primary)',
                        display: 'flex', justifyContent: 'space-between', alignItems: 'center', textAlign: 'left'
                      }}
                    >
                      <div>
                        <div style={{ fontSize: preset.base, fontWeight: 600, color: 'var(--text-primary)' }}>{preset.label}</div>
                        <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>Base: {preset.base} · Escala: {preset.scale}x</div>
                      </div>
                      <span style={{ fontSize: preset.base, color: 'var(--text-secondary)' }}>Aa</span>
                    </button>
                  ))}
                </div>
              </div>
            )}

            {interfaceSubTab === 'layout' && (
              <div>
                <SectionHeader icon={<Layout size={20} className="text-accent" />} title="Disposición de la Interfaz" description="Controla el diseño estructural de las áreas del POS." />
                <SettingsSelect
                  label="Posición de Barra Lateral"
                  value={sidebarPosition} onChange={setSidebarPosition}
                  icon={<Layout size={14} />}
                  options={[
                    { value: 'left', label: 'Izquierda (Predeterminado)' },
                    { value: 'right', label: 'Derecha' },
                    { value: 'top', label: 'Arriba (Barra Horizontal Superior)' },
                    { value: 'bottom', label: 'Abajo (Barra Horizontal Inferior)' },
                  ]}
                />
                <SettingsSelect
                  label="Ancho del Panel de Ticket"
                  value={ticketPanelWidth} onChange={setTicketPanelWidth}
                  options={[
                    { value: '340', label: 'Estrecho (340px)' },
                    { value: '400', label: 'Normal (400px)' },
                    { value: '480', label: 'Ancho (480px)' },
                    { value: '560', label: 'Extra Ancho (560px)' },
                  ]}
                />
                <SettingsSelect
                  label="Columnas de Productos"
                  value={productGridCols} onChange={setProductGridCols}
                  options={[
                    { value: 'auto', label: 'Auto (Adaptativo)' },
                    { value: '3', label: '3 columnas' },
                    { value: '4', label: '4 columnas' },
                    { value: '5', label: '5 columnas' },
                    { value: '6', label: '6 columnas' },
                  ]}
                />
                <SettingsToggle label="Modo Compacto" description="Reduce padding y márgenes para mayor densidad de información." checked={compactMode} onChange={setCompactMode} />
                <SettingsToggle label="Mostrar Badge de Stock" description="Muestra la cantidad disponible en cada tarjeta de producto." checked={showStockBadge} onChange={setShowStockBadge} />
                <SettingsToggle label="Mostrar Imágenes de Producto" description="Muestra iconos/imágenes en la cuadrícula de productos." checked={showProductImages} onChange={setShowProductImages} />
              </div>
            )}

            {interfaceSubTab === 'animaciones' && (
              <div>
                <SectionHeader icon={<Lightning size={20} className="text-accent" />} title="Movimiento y Transiciones" description="Controla la velocidad y presencia de animaciones en la UI." />
                <SettingsToggle label="Animaciones Habilitadas" description="Activa o desactiva todas las micro-animaciones de la interfaz." checked={showAnimations} onChange={setShowAnimations} />
                <SettingsToggle label="Efectos Hover" description="Efectos visuales al pasar el cursor sobre elementos interactivos." checked={hoverEffects} onChange={setHoverEffects} />
                <SettingsSelect
                  label="Velocidad de Transiciones"
                  value={transitionSpeed}
                  onChange={(v) => { setTransitionSpeed(v); applyTransitionSpeed(v); }}
                  options={[
                    { value: 'none', label: 'Sin transiciones' },
                    { value: 'fast', label: 'Rápida (80ms)' },
                    { value: 'normal', label: 'Normal (120ms)' },
                    { value: 'slow', label: 'Lenta (250ms)' },
                  ]}
                />
              </div>
            )}
          </div>
        );

      // ═══════════════════════════════════════════════════
      // TICKET
      // ═══════════════════════════════════════════════════
      case 'ticket':
        return (
          <div>
            <div style={{ display: 'flex', gap: '8px', marginBottom: '20px', flexWrap: 'wrap' }}>
              <button style={subTabStyle(ticketSubTab === 'formato')} onClick={() => setTicketSubTab('formato')}>
                <Printer size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Formato
              </button>
              <button style={subTabStyle(ticketSubTab === 'contenido')} onClick={() => setTicketSubTab('contenido')}>
                <Receipt size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Contenido
              </button>
              <button style={subTabStyle(ticketSubTab === 'preview')} onClick={() => setTicketSubTab('preview')}>
                <Eye size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Vista Previa
              </button>
            </div>

            {ticketSubTab === 'formato' && (
              <div>
                <SectionHeader icon={<Printer size={20} className="text-accent" />} title="Formato de Impresión" description="Ajustes del tamaño de papel, tipografía y salida de impresión." />
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                  <SettingsSelect label="Ancho de Papel" value={paperWidth} onChange={setPaperWidth} icon={<Printer size={14} />} options={[
                    { value: '80mm', label: '80mm (Estándar)' },
                    { value: '58mm', label: '58mm (Pequeño)' },
                    { value: '76mm', label: '76mm (Intermedio)' },
                  ]} />
                  <SettingsSelect label="Tamaño de Letra" value={ticketFontSize} onChange={setTicketFontSize} icon={<TextAa size={14} />} options={[
                    { value: '10px', label: 'Pequeña (10px)' },
                    { value: '11px', label: 'Mediana (11px)' },
                    { value: '12px', label: 'Normal (12px)' },
                    { value: '14px', label: 'Grande (14px)' },
                  ]} />
                </div>
                <SettingsSelect label="Copias por Ticket" value={printCopies} onChange={setPrintCopies} options={[
                  { value: '1', label: '1 copia' },
                  { value: '2', label: '2 copias' },
                  { value: '3', label: '3 copias' },
                ]} />
                <SettingsToggle label="Impresión Automática" description="Imprimir ticket automáticamente al completar una venta." checked={autoPrint} onChange={setAutoPrint} />
              </div>
            )}

            {ticketSubTab === 'contenido' && (
              <div>
                <SectionHeader icon={<Receipt size={20} className="text-accent" />} title="Contenido del Ticket" description="Qué información se muestra en el ticket impreso." />
                <SettingsToggle label="Mostrar Logo / Nombre" description="Encabezado con nombre del negocio y logo." checked={showLogo} onChange={setShowLogo} />
                <SettingsToggle label="Mostrar Código de Barras" description="Código de barras o QR de la transacción." checked={showBarcode} onChange={setShowBarcode} />
                <SettingsToggle label="Desglose de Impuestos" description="Muestra el IVA o impuesto desglosado en el ticket." checked={showTaxBreakdown} onChange={setShowTaxBreakdown} />
                <SettingsInput label="Mensaje de Pie de Ticket" value={footerMessage} onChange={setFooterMessage} placeholder="¡Gracias por su compra!" hint="Mensaje personalizado al final del ticket." />
              </div>
            )}

            {ticketSubTab === 'preview' && (
              <div>
                <SectionHeader icon={<Eye size={20} className="text-accent" />} title="Vista Previa del Ticket" description="Así se verá un ticket con la configuración actual." />
                <div style={{
                  padding: '24px', backgroundColor: '#fff', color: '#000', fontFamily: 'monospace',
                  borderRadius: '4px', border: '1px solid #D1D5DB', fontSize: ticketFontSize,
                  maxWidth: paperWidth === '58mm' ? '240px' : paperWidth === '76mm' ? '300px' : '320px',
                  margin: '0 auto', lineHeight: 1.6
                }}>
                  {showLogo && (
                    <div style={{ textAlign: 'center', marginBottom: '12px', borderBottom: '1px dashed #999', paddingBottom: '8px' }}>
                      <div style={{ fontWeight: 'bold', fontSize: '16px' }}>{storeName || 'Mi Tienda'}</div>
                      {storeSlogan && <div style={{ fontSize: '10px', color: '#666' }}>{storeSlogan}</div>}
                      <div style={{ fontSize: '10px', color: '#666' }}>{address}</div>
                      <div style={{ fontSize: '10px', color: '#666' }}>Tel: {phone}</div>
                      {rfc && <div style={{ fontSize: '10px', color: '#666' }}>RFC: {rfc}</div>}
                    </div>
                  )}
                  <div style={{ textAlign: 'center', fontSize: '10px', color: '#999', marginBottom: '8px' }}>
                    {new Date().toLocaleDateString()} {new Date().toLocaleTimeString()}
                  </div>
                  <div style={{ borderBottom: '1px dashed #999', paddingBottom: '8px', marginBottom: '8px' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <span>2x Coca-Cola 600ml</span><span>{currencySymbol}30.00</span>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <span>1x Sabritas Original</span><span>{currencySymbol}18.50</span>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <span>3x Pan Bimbo</span><span>{currencySymbol}105.00</span>
                    </div>
                  </div>
                  {showTaxBreakdown && (
                    <div style={{ fontSize: '10px', color: '#666', marginBottom: '4px' }}>
                      Subtotal: {currencySymbol}132.33 | IVA ({taxRate}%): {currencySymbol}21.17
                    </div>
                  )}
                  <div style={{ fontWeight: 'bold', fontSize: '16px', textAlign: 'center', padding: '8px 0', borderTop: '1px dashed #999' }}>
                    TOTAL: {currencySymbol}153.50
                  </div>
                  <div style={{ textAlign: 'center', fontSize: '10px', color: '#666', marginTop: '4px' }}>
                    Pagó: {currencySymbol}200.00 | Cambio: {currencySymbol}46.50
                  </div>
                  {showBarcode && (
                    <div style={{ textAlign: 'center', marginTop: '12px', padding: '8px', backgroundColor: '#f5f5f5', borderRadius: '2px' }}>
                      <div style={{ fontFamily: 'monospace', fontSize: '9px', letterSpacing: '2px' }}>|||||||||||||||||||||||</div>
                      <div style={{ fontSize: '9px', color: '#999' }}>VTA-1718267890-001</div>
                    </div>
                  )}
                  <div style={{ textAlign: 'center', marginTop: '12px', fontSize: '11px', color: '#333' }}>
                    {footerMessage}
                  </div>
                </div>
              </div>
            )}
          </div>
        );

      // ═══════════════════════════════════════════════════
      // CONEXIONES
      // ═══════════════════════════════════════════════════
      case 'conexiones':
        return (
          <div>
            <SectionHeader icon={<WifiHigh size={20} className="text-accent" />} title="Conexiones de Red Local" description="Conecta otros dispositivos en la misma red para visualizar datos en tiempo real." />

            <div style={{ padding: '16px', backgroundColor: 'var(--bg-tertiary)', borderRadius: '6px', border: '1px solid var(--border-color)', marginBottom: '16px' }}>
              <h4 style={{ fontSize: '14px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px', color: 'var(--accent-primary)' }}>
                <MonitorPlay size={20} /> Segunda Pantalla Cliente
              </h4>
              <p style={{ fontSize: '12px', color: 'var(--text-secondary)', marginBottom: '12px' }}>
                Muestra el carrito de compras en tiempo real al cliente.
              </p>
              <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
                <a href="/client-view" target="_blank" rel="noopener noreferrer"
                  style={{ padding: '8px 16px', backgroundColor: 'var(--accent-primary)', color: 'var(--text-on-accent)', borderRadius: '4px', textDecoration: 'none', fontSize: '12px', fontWeight: 600 }}>
                  ABRIR PANTALLA
                </a>
                <button onClick={() => { navigator.clipboard.writeText(`http://${localIp}:8080/client-view`); toast.success('URL copiada'); }}
                  style={{ padding: '8px 16px', background: 'transparent', border: '1px solid var(--border-color)', borderRadius: '4px', color: 'var(--text-primary)', cursor: 'pointer', fontSize: '12px' }}>
                  Copiar URL LAN
                </button>
              </div>
            </div>

            <div style={{ padding: '16px', backgroundColor: 'var(--bg-tertiary)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
              <h4 style={{ fontSize: '14px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px', color: 'var(--accent-success)' }}>
                <QrCode size={20} /> Kiosko Verificador (Webcam)
              </h4>
              <p style={{ fontSize: '12px', color: 'var(--text-secondary)', marginBottom: '12px' }}>
                Transforma una tablet vieja en un escáner de pasillo.
              </p>
              <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
                <a href="/kiosko" target="_blank" rel="noopener noreferrer"
                  style={{ padding: '8px 16px', backgroundColor: 'var(--accent-success)', color: 'var(--text-on-success)', borderRadius: '4px', textDecoration: 'none', fontSize: '12px', fontWeight: 600 }}>
                  ABRIR KIOSKO
                </a>
                <button onClick={() => { navigator.clipboard.writeText(`http://${localIp}:8080/kiosko`); toast.success('URL copiada'); }}
                  style={{ padding: '8px 16px', background: 'transparent', border: '1px solid var(--border-color)', borderRadius: '4px', color: 'var(--text-primary)', cursor: 'pointer', fontSize: '12px' }}>
                  Copiar URL LAN
                </button>
              </div>
            </div>
          </div>
        );

      // ═══════════════════════════════════════════════════
      // SEGURIDAD
      // ═══════════════════════════════════════════════════
      case 'seguridad':
        return (
          <div>
            <div style={{ display: 'flex', gap: '8px', marginBottom: '20px', flexWrap: 'wrap' }}>
              <button style={subTabStyle(seguridadSubTab === 'acceso')} onClick={() => setSeguridadSubTab('acceso')}>
                <ShieldCheck size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Control de Acceso
              </button>
              <button style={subTabStyle(seguridadSubTab === 'sesion')} onClick={() => setSeguridadSubTab('sesion')}>
                <ClockCounterClockwise size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Sesión
              </button>
              <button style={subTabStyle(seguridadSubTab === 'auditoria')} onClick={() => setSeguridadSubTab('auditoria')}>
                <Eye size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Auditoría
              </button>
            </div>

            {seguridadSubTab === 'acceso' && (
              <div>
                <SectionHeader icon={<ShieldCheck size={20} className="text-accent" />} title="Control de Acceso" description="Permisos y restricciones para operaciones sensibles." />
                <SettingsToggle label="Requerir PIN para Cancelar Ventas" description="Solicita autenticación de administrador para anular transacciones." checked={requirePinForVoids} onChange={setRequirePinForVoids} />
                <SettingsToggle label="Requerir PIN para Descuentos" description="Solicita autenticación para aplicar descuentos manuales." checked={requirePinForDiscounts} onChange={setRequirePinForDiscounts} />
                <SettingsInput label="Descuento Máximo Permitido (%)" value={maxDiscountPercent} onChange={setMaxDiscountPercent} type="number" hint="Porcentaje máximo que un cajero puede aplicar como descuento." />
                <SettingsToggle label="Solicitar Cambio de Contraseña Periódico" description="Fuerza al usuario a cambiar su contraseña cada 30 días." checked={requirePasswordChange} onChange={setRequirePasswordChange} />
              </div>
            )}

            {seguridadSubTab === 'sesion' && (
              <div>
                <SectionHeader icon={<ClockCounterClockwise size={20} className="text-accent" />} title="Gestión de Sesión" description="Controla timeouts y comportamiento de cierre automático." />
                <SettingsToggle label="Bloqueo Automático por Inactividad" description="Bloquea la pantalla tras un periodo de inactividad." checked={autoLock} onChange={setAutoLock} />
                <SettingsSelect label="Timeout de Inactividad" value={sessionTimeout} onChange={setSessionTimeout} options={[
                  { value: '5', label: '5 minutos' },
                  { value: '10', label: '10 minutos' },
                  { value: '15', label: '15 minutos' },
                  { value: '30', label: '30 minutos' },
                  { value: '60', label: '1 hora' },
                  { value: 'never', label: 'Nunca' },
                ]} />
                <SettingsToggle label="Permitir Sesiones Simultáneas" description="Permite que el mismo usuario inicie sesión en múltiples terminales." checked={allowMultipleSessions} onChange={setAllowMultipleSessions} />
              </div>
            )}

            {seguridadSubTab === 'auditoria' && (
              <div>
                <SectionHeader icon={<Eye size={20} className="text-accent" />} title="Registro de Auditoría" description="Rastrea todas las acciones importantes del sistema." />
                <SettingsToggle label="Registrar Eventos de Auditoría" description="Guarda un log de todas las acciones como ventas, cancelaciones, cambios de inventario." checked={showAuditTrail} onChange={setShowAuditTrail} />
                <div style={{ padding: '16px', backgroundColor: 'var(--bg-tertiary)', borderRadius: '4px', border: '1px solid var(--border-color)', marginTop: '16px' }}>
                  <p style={{ fontSize: '12px', color: 'var(--text-secondary)', marginBottom: '12px' }}>
                    Los registros de auditoría se almacenan localmente en la base de datos SQLite del sistema. Consulte la tabla <code>audit_log</code> para revisiones forenses.
                  </p>
                  <div style={{ display: 'flex', gap: '8px' }}>
                    <div style={{ padding: '8px 12px', backgroundColor: 'var(--bg-primary)', borderRadius: '4px', fontSize: '11px', display: 'flex', alignItems: 'center', gap: '4px' }}>
                      <Info size={14} color="var(--accent-primary)" /> Nivel: INFO, WARN, ERROR
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>
        );

      // ═══════════════════════════════════════════════════
      // DATOS
      // ═══════════════════════════════════════════════════
      case 'datos':
        return (
          <div>
            <div style={{ display: 'flex', gap: '8px', marginBottom: '20px', flexWrap: 'wrap' }}>
              <button style={subTabStyle(datosSubTab === 'respaldos')} onClick={() => setDatosSubTab('respaldos')}>
                <DownloadSimple size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Respaldos
              </button>
              <button style={subTabStyle(datosSubTab === 'limpieza')} onClick={() => setDatosSubTab('limpieza')}>
                <Trash size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Limpieza
              </button>
              <button style={subTabStyle(datosSubTab === 'exportar')} onClick={() => setDatosSubTab('exportar')}>
                <Export size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Exportar
              </button>
            </div>

            {datosSubTab === 'respaldos' && (
              <div>
                <SectionHeader icon={<DownloadSimple size={20} className="text-accent" />} title="Respaldos de Seguridad" description="Crea snapshots de tu base de datos para recuperación ante desastres." />
                <button
                  onClick={handleBackup}
                  style={{
                    width: '100%', padding: '16px', backgroundColor: 'var(--bg-primary)', color: 'var(--text-primary)',
                    border: '1px solid var(--border-color)', borderRadius: '4px', cursor: 'pointer',
                    display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px', fontWeight: 700, fontSize: '14px',
                    transition: 'all 0.12s', marginBottom: '12px'
                  }}
                  onMouseEnter={e => { e.currentTarget.style.borderColor = 'var(--accent-primary)'; e.currentTarget.style.backgroundColor = 'rgba(15,82,186,0.05)'; }}
                  onMouseLeave={e => { e.currentTarget.style.borderColor = 'var(--border-color)'; e.currentTarget.style.backgroundColor = 'var(--bg-primary)'; }}
                >
                  <FloppyDisk size={20} /> REALIZAR RESPALDO LOCAL AHORA
                </button>
                <p style={{ fontSize: '11px', color: 'var(--text-muted)', textAlign: 'center' }}>
                  Se crea un archivo .db en la carpeta de respaldos del equipo.
                </p>
              </div>
            )}

            {datosSubTab === 'limpieza' && (
              <div>
                <SectionHeader icon={<Trash size={20} style={{ color: 'var(--accent-danger)' }} />} title="Limpieza de Datos" description="Operaciones destructivas. Usa con precaución." />
                <div style={{ padding: '16px', backgroundColor: 'rgba(239, 68, 68, 0.05)', border: '1px solid rgba(239, 68, 68, 0.2)', borderRadius: '4px', marginBottom: '16px' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px' }}>
                    <Warning size={18} color="var(--accent-danger)" />
                    <span style={{ fontSize: '13px', fontWeight: 600, color: 'var(--accent-danger)' }}>Zona de Peligro</span>
                  </div>
                  <p style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                    Estas acciones son irreversibles. Se recomienda hacer un respaldo antes de proceder.
                  </p>
                </div>
                <button
                  onClick={async () => {
                    if (confirm('¿Borrar TODOS los registros de auditoría? Esta acción no se puede deshacer.')) {
                      try {
                        const { default: AppDatabase } = await import('@/lib/database');
                        const db = await AppDatabase.getInstance();
                        await db.execute('DELETE FROM audit_log;');
                        toast.success('Registros de auditoría eliminados');
                      } catch (e) { toast.error('Error al limpiar auditoría'); }
                    }
                  }}
                  style={{
                    width: '100%', padding: '12px', backgroundColor: 'transparent', color: 'var(--accent-danger)',
                    border: '1px solid var(--accent-danger)', borderRadius: '4px', cursor: 'pointer', fontWeight: 600,
                    fontSize: '13px', marginBottom: '8px'
                  }}
                >
                  Limpiar Registros de Auditoría
                </button>
                <button
                  onClick={async () => {
                    if (confirm('¿Borrar TODO el historial de ventas? Esta acción no se puede deshacer.')) {
                      try {
                        const { default: AppDatabase } = await import('@/lib/database');
                        const db = await AppDatabase.getInstance();
                        await db.execute('DELETE FROM sales;');
                        toast.success('Historial de ventas eliminado');
                      } catch (e) { toast.error('Error al limpiar ventas'); }
                    }
                  }}
                  style={{
                    width: '100%', padding: '12px', backgroundColor: 'transparent', color: 'var(--accent-danger)',
                    border: '1px solid var(--accent-danger)', borderRadius: '4px', cursor: 'pointer', fontWeight: 600,
                    fontSize: '13px', marginBottom: '8px'
                  }}
                >
                  Limpiar Historial de Ventas
                </button>
                <button
                  onClick={async () => {
                    if (confirm('¿Restaurar TODA la configuración a valores predeterminados?')) {
                      try {
                        const { default: AppDatabase } = await import('@/lib/database');
                        const db = await AppDatabase.getInstance();
                        await db.execute('DELETE FROM settings;');
                        toast.success('Configuración restaurada. Recarga la aplicación.');
                      } catch (e) { toast.error('Error al restaurar configuración'); }
                    }
                  }}
                  style={{
                    width: '100%', padding: '12px', backgroundColor: 'transparent', color: 'var(--accent-warning)',
                    border: '1px solid var(--accent-warning)', borderRadius: '4px', cursor: 'pointer', fontWeight: 600,
                    fontSize: '13px'
                  }}
                >
                  <ArrowCounterClockwise size={14} style={{ display: 'inline', marginRight: '4px', verticalAlign: 'middle' }} /> Restaurar Configuración Predeterminada
                </button>
              </div>
            )}

            {datosSubTab === 'exportar' && (
              <div>
                <SectionHeader icon={<Export size={20} className="text-accent" />} title="Exportar Datos" description="Exporta información del sistema en formatos compatibles." />
                <p style={{ fontSize: '12px', color: 'var(--text-muted)', marginBottom: '16px' }}>
                  Las opciones de exportación generan archivos locales que puedes compartir o archivar.
                </p>
                <button
                  onClick={() => toast.info('Función de exportación CSV en desarrollo.')}
                  style={{
                    width: '100%', padding: '14px', backgroundColor: 'var(--bg-primary)', color: 'var(--text-primary)',
                    border: '1px solid var(--border-color)', borderRadius: '4px', cursor: 'pointer',
                    display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px', fontWeight: 600,
                    fontSize: '13px', marginBottom: '8px'
                  }}
                >
                  <Export size={16} /> Exportar Inventario como CSV
                </button>
                <button
                  onClick={() => toast.info('Función de exportación de ventas en desarrollo.')}
                  style={{
                    width: '100%', padding: '14px', backgroundColor: 'var(--bg-primary)', color: 'var(--text-primary)',
                    border: '1px solid var(--border-color)', borderRadius: '4px', cursor: 'pointer',
                    display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px', fontWeight: 600,
                    fontSize: '13px'
                  }}
                >
                  <Export size={16} /> Exportar Historial de Ventas como CSV
                </button>
              </div>
            )}
          </div>
        );

      // ═══════════════════════════════════════════════════
      // NOTIFICACIONES
      // ═══════════════════════════════════════════════════
      case 'notificaciones':
        return (
          <div>
            <SectionHeader icon={<Bell size={20} className="text-accent" />} title="Alertas y Notificaciones" description="Configura los sonidos, alertas y posición de las notificaciones." />
            <SettingsToggle label="Sonido al Escanear Producto" description="Feedback auditivo de beep al detectar un código de barras." checked={soundOnScan} onChange={setSoundOnScan} />
            <SettingsToggle label="Sonido al Completar Venta" description="Sonido de confirmación al cerrar una transacción." checked={soundOnSale} onChange={setSoundOnSale} />
            <SettingsToggle label="Alerta de Stock Bajo" description="Notificación cuando un producto tiene pocas unidades." checked={lowStockAlert} onChange={setLowStockAlert} />
            {lowStockAlert && (
              <SettingsInput label="Umbral de Stock Bajo" value={lowStockThreshold} onChange={setLowStockThreshold} type="number" hint="Número de unidades para considerar stock como bajo." />
            )}
            <SettingsToggle label="Recordatorio de Cierre de Turno" description="Alerta si el turno lleva más de 8 horas abierto." checked={shiftReminder} onChange={setShiftReminder} />
            <SettingsSelect label="Posición de Notificaciones" value={toastPosition} onChange={setToastPosition} icon={<Megaphone size={14} />} options={[
              { value: 'top-right', label: 'Arriba a la derecha' },
              { value: 'top-left', label: 'Arriba a la izquierda' },
              { value: 'bottom-right', label: 'Abajo a la derecha' },
              { value: 'bottom-left', label: 'Abajo a la izquierda' },
              { value: 'top-center', label: 'Centro superior' },
              { value: 'bottom-center', label: 'Centro inferior' },
            ]} />
            <SettingsSelect label="Duración de Notificaciones" value={toastDuration} onChange={setToastDuration} options={[
              { value: '3', label: '3 segundos' },
              { value: '5', label: '5 segundos (estándar)' },
              { value: '8', label: '8 segundos' },
              { value: '10', label: '10 segundos' },
            ]} />
          </div>
        );

      // ═══════════════════════════════════════════════════
      // ATAJOS ZERO-MOUSE
      // ═══════════════════════════════════════════════════
      case 'atajos':
        return (
          <div>
            <SectionHeader icon={<Keyboard size={20} className="text-accent" />} title="Atajos de Teclado (Zero-Mouse)" description="NextVent está diseñado para que un cajero experto nunca suelte el teclado." />
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1px', backgroundColor: 'var(--border-color)', borderRadius: '6px', overflow: 'hidden' }}>
              {[
                { key: 'F1', desc: 'Focar barra de búsqueda (código de barras o nombre)', color: 'var(--accent-primary)' },
                { key: 'F5', desc: 'Abrir modal de cobro directo (Pasar a cobrar)', color: 'var(--accent-primary)' },
                { key: '+ (Num)', desc: 'Sumar 1 unidad al último artículo escaneado', color: 'var(--accent-success)' },
                { key: '- (Num)', desc: 'Restar 1 unidad al último artículo escaneado', color: 'var(--accent-danger)' },
                { key: 'Enter', desc: 'Ejecutar búsqueda / confirmar código escaneado', color: 'var(--accent-primary)' },
                { key: 'Esc', desc: 'Cerrar modal o diálogo activo', color: 'var(--text-muted)' },
              ].map((shortcut, i) => (
                <div key={i} style={{
                  display: 'flex', alignItems: 'center', gap: '16px', padding: '12px 16px',
                  backgroundColor: 'var(--bg-secondary)'
                }}>
                  <div style={{
                    minWidth: '80px', padding: '4px 10px', backgroundColor: 'var(--bg-tertiary)',
                    borderRadius: '4px', border: '1px solid var(--border-color)',
                    fontWeight: 700, fontSize: '12px', textAlign: 'center', color: shortcut.color,
                    fontFamily: 'monospace'
                  }}>
                    [ {shortcut.key} ]
                  </div>
                  <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>{shortcut.desc}</span>
                </div>
              ))}
            </div>
            <div style={{
              marginTop: '20px', padding: '16px', backgroundColor: 'var(--bg-tertiary)',
              borderRadius: '4px', border: '1px solid var(--border-color)'
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px' }}>
                <Barcode size={18} color="var(--accent-primary)" />
                <span style={{ fontSize: '13px', fontWeight: 600 }}>Escaneo de Código de Barras</span>
              </div>
              <p style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                NextVent detecta automáticamente lectores de código de barras USB. Simplemente escanea y el producto se agregará al ticket.
                La detección funciona por ráfagas rápidas de teclas (&lt;25ms entre caracteres).
              </p>
            </div>
          </div>
        );
      // ═══════════════════════════════════════════════════
      // USUARIOS
      // ═══════════════════════════════════════════════════
      case 'usuarios':
        return <SettingsUsers />;

      default:
        return null;
    }
  };

  return (
    <div style={{
      position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.75)', zIndex: 1000,
      display: 'flex', alignItems: 'center', justifyContent: 'center', backdropFilter: 'blur(6px)'
    }}>
      <div style={{
        backgroundColor: 'var(--bg-secondary)', borderRadius: '8px',
        width: '100%', maxWidth: '920px', maxHeight: '90vh',
        border: '1px solid var(--border-color)', display: 'flex', flexDirection: 'column',
        boxShadow: '0 25px 50px rgba(0,0,0,0.3)', overflow: 'hidden'
      }}>
        {/* ─── HEADER ──────────────────────────────────── */}
        <div style={{
          display: 'flex', justifyContent: 'space-between', alignItems: 'center',
          padding: '20px 24px', borderBottom: '1px solid var(--border-color)', flexShrink: 0
        }}>
          <h2 style={{ fontSize: '20px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '10px', color: 'var(--text-primary)' }}>
            <GearSix size={24} weight="regular" className="text-accent" /> Configuración del Sistema
          </h2>
          <button onClick={handleCloseAndReset} style={{
            background: 'transparent', border: '1px solid var(--border-color)', borderRadius: '4px',
            width: '36px', height: '36px', display: 'flex', alignItems: 'center', justifyContent: 'center',
            cursor: 'pointer', color: 'var(--text-secondary)', transition: 'all 0.12s'
          }}
          onMouseEnter={e => { e.currentTarget.style.backgroundColor = 'var(--bg-tertiary)'; e.currentTarget.style.color = 'var(--text-primary)'; }}
          onMouseLeave={e => { e.currentTarget.style.backgroundColor = 'transparent'; e.currentTarget.style.color = 'var(--text-secondary)'; }}
          >
            <X size={18} weight="regular" />
          </button>
        </div>

        {/* ─── BODY (sidebar + content) ────────────────── */}
        <div style={{ display: 'flex', flex: 1, overflow: 'hidden' }}>
          {/* Category sidebar */}
          <nav style={{
            width: '200px', flexShrink: 0, borderRight: '1px solid var(--border-color)',
            backgroundColor: 'var(--bg-primary)', display: 'flex', flexDirection: 'column',
            padding: '8px 0', overflowY: 'auto'
          }}>
            {categories.map(cat => (
              <button
                key={cat.id}
                onClick={() => setActiveCategory(cat.id)}
                style={{
                  display: 'flex', alignItems: 'center', gap: '10px',
                  padding: '12px 20px', border: 'none', cursor: 'pointer',
                  backgroundColor: activeCategory === cat.id ? 'var(--bg-secondary)' : 'transparent',
                  color: activeCategory === cat.id ? 'var(--accent-primary)' : 'var(--text-secondary)',
                  fontWeight: activeCategory === cat.id ? 600 : 400,
                  fontSize: '13px', textAlign: 'left', transition: 'all 0.1s',
                  borderLeft: activeCategory === cat.id ? '3px solid var(--accent-primary)' : '3px solid transparent',
                  fontFamily: 'Inter, sans-serif',
                }}
                onMouseEnter={e => { if (activeCategory !== cat.id) { e.currentTarget.style.backgroundColor = 'var(--bg-tertiary)'; } }}
                onMouseLeave={e => { if (activeCategory !== cat.id) { e.currentTarget.style.backgroundColor = 'transparent'; } }}
              >
                {cat.icon} {cat.label}
              </button>
            ))}

            <div style={{ marginTop: 'auto', padding: '12px 20px', borderTop: '1px solid var(--border-color)' }}>
              <div style={{ fontSize: '10px', color: 'var(--text-muted)', textAlign: 'center' }}>
                <strong>NextVent</strong> v2.0<br />
                Zima Technologies
              </div>
            </div>
          </nav>

          {/* Content panel */}
          <div style={{ flex: 1, padding: '24px', overflowY: 'auto' }}>
            {renderContent()}
          </div>
        </div>

        {/* ─── FOOTER ──────────────────────────────────── */}
        <div style={{
          display: 'flex', justifyContent: 'flex-end', gap: '12px',
          padding: '16px 24px', borderTop: '1px solid var(--border-color)',
          backgroundColor: 'var(--bg-primary)', flexShrink: 0
        }}>
          <button
            onClick={handleCloseAndReset}
            style={{
              padding: '10px 24px', backgroundColor: 'transparent', color: 'var(--text-secondary)',
              border: '1px solid var(--border-color)', borderRadius: '4px', cursor: 'pointer',
              fontSize: '13px', fontWeight: 500
            }}
          >
            Cancelar
          </button>
          <button
            onClick={handleSaveAll}
            style={{
              padding: '10px 28px', backgroundColor: 'var(--accent-primary)', color: 'var(--text-on-accent)',
              border: '1px solid var(--accent-hover)', borderRadius: '4px', cursor: 'pointer',
              fontSize: '13px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px',
              transition: 'all 0.12s'
            }}
            onMouseEnter={e => e.currentTarget.style.backgroundColor = 'var(--accent-hover)'}
            onMouseLeave={e => e.currentTarget.style.backgroundColor = 'var(--accent-primary)'}
          >
            <FloppyDisk size={16} /> GUARDAR TODO
          </button>
        </div>
      </div>
    </div>
  );
};
