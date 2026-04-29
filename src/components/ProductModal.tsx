'use client';

import React, { useState, useEffect, useRef } from 'react';
import { X, Save, Barcode, Package, DollarSign, Layers, Tag, ScanLine } from 'lucide-react';
import { Product } from '@/types';
import { CATEGORIES } from '@/constants';

type ProductModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onSave: (product: Product) => void;
  product?: Product | null;
  title: string;
};

export const ProductModal = ({ isOpen, onClose, onSave, product, title }: ProductModalProps) => {
  const [formData, setFormData] = useState<any>({
    id: '',
    name: '',
    cost: '',
    price: '',
    wholesalePrice: '',
    wholesaleThreshold: '',
    stock: '',
    category: 'Abarrotes',
    unit: 'Pza',
    barcode: ''
  });

  const [isScanning, setIsScanning] = useState(false);
  const barcodeRef = useRef<HTMLInputElement>(null);
  const nameRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (isOpen) {
      if (product) {
        setFormData({ ...product });
      } else {
        setFormData({
          id: ``,
          name: '',
          cost: '',
          price: '',
          wholesalePrice: '',
          wholesaleThreshold: '',
          stock: '',
          category: 'Abarrotes',
          unit: 'Pza',
          barcode: ''
        });

      }
      // Focus barcode field after a short delay to ensure modal is rendered
      setTimeout(() => {
          barcodeRef.current?.focus();
          barcodeRef.current?.select();
      }, 100);
    }
  }, [isOpen, product]);

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const finalData = {
      ...formData,
      cost: formData.cost === '' ? 0 : formData.cost,
      price: formData.price === '' ? 0 : formData.price,
      wholesalePrice: formData.wholesalePrice === '' ? 0 : formData.wholesalePrice,
      wholesaleThreshold: formData.wholesaleThreshold === '' ? 0 : formData.wholesaleThreshold,
      stock: formData.stock === '' ? 0 : formData.stock,
    };
    onSave(finalData);
  };

  const handleScanClick = () => {
    setIsScanning(true);
    barcodeRef.current?.focus();
    barcodeRef.current?.select();
    setTimeout(() => setIsScanning(false), 2000);
  };

  const handleBarcodeKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      nameRef.current?.focus();
    }
  };

  // Helper to handle auto-selection of text on focus and click to fix "0" value issue
  const handleFocus = (e: React.FocusEvent<HTMLInputElement>) => {
    e.target.select();
  };
  
  const handleClick = (e: React.MouseEvent<HTMLInputElement>) => {
    (e.target as HTMLInputElement).select();
  };


  return (
    <div className="modal-overlay" style={{
      position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
      backgroundColor: 'rgba(0,0,0,0.85)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1100,
      backdropFilter: 'blur(8px)'
    }}>
      <div className="modal-content" style={{
        backgroundColor: 'var(--bg-secondary)', padding: '32px', borderRadius: 'var(--radius-lg)',
        width: '100%', maxWidth: '600px', border: '1px solid var(--border-color)', boxShadow: 'var(--shadow-lg)',
        maxHeight: '90vh', overflowY: 'auto'
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
          <h2 style={{ fontSize: '24px', fontWeight: 'bold', display: 'flex', alignItems: 'center', gap: '12px' }}>
            <Package className="text-accent-primary" /> {title}
          </h2>
          <button onClick={onClose} className="icon-btn"><X size={24} /></button>
        </div>

        <form onSubmit={handleSubmit}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px' }}>
            
            {/* Barcode - Full Width */}
            <div style={{ gridColumn: '1 / span 2' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                <label style={{ fontSize: '14px', color: 'var(--text-secondary)' }}>Código de Barras</label>
                <button 
                  type="button" 
                  onClick={handleScanClick}
                  style={{ 
                    display: 'flex', alignItems: 'center', gap: '6px', fontSize: '12px', padding: '4px 12px', 
                    borderRadius: '20px', border: 'none', cursor: 'pointer',
                    backgroundColor: isScanning ? 'var(--accent-success)' : 'var(--bg-tertiary)',
                    color: '#fff', transition: 'var(--transition)'
                  }}
                >
                  <ScanLine size={14} /> {isScanning ? 'ESCANEANDO...' : 'MODO SCANNER'}
                </button>
              </div>
              <div style={{ 
                display: 'flex', alignItems: 'center', backgroundColor: 'var(--bg-tertiary)', borderRadius: 'var(--radius-md)', padding: '0 16px', 
                border: isScanning ? '2px solid var(--accent-success)' : '1px solid var(--border-color)',
                transition: 'border 0.2s'
              }}>
                <Barcode size={20} className="text-muted" style={{ marginRight: '12px' }} />
                <input 
                  ref={barcodeRef}
                  type="text"
                  value={formData.barcode}
                  onChange={e => setFormData({ ...formData, barcode: e.target.value })}
                  onKeyDown={handleBarcodeKeyDown}
                  onFocus={handleFocus}
                  style={{ width: '100%', padding: '12px 0', backgroundColor: 'transparent', border: 'none', color: '#fff', fontSize: '16px', outline: 'none' }}
                  placeholder="0000000000000"
                />
              </div>
            </div>

            {/* Name - Full Width */}
            <div style={{ gridColumn: '1 / span 2' }}>
              <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>Nombre del Producto</label>
              <input 
                ref={nameRef}
                required
                type="text"
                value={formData.name}
                onFocus={handleFocus}
                onChange={e => setFormData({ ...formData, name: e.target.value })}
                style={{ width: '100%', padding: '12px 16px', backgroundColor: 'var(--bg-tertiary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)', color: '#fff', fontSize: '16px', outline: 'none' }}
                placeholder="Ej. Coca Cola 600ml"
              />
            </div>

            {/* Category */}
            <div>
              <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>Categoría</label>
              <select 
                value={formData.category}
                onChange={e => setFormData({ ...formData, category: e.target.value })}
                style={{ width: '100%', padding: '12px 16px', backgroundColor: 'var(--bg-tertiary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)', color: '#fff', fontSize: '16px', outline: 'none' }}
              >
                {CATEGORIES.filter(c => c !== 'Todos').map(c => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </select>
            </div>

            {/* Unit */}
            <div>
              <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>Unidad de Medida</label>
              <select 
                value={formData.unit}
                onChange={e => setFormData({ ...formData, unit: e.target.value })}
                style={{ width: '100%', padding: '12px 16px', backgroundColor: 'var(--bg-tertiary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)', color: '#fff', fontSize: '16px', outline: 'none' }}
              >
                <option value="Pza">Pieza (Pza)</option>
                <option value="Kg">Kilogramo (Kg)</option>
                <option value="Paq">Paquete (Paq)</option>
                <option value="Caja">Caja</option>
                <option value="Lt">Litro (Lt)</option>
              </select>
            </div>

            {/* Cost */}
            <div>
              <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>Costo de Compra</label>
              <div style={{ display: 'flex', alignItems: 'center', backgroundColor: 'var(--bg-tertiary)', borderRadius: 'var(--radius-md)', padding: '0 16px', border: '1px solid var(--border-color)' }}>
                <span style={{ color: 'var(--text-muted)', marginRight: '8px' }}>$</span>
                <input 
                  type="number" step="0.01"
                  value={formData.cost}
                  onFocus={handleFocus}
                  onClick={handleClick}
                  onChange={e => setFormData({ ...formData, cost: e.target.value === '' ? '' : parseFloat(e.target.value) })}
                  style={{ width: '100%', padding: '12px 0', backgroundColor: 'transparent', border: 'none', color: '#fff', fontSize: '16px', outline: 'none' }}
                />
              </div>
            </div>

            {/* Price */}
            <div>
              <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>Precio de Venta</label>
              <div style={{ display: 'flex', alignItems: 'center', backgroundColor: 'var(--bg-tertiary)', borderRadius: 'var(--radius-md)', padding: '0 16px', border: '1px solid var(--border-color)' }}>
                <span style={{ color: 'var(--accent-success)', marginRight: '8px' }}>$</span>
                <input 
                  required
                  type="number" step="0.01"
                  value={formData.price}
                  onFocus={handleFocus}
                  onClick={handleClick}
                  onChange={e => setFormData({ ...formData, price: e.target.value === '' ? '' : parseFloat(e.target.value) })}
                  style={{ width: '100%', padding: '12px 0', backgroundColor: 'transparent', border: 'none', color: '#fff', fontSize: '16px', outline: 'none' }}
                />
              </div>
            </div>

            {/* Wholesale Price */}
            <div>
              <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>Precio Mayoreo</label>
              <div style={{ display: 'flex', alignItems: 'center', backgroundColor: 'var(--bg-tertiary)', borderRadius: 'var(--radius-md)', padding: '0 16px', border: '1px solid var(--border-color)' }}>
                <span style={{ color: 'var(--accent-warning)', marginRight: '8px' }}>$</span>
                <input 
                  type="number" step="0.01"
                  value={formData.wholesalePrice}
                  onFocus={handleFocus}
                  onClick={handleClick}
                  onChange={e => setFormData({ ...formData, wholesalePrice: e.target.value === '' ? '' : parseFloat(e.target.value) })}
                  style={{ width: '100%', padding: '12px 0', backgroundColor: 'transparent', border: 'none', color: '#fff', fontSize: '16px', outline: 'none' }}
                />
              </div>
            </div>

            {/* Wholesale Threshold */}
            <div>
              <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>Cant. para Mayoreo</label>
              <input 
                type="number"
                value={formData.wholesaleThreshold}
                onFocus={handleFocus}
                onClick={handleClick}
                onChange={e => setFormData({ ...formData, wholesaleThreshold: e.target.value === '' ? '' : parseInt(e.target.value) })}
                style={{ width: '100%', padding: '12px 16px', backgroundColor: 'var(--bg-tertiary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)', color: '#fff', fontSize: '16px', outline: 'none' }}
              />
            </div>

            {/* Stock */}
            <div>
              <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>Stock Actual</label>
              <div style={{ display: 'flex', alignItems: 'center', backgroundColor: 'var(--bg-tertiary)', borderRadius: 'var(--radius-md)', padding: '0 16px', border: '1px solid var(--border-color)' }}>
                <Layers size={18} className="text-muted" style={{ marginRight: '12px' }} />
                <input 
                  type="number"
                  value={formData.stock}
                  onFocus={handleFocus}
                  onClick={handleClick}
                  onChange={e => setFormData({ ...formData, stock: e.target.value === '' ? '' : parseFloat(e.target.value) })}
                  style={{ width: '100%', padding: '12px 0', backgroundColor: 'transparent', border: 'none', color: '#fff', fontSize: '16px', outline: 'none' }}
                />

              </div>
            </div>

            {/* Margin Preview */}
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', backgroundColor: 'rgba(34, 197, 94, 0.1)', borderRadius: 'var(--radius-md)', padding: '12px' }}>
               <div style={{ textAlign: 'center' }}>
                  <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Ganancia x Unidad</div>
                  <div style={{ fontSize: '20px', fontWeight: 'bold', color: 'var(--accent-success)' }}>
                    ${((parseFloat(formData.price as string) || 0) - (parseFloat(formData.cost as string) || 0)).toFixed(2)}
                  </div>
               </div>
            </div>

          </div>

          <div style={{ marginTop: '32px', display: 'flex', gap: '12px' }}>
            <button 
              type="button" onClick={onClose}
              style={{ flex: 1, padding: '16px', backgroundColor: 'var(--bg-tertiary)', border: 'none', borderRadius: 'var(--radius-md)', color: '#fff', fontWeight: 'bold', cursor: 'pointer' }}
            >
              CANCELAR
            </button>
            <button 
              type="submit"
              style={{ flex: 2, padding: '16px', backgroundColor: 'var(--accent-primary)', border: 'none', borderRadius: 'var(--radius-md)', color: '#fff', fontWeight: 'bold', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px' }}
            >
              <Save size={20} /> GUARDAR PRODUCTO
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
