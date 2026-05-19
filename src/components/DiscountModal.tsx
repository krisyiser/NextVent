'use client';

import React, { useState } from 'react';
import { X, Percent, DollarSign, CheckCircle2 } from 'lucide-react';

type DiscountModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onApply: (amount: number) => void;
  currentDiscount: number;
  subtotal: number;
};

export const DiscountModal = ({ isOpen, onClose, onApply, currentDiscount, subtotal }: DiscountModalProps) => {
  const [amount, setAmount] = useState(currentDiscount.toString());
  const [type, setType] = useState<'fixed' | 'percent'>('fixed');

  if (!isOpen) return null;

  const handleSave = () => {
    let finalAmount = parseFloat(amount) || 0;
    if (type === 'percent') {
      finalAmount = (subtotal * finalAmount) / 100;
    }
    onApply(finalAmount);
    onClose();
  };

  return (
    <div className="modal-overlay" style={{
      position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
      backgroundColor: 'rgba(0,0,0,0.85)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1200,
      backdropFilter: 'blur(8px)'
    }}>
      <div className="modal-content" style={{
        backgroundColor: 'var(--bg-secondary)', padding: '32px', borderRadius: 'var(--radius-lg)',
        width: '100%', maxWidth: '400px', border: '1px solid var(--border-color)', boxShadow: 'var(--shadow-lg)'
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
          <h2 style={{ fontSize: '20px', fontWeight: 'bold', display: 'flex', alignItems: 'center', gap: '12px' }}>
            <Percent className="text-accent-primary" /> Descuento Global
          </h2>
          <button onClick={onClose} className="icon-btn"><X size={24} /></button>
        </div>

        <div style={{ marginBottom: '24px', display: 'flex', gap: '8px', padding: '4px', backgroundColor: 'var(--bg-tertiary)', borderRadius: 'var(--radius-md)' }}>
          <button 
            onClick={() => setType('fixed')}
            style={{ 
                flex: 1, padding: '10px', borderRadius: 'var(--radius-sm)', border: 'none', cursor: 'pointer',
                backgroundColor: type === 'fixed' ? 'var(--accent-primary)' : 'transparent',
                color: '#fff', fontWeight: 'bold', transition: 'var(--transition)'
            }}
          >
            MONTO ($)
          </button>
          <button 
            onClick={() => setType('percent')}
            style={{ 
                flex: 1, padding: '10px', borderRadius: 'var(--radius-sm)', border: 'none', cursor: 'pointer',
                backgroundColor: type === 'percent' ? 'var(--accent-primary)' : 'transparent',
                color: '#fff', fontWeight: 'bold', transition: 'var(--transition)'
            }}
          >
            PORCENTAJE (%)
          </button>
        </div>

        <div style={{ marginBottom: '32px' }}>
          <div style={{ display: 'flex', alignItems: 'center', backgroundColor: 'var(--bg-tertiary)', borderRadius: 'var(--radius-md)', padding: '0 16px', border: '1px solid var(--border-color)' }}>
            {type === 'fixed' ? <DollarSign size={20} className="text-muted" /> : <Percent size={20} className="text-muted" />}
            <input 
              autoFocus
              type="number"
              value={amount}
              onChange={e => setAmount(e.target.value)}
              style={{ width: '100%', padding: '16px', backgroundColor: 'transparent', border: 'none', color: '#fff', fontSize: '24px', outline: 'none' }}
              placeholder="0.00"
            />
          </div>
          {type === 'percent' && amount && (
            <div style={{ marginTop: '12px', fontSize: '14px', color: 'var(--accent-warning)', textAlign: 'right' }}>
              Equivale a: <b>${((subtotal * (parseFloat(amount) || 0)) / 100).toFixed(2)}</b>
            </div>
          )}
        </div>

        <button 
          onClick={handleSave}
          style={{ 
            width: '100%', padding: '20px', backgroundColor: 'var(--accent-success)', color: '#fff', 
            border: 'none', borderRadius: 'var(--radius-md)', fontWeight: 'bold', fontSize: '16px',
            display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '12px', cursor: 'pointer'
          }}
        >
          <CheckCircle2 size={24} /> APLICAR DESCUENTO
        </button>
      </div>
    </div>
  );
};
