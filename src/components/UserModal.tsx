'use client';

import React, { useState, useEffect } from 'react';
import { X, User, LogOut, CheckCircle2, Shield, CreditCard, LayoutDashboard } from 'lucide-react';

type UserModalProps = {
  isOpen: boolean;
  onClose: () => void;
};

export const UserModal = ({ isOpen, onClose }: UserModalProps) => {
  const [selectedProfile, setSelectedProfile] = useState('Administrador');

  const profiles = [
    { id: 'admin', name: 'Administrador', icon: <Shield size={20} /> },
    { id: 'manager', name: 'Gerente', icon: <LayoutDashboard size={20} /> },
    { id: 'box1', name: 'Caja 1', icon: <CreditCard size={20} /> },
    { id: 'box2', name: 'Caja 2', icon: <CreditCard size={20} /> },
  ];

  useEffect(() => {
    if (isOpen) {
      setSelectedProfile(localStorage.getItem('pos_user_name') || 'Administrador');
    }
  }, [isOpen]);

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();
    localStorage.setItem('pos_user_name', selectedProfile);
    // Role can be derived or same as name for now as requested
    localStorage.setItem('pos_user_role', selectedProfile);
    
    // Dispatch a storage event so TopBar can update immediately without refresh
    window.dispatchEvent(new Event('storage'));
    
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" style={{
      position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.8)', zIndex: 1100, 
      display: 'flex', alignItems: 'center', justifyContent: 'center', backdropFilter: 'blur(8px)'
    }}>
      <div className="modal-content" style={{
        backgroundColor: 'var(--bg-secondary)', padding: '32px', borderRadius: 'var(--radius-lg)',
        width: '100%', maxWidth: '450px', border: '1px solid var(--border-color)',
        boxShadow: '0 25px 50px -12px rgba(0, 0, 0, 0.5)'
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
          <h2 style={{ fontSize: '24px', fontWeight: 'bold', display: 'flex', alignItems: 'center', gap: '12px' }}>
            <User size={28} className="text-accent-primary" /> Perfil de Usuario
          </h2>
          <button onClick={onClose} className="icon-btn"><X size={24} /></button>
        </div>

        <p style={{ color: 'var(--text-muted)', marginBottom: '24px', fontSize: '14px' }}>
          Selecciona el perfil activo para esta sesión. Los permisos y reportes se ajustarán automáticamente.
        </p>

        <form onSubmit={handleSave}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', marginBottom: '32px' }}>
            {profiles.map((profile) => (
              <button
                key={profile.id}
                type="button"
                onClick={() => setSelectedProfile(profile.name)}
                style={{
                  display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px',
                  padding: '20px', borderRadius: 'var(--radius-md)', border: '1px solid',
                  cursor: 'pointer', transition: 'all 0.2s ease',
                  backgroundColor: selectedProfile === profile.name ? 'rgba(59, 130, 246, 0.1)' : 'var(--bg-tertiary)',
                  borderColor: selectedProfile === profile.name ? 'var(--accent-primary)' : 'var(--border-color)',
                  color: selectedProfile === profile.name ? 'var(--accent-primary)' : 'var(--text-secondary)'
                }}
              >
                {profile.icon}
                <span style={{ fontWeight: '600', fontSize: '14px' }}>{profile.name}</span>
                {selectedProfile === profile.name && (
                   <div style={{ position: 'absolute', top: '8px', right: '8px' }}>
                     <CheckCircle2 size={14} />
                   </div>
                )}
              </button>
            ))}
          </div>

          <button type="submit" className="checkout-btn" style={{ 
            width: '100%', padding: '16px', display: 'flex', alignItems: 'center', 
            justifyContent: 'center', gap: '8px', fontWeight: 'bold'
          }}>
            <CheckCircle2 size={20} /> GUARDAR Y ACTIVAR PERFIL
          </button>
        </form>
      </div>
    </div>
  );
};
