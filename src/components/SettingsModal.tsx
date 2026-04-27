'use client';

import React, { useState, useEffect } from 'react';
import { X, Settings, Store, Printer, Save } from 'lucide-react';

type SettingsModalProps = {
  isOpen: boolean;
  onClose: () => void;
};

export const SettingsModal = ({ isOpen, onClose }: SettingsModalProps) => {
  const [storeName, setStoreName] = useState('Mi Tienda POS');
  const [address, setAddress] = useState('Av. Principal #123');
  const [phone, setPhone] = useState('555-123-4567');

  useEffect(() => {
    if (isOpen) {
      setStoreName(localStorage.getItem('pos_store_name') || 'Mi Tienda POS');
      setAddress(localStorage.getItem('pos_store_address') || 'Av. Principal #123');
      setPhone(localStorage.getItem('pos_store_phone') || '555-123-4567');
    }
  }, [isOpen]);

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();
    localStorage.setItem('pos_store_name', storeName);
    localStorage.setItem('pos_store_address', address);
    localStorage.setItem('pos_store_phone', phone);
    alert('Configuración guardada correctamente.');
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" style={{
      position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.8)', zIndex: 1000, 
      display: 'flex', alignItems: 'center', justifyContent: 'center', backdropFilter: 'blur(4px)'
    }}>
      <div className="modal-content" style={{
        backgroundColor: 'var(--bg-secondary)', padding: '32px', borderRadius: 'var(--radius-lg)',
        width: '100%', maxWidth: '500px', border: '1px solid var(--border-color)'
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
          <h2 style={{ fontSize: '24px', fontWeight: 'bold', display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Settings size={28} className="text-accent" /> Configuración del Sistema
          </h2>
          <button onClick={onClose} className="icon-btn"><X size={24} /></button>
        </div>

        <form onSubmit={handleSave} style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <div className="form-group">
            <label style={{ color: 'var(--text-secondary)', marginBottom: '8px', display: 'flex', alignItems: 'center', gap: '4px' }}>
              <Store size={16} /> Nombre del Negocio (Aparecerá en el Ticket)
            </label>
            <input 
              required
              value={storeName} 
              onChange={e => setStoreName(e.target.value)} 
              className="search-input" 
              style={{ backgroundColor: 'var(--bg-primary)' }} 
            />
          </div>

          <div className="form-group">
            <label style={{ color: 'var(--text-secondary)', marginBottom: '8px' }}>Dirección</label>
            <input 
              value={address} 
              onChange={e => setAddress(e.target.value)} 
              className="search-input" 
              style={{ backgroundColor: 'var(--bg-primary)' }} 
            />
          </div>

          <div className="form-group">
            <label style={{ color: 'var(--text-secondary)', marginBottom: '8px' }}>Teléfono</label>
            <input 
              value={phone} 
              onChange={e => setPhone(e.target.value)} 
              className="search-input" 
              style={{ backgroundColor: 'var(--bg-primary)' }} 
            />
          </div>

          <button type="submit" className="checkout-btn" style={{ marginTop: '16px', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px' }}>
            <Save size={20} /> GUARDAR CONFIGURACIÓN
          </button>
        </form>
      </div>
    </div>
  );
};
