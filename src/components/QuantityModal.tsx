'use client';

import React, { useState, useEffect } from 'react';
import { X, Scale, Calculator, CheckCircle2 } from 'lucide-react';
import { Product } from '@/types';

type QuantityModalProps = {
  isOpen: boolean;
  onClose: () => void;
  product: Product | null;
  onConfirm: (quantity: number) => void;
};

export const QuantityModal = ({ isOpen, onClose, product, onConfirm }: QuantityModalProps) => {
  const [quantity, setQuantity] = useState<string>('1');

  useEffect(() => {
    if (isOpen && product) {
      setQuantity(product.unit === 'Kg' ? '' : '1');
    }
  }, [isOpen, product]);

  if (!isOpen || !product) return null;

  const total = (parseFloat(quantity || '0') * product.price);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const q = parseFloat(quantity);
    if (!isNaN(q) && q > 0) {
      onConfirm(q);
      onClose();
    } else {
      alert("La cantidad debe ser mayor a cero");
    }
  };

  return (
    <div className="modal-overlay" style={{
      position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
      backgroundColor: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1100,
      backdropFilter: 'blur(4px)'
    }}>
      <div className="modal-content" style={{
        backgroundColor: 'var(--bg-secondary)', padding: '32px', borderRadius: 'var(--radius-lg)',
        width: '100%', maxWidth: '400px', border: '1px solid var(--border-color)', boxShadow: 'var(--shadow-lg)'
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
          <h2 style={{ fontSize: '20px', fontWeight: 'bold', display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Scale size={20} className="text-accent" />
            Venta por {product.unit}
          </h2>
          <button onClick={onClose} className="icon-btn"><X size={20} /></button>
        </div>

        <form onSubmit={handleSubmit}>
          <div style={{ marginBottom: '24px' }}>
            <div style={{ fontSize: '18px', fontWeight: 'bold', marginBottom: '8px' }}>{product.name}</div>
            <div style={{ color: 'var(--text-secondary)', fontSize: '14px' }}>Precio: ${product.price.toFixed(2)} por {product.unit}</div>
          </div>

          <div style={{ marginBottom: '24px' }}>
            <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>
               Ingresar {product.unit === 'Kg' ? 'Peso (Kg)' : 'Cantidad'}:
            </label>
            <div style={{ display: 'flex', alignItems: 'center', backgroundColor: 'var(--bg-tertiary)', borderRadius: 'var(--radius-md)', padding: '0 16px' }}>
              <input 
                autoFocus
                type="number"
                step="0.001"
                value={quantity}
                onChange={e => setQuantity(e.target.value)}
                style={{
                  width: '100%', padding: '16px', backgroundColor: 'transparent', border: 'none', color: '#fff', fontSize: '24px', outline: 'none', textAlign: 'center'
                }}
                placeholder="0.00"
              />
            </div>
          </div>

          <div style={{ textAlign: 'center', marginBottom: '32px', padding: '16px', backgroundColor: 'var(--bg-primary)', borderRadius: 'var(--radius-md)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span style={{ color: 'var(--text-secondary)' }}>Subtotal:</span>
            <span style={{ fontSize: '24px', fontWeight: 'bold', color: 'var(--accent-success)' }}>${total.toFixed(2)}</span>
          </div>

          <button 
            type="submit"
            disabled={!parseFloat(quantity || '0')}
            style={{
              width: '100%', padding: '18px', borderRadius: 'var(--radius-md)', border: 'none',
              backgroundColor: 'var(--accent-primary)', color: '#fff', fontSize: '16px', fontWeight: 'bold', cursor: 'pointer',
              display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px'
            }}
          >
            <CheckCircle2 size={20} />
            AGREGAR {quantity} {product.unit}
          </button>
        </form>
      </div>
    </div>
  );
};
