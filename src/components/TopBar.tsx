'use client';

import React, { useState, useRef } from 'react';
import { MagnifyingGlass, CornersOut, Barcode, ShoppingCart } from 'phosphor-react';
import { SignOut } from 'phosphor-react';
import { useAuth } from '@/auth/AuthProvider';

type TopBarProps = {
  searchQuery: string;
  setSearchQuery: (query: string) => void;
  onScan: (barcode: string) => void;
  isMobileMode: boolean;
  setIsMobileMode: (mode: boolean) => void;
  isScannerDetected: boolean;
  toggleKiosk: () => void;
  isKioskMode: boolean;
};

export const TopBar = ({ 
    searchQuery, 
    setSearchQuery, 
    onScan, 
    isScannerDetected,
    toggleKiosk,
    isKioskMode
}: TopBarProps) => {

  const { auth, logout } = useAuth();
  const userName = auth.user || 'Administrador';
  const [barcodeInput, setBarcodeInput] = useState('');
  const barcodeRef = useRef<HTMLInputElement>(null);

  const handleBarcodeSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (barcodeInput.trim()) {
        onScan(barcodeInput.trim());
        setBarcodeInput('');
    }
  };

  return (
    <header className="top-bar">
      <div className="ticket-title" style={{ fontSize: '24px', display: 'flex', alignItems: 'center', gap: '12px', minWidth: 'max-content' }}>
        <ShoppingCart size={28} className="text-accent" weight="regular" />
        Punto de Venta
      </div>
      <div style={{ display: 'flex', gap: '16px', flex: 1, maxWidth: '1000px' }}>
        
        {/* Search Input */}
        <div className="search-container" style={{ flex: 3 }}>
            <MagnifyingGlass size={18} color="var(--text-muted)" weight="regular" />
            <input
            id="pos-search-input"
            type="text"
            className="search-input"
            placeholder="Buscar por nombre (Ej. Coca Cola)..."
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            />
        </div>

        {/* Barcode Scanner Input - Integrated Status Icon */}
        <form onSubmit={handleBarcodeSubmit} className="search-container" style={{ 
            width: '48px', flex: '0 0 auto', padding: '0', justifyContent: 'center',
            border: isScannerDetected ? '2px solid var(--accent-primary)' : '1px solid var(--border-color)', 
            backgroundColor: isScannerDetected ? 'rgba(59, 130, 246, 0.1)' : 'rgba(255, 255, 255, 0.05)',
            transition: 'all 0.3s ease',
            cursor: 'pointer'
        }} onClick={() => barcodeRef.current?.focus()}>
            <Barcode size={22} color={isScannerDetected ? 'var(--accent-success)' : 'var(--text-muted)'} weight="regular" />
            <input
            ref={barcodeRef}
            type="text"
            className="search-input"
            value={barcodeInput}
            onChange={e => setBarcodeInput(e.target.value)}
            style={{ position: 'absolute', opacity: 0, width: '1px', pointerEvents: 'none' }}
            />
            <button type="submit" style={{ display: 'none' }} />
        </form>

      </div>

      <div className="top-bar-actions">
        <div className="status-indicator">
          <div className="status-dot"></div>
          {userName}
        </div>

        <button 
            className="icon-btn" 
            title={isKioskMode ? "Salir de Modo Kiosko (F11)" : "Modo Kiosko (F11)"}
            onClick={toggleKiosk}
            style={{ backgroundColor: isKioskMode ? 'var(--accent-primary)' : 'transparent', color: isKioskMode ? '#fff' : 'inherit' }}
        >
          <CornersOut size={20} weight="regular" />
        </button>

        <button className="icon-btn" title="Cerrar Sesión" onClick={logout} style={{ color: 'var(--accent-danger)' }}>
          <SignOut size={20} weight="regular" />
        </button>
      </div>

    </header>
  );
};
