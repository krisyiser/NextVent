'use client';

import React, { useState } from 'react';
import { ShoppingCart, Money, CheckCircle } from 'phosphor-react';
import { openShift } from '@/lib/storage';

type ShiftModalProps = {
  isOpen: boolean;
  onOpened: (shift: any) => void;
};

export const ShiftModal = ({ isOpen, onOpened }: ShiftModalProps) => {
  const [openingBalance, setOpeningBalance] = useState<string>('500');

  if (!isOpen) return null;

  const handleOpenShift = async () => {
    const shift = await openShift(parseFloat(openingBalance || '0'));
    onOpened(shift);
  };

  return (
    <div className="modal-overlay" style={{
      position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
      backgroundColor: 'rgba(15, 23, 42, 0.95)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 10000,
      backdropFilter: 'blur(8px)'
    }}>
      <div className="modal-content" style={{
        backgroundColor: 'var(--bg-secondary)', padding: '40px', borderRadius: 'var(--radius-lg)',
        width: '100%', maxWidth: '450px', border: '1px solid var(--border-color)', textAlign: 'center'
      }}>
        <div style={{ padding: '20px', backgroundColor: 'rgba(59, 130, 246, 0.1)', borderRadius: '50%', width: 'fit-content', margin: '0 auto 24px', color: 'var(--accent-primary)' }}>
          <ShoppingCart size={48} weight="regular" />
        </div>
        
        <h2 style={{ fontSize: '28px', fontWeight: 'bold', marginBottom: '8px' }}>Apertura de Caja</h2>
        <p style={{ color: 'var(--text-secondary)', marginBottom: '32px' }}>Ingresa el fondo inicial de efectivo al iniciar sesión.</p>

        <div style={{ marginBottom: '32px' }}>
          <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '12px' }}>Fondo de Caja (Cambio):</label>
          <div style={{ display: 'flex', alignItems: 'center', backgroundColor: 'var(--bg-tertiary)', borderRadius: 'var(--radius-md)', padding: '0 24px', border: '1px solid var(--border-color)' }}>
            <Money size={24} color="var(--text-muted)" weight="regular" />
            <span style={{ fontSize: '32px', marginLeft: '12px', fontWeight: 'bold' }}>$</span>
            <input 
              autoFocus
              type="number"
              value={openingBalance}
              onChange={e => setOpeningBalance(e.target.value)}
              style={{
                width: '100%', padding: '24px 12px', backgroundColor: 'transparent', border: 'none', color: 'var(--text-primary)', fontSize: '32px', outline: 'none', fontWeight: 'bold'
              }}
            />
          </div>
        </div>

        <button 
          onClick={handleOpenShift}
          style={{
            width: '100%', padding: '20px', borderRadius: 'var(--radius-md)', border: 'none',
            backgroundColor: 'var(--accent-primary)', color: 'var(--text-on-accent)', fontSize: '18px', fontWeight: 'bold', cursor: 'pointer',
            display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '12px', transition: 'var(--transition)',
            boxShadow: 'var(--shadow-glow)'
          }}
        >
          <CheckCircle size={24} weight="regular" />
          INICIAR TURNO
        </button>
      </div>
    </div>
  );
};
