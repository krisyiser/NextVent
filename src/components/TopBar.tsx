'use client';

import React, { useState, useEffect, useRef } from 'react';
import { Search, Maximize, User, Barcode, Scan } from 'lucide-react';
import { UserModal } from './UserModal';

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
    isMobileMode, 
    setIsMobileMode, 
    isScannerDetected,
    toggleKiosk,
    isKioskMode
}: TopBarProps) => {

  const [isUserOpen, setIsUserOpen] = useState(false);
  const [userName, setUserName] = useState('Administrador');
  const [barcodeInput, setBarcodeInput] = useState('');
  const barcodeRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const updateInfo = () => {
        setUserName(localStorage.getItem('pos_user_name') || 'Administrador');
    };
    
    updateInfo();
    
    const handleKeyDown = (e: KeyboardEvent) => {
        if (e.key === 'F1') {
            e.preventDefault();
            barcodeRef.current?.focus();
        }
        if (e.key === 'F11') {
            e.preventDefault();
            toggleKiosk();
        }
    };

    window.addEventListener('keydown', handleKeyDown);
    window.addEventListener('storage', updateInfo);
    
    return () => {
        window.removeEventListener('keydown', handleKeyDown);
        window.removeEventListener('storage', updateInfo);
    };
  }, [isUserOpen, toggleKiosk]);



  const handleBarcodeSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (barcodeInput.trim()) {
        onScan(barcodeInput.trim());
        setBarcodeInput('');
    }
  };

  return (
    <header className="top-bar">
      <div style={{ display: 'flex', gap: '16px', flex: 1, maxWidth: '1000px' }}>
        
        {/* Search Input */}
        <div className="search-container" style={{ flex: 3 }}>
            <Search size={18} color="var(--text-muted)" />
            <input
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
            <Barcode size={22} color={isScannerDetected ? 'var(--accent-success)' : 'var(--text-muted)'} />
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
          <Maximize size={20} />
        </button>

        <button className="icon-btn" title="Perfil de Usuario" onClick={() => setIsUserOpen(true)}>
          <User size={20} />
        </button>
      </div>


      <UserModal isOpen={isUserOpen} onClose={() => setIsUserOpen(false)} />
    </header>
  );

};
