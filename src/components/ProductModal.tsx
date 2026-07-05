'use client';

import React, { useEffect, useRef } from 'react';
import { X, FloppyDisk, Barcode, Package, Stack, Scan } from 'phosphor-react';
import { Product } from '@/types';
import { CATEGORIES } from '@/constants';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const productSchema = z.object({
  id: z.string().optional(),
  name: z.string().min(1, 'El nombre es obligatorio'),
  cost: z.number().min(0, 'El costo no puede ser negativo'),
  price: z.number().min(0.01, 'El precio debe ser mayor a 0'),
  wholesalePrice: z.number().min(0).optional(),
  wholesaleThreshold: z.number().min(0).optional(),
  stock: z.number().min(0, 'El stock no puede ser negativo'),
  category: z.string().min(1),
  unit: z.string().min(1),
  barcode: z.string().optional(),
});

type ProductFields = z.infer<typeof productSchema>;

type ProductModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onSave: (product: Product) => void;
  product?: Product | null;
  title: string;
};

export const ProductModal = ({ isOpen, onClose, onSave, product, title }: ProductModalProps) => {
  const barcodeRef = useRef<HTMLInputElement | null>(null);

  const { register, handleSubmit, reset, setValue, watch, formState: { errors } } = useForm<ProductFields>({
    resolver: zodResolver(productSchema),
    defaultValues: {
      name: '',
      cost: 0,
      price: 0,
      wholesalePrice: 0,
      wholesaleThreshold: 0,
      stock: 0,
      category: 'Abarrotes',
      unit: 'Pza',
      barcode: '',
    }
  });

  const watchPrice = watch('price') || 0;
  const watchCost = watch('cost') || 0;
  const margin = Math.max(0, watchPrice - watchCost);

  useEffect(() => {
    if (isOpen) {
      if (product) {
        reset({
          id: product.id,
          name: product.name,
          cost: product.cost,
          price: product.price,
          wholesalePrice: product.wholesalePrice || 0,
          wholesaleThreshold: product.wholesaleThreshold || 0,
          stock: product.stock,
          category: product.category,
          unit: product.unit,
          barcode: product.barcode || '',
        });
      } else {
        reset({
          name: '',
          cost: 0,
          price: 0,
          wholesalePrice: 0,
          wholesaleThreshold: 0,
          stock: 0,
          category: 'Abarrotes',
          unit: 'Pza',
          barcode: '',
        });
      }
      setTimeout(() => {
        barcodeRef.current?.focus();
        barcodeRef.current?.select();
      }, 100);
    }
  }, [isOpen, product, reset]);

  if (!isOpen) return null;

  const onSubmit = (data: ProductFields) => {
    const finalData: Product = {
      id: data.id || `PROD-${Date.now()}`,
      name: data.name,
      cost: data.cost,
      price: data.price,
      wholesalePrice: data.wholesalePrice || 0,
      wholesaleThreshold: data.wholesaleThreshold || 0,
      stock: data.stock,
      category: data.category,
      unit: data.unit,
      barcode: data.barcode || undefined,
      expiresSoon: product?.expiresSoon || false
    };
    onSave(finalData);
  };

  const handleFocus = (e: React.FocusEvent<HTMLInputElement>) => {
    e.target.select();
  };

  return (
    <div className="modal-overlay" style={{
      position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
      backgroundColor: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1100,
      backdropFilter: 'blur(4px)'
    }}>
      <div className="modal-content" style={{
        backgroundColor: 'var(--bg-secondary)', padding: '32px', borderRadius: 'var(--radius-sm)',
        width: '100%', maxWidth: '600px', border: '1px solid var(--border-color)', boxShadow: 'var(--shadow-lg)',
        maxHeight: '90vh', overflowY: 'auto', color: 'var(--text-primary)'
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
          <h2 style={{ fontSize: '20px', fontWeight: 'bold', display: 'flex', alignItems: 'center', gap: '12px', color: 'var(--accent-primary)' }}>
            <Package size={24} weight="regular" /> {title}
          </h2>
          <button onClick={onClose} className="icon-btn" style={{ border: '1px solid var(--border-color)', color: 'var(--text-primary)' }}><X size={20} /></button>
        </div>

        <form onSubmit={handleSubmit(onSubmit)}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px' }}>
            
            {/* Barcode - Full Width */}
            <div style={{ gridColumn: '1 / span 2' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                <label style={{ fontSize: '13px', fontWeight: '500', color: 'var(--text-secondary)' }}>Código de Barras</label>
              </div>
              <div style={{ 
                display: 'flex', alignItems: 'center', backgroundColor: 'var(--bg-primary)', borderRadius: 'var(--radius-sm)', padding: '0 16px', 
                border: '1px solid var(--border-color)',
                transition: 'border 0.2s'
              }}>
                <Barcode size={20} className="text-muted" style={{ marginRight: '12px' }} weight="regular" />
                <input 
                  type="text"
                  {...register('barcode')}
                  ref={(e) => {
                    register('barcode').ref(e);
                    barcodeRef.current = e;
                  }}
                  onFocus={handleFocus}
                  style={{ width: '100%', padding: '12px 0', backgroundColor: 'transparent', border: 'none', color: 'var(--text-primary)', fontSize: '15px', outline: 'none' }}
                  placeholder="0000000000000"
                />
              </div>
            </div>

            {/* Name - Full Width */}
            <div style={{ gridColumn: '1 / span 2' }}>
              <label style={{ display: 'block', fontSize: '13px', fontWeight: '500', color: 'var(--text-secondary)', marginBottom: '8px' }}>Nombre del Producto</label>
              <input 
                type="text"
                {...register('name')}
                onFocus={handleFocus}
                style={{ width: '100%', padding: '12px 16px', backgroundColor: 'var(--bg-primary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-sm)', color: 'var(--text-primary)', fontSize: '15px', outline: 'none' }}
                placeholder="Ej. Coca Cola 600ml"
              />
              {errors.name && <p style={{ color: 'var(--accent-danger)', fontSize: '12px', marginTop: '4px' }}>{errors.name.message}</p>}
            </div>

            {/* Category */}
            <div>
              <label style={{ display: 'block', fontSize: '13px', fontWeight: '500', color: 'var(--text-secondary)', marginBottom: '8px' }}>Categoría</label>
              <select 
                {...register('category')}
                style={{ width: '100%', padding: '12px 16px', backgroundColor: 'var(--bg-primary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-sm)', color: 'var(--text-primary)', fontSize: '15px', outline: 'none' }}
              >
                {CATEGORIES.filter(c => c !== 'Todos').map(c => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </select>
            </div>

            {/* Unit */}
            <div>
              <label style={{ display: 'block', fontSize: '13px', fontWeight: '500', color: 'var(--text-secondary)', marginBottom: '8px' }}>Unidad de Medida</label>
              <select 
                {...register('unit')}
                style={{ width: '100%', padding: '12px 16px', backgroundColor: 'var(--bg-primary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-sm)', color: 'var(--text-primary)', fontSize: '15px', outline: 'none' }}
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
              <label style={{ display: 'block', fontSize: '13px', fontWeight: '500', color: 'var(--text-secondary)', marginBottom: '8px' }}>Costo de Compra</label>
              <div style={{ display: 'flex', alignItems: 'center', backgroundColor: 'var(--bg-primary)', borderRadius: 'var(--radius-sm)', padding: '0 16px', border: '1px solid var(--border-color)' }}>
                <span style={{ color: 'var(--text-muted)', marginRight: '8px' }}>$</span>
                <input 
                  type="number" step="0.01"
                  {...register('cost', { valueAsNumber: true })}
                  onFocus={handleFocus}
                  style={{ width: '100%', padding: '12px 0', backgroundColor: 'transparent', border: 'none', color: 'var(--text-primary)', fontSize: '15px', outline: 'none' }}
                />
              </div>
              {errors.cost && <p style={{ color: 'var(--accent-danger)', fontSize: '12px', marginTop: '4px' }}>{errors.cost.message}</p>}
            </div>

            {/* Price */}
            <div>
              <label style={{ display: 'block', fontSize: '13px', fontWeight: '500', color: 'var(--text-secondary)', marginBottom: '8px' }}>Precio de Venta</label>
              <div style={{ display: 'flex', alignItems: 'center', backgroundColor: 'var(--bg-primary)', borderRadius: 'var(--radius-sm)', padding: '0 16px', border: '1px solid var(--border-color)' }}>
                <span style={{ color: 'var(--accent-success)', marginRight: '8px' }}>$</span>
                <input 
                  type="number" step="0.01"
                  {...register('price', { valueAsNumber: true })}
                  onFocus={handleFocus}
                  style={{ width: '100%', padding: '12px 0', backgroundColor: 'transparent', border: 'none', color: 'var(--text-primary)', fontSize: '15px', outline: 'none' }}
                />
              </div>
              {errors.price && <p style={{ color: 'var(--accent-danger)', fontSize: '12px', marginTop: '4px' }}>{errors.price.message}</p>}
            </div>

            {/* Wholesale Price */}
            <div>
              <label style={{ display: 'block', fontSize: '13px', fontWeight: '500', color: 'var(--text-secondary)', marginBottom: '8px' }}>Precio Mayoreo</label>
              <div style={{ display: 'flex', alignItems: 'center', backgroundColor: 'var(--bg-primary)', borderRadius: 'var(--radius-sm)', padding: '0 16px', border: '1px solid var(--border-color)' }}>
                <span style={{ color: 'var(--accent-warning)', marginRight: '8px' }}>$</span>
                <input 
                  type="number" step="0.01"
                  {...register('wholesalePrice', { valueAsNumber: true })}
                  onFocus={handleFocus}
                  style={{ width: '100%', padding: '12px 0', backgroundColor: 'transparent', border: 'none', color: 'var(--text-primary)', fontSize: '15px', outline: 'none' }}
                />
              </div>
            </div>

            {/* Wholesale Threshold */}
            <div>
              <label style={{ display: 'block', fontSize: '13px', fontWeight: '500', color: 'var(--text-secondary)', marginBottom: '8px' }}>Cant. para Mayoreo</label>
              <input 
                type="number"
                {...register('wholesaleThreshold', { valueAsNumber: true })}
                onFocus={handleFocus}
                style={{ width: '100%', padding: '12px 16px', backgroundColor: 'var(--bg-primary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-sm)', color: 'var(--text-primary)', fontSize: '15px', outline: 'none' }}
              />
            </div>

            {/* Stock */}
            <div>
              <label style={{ display: 'block', fontSize: '13px', fontWeight: '500', color: 'var(--text-secondary)', marginBottom: '8px' }}>Stock Actual</label>
              <div style={{ display: 'flex', alignItems: 'center', backgroundColor: 'var(--bg-primary)', borderRadius: 'var(--radius-sm)', padding: '0 16px', border: '1px solid var(--border-color)' }}>
                <Stack size={18} className="text-muted" style={{ marginRight: '12px' }} weight="regular" />
                <input 
                  type="number"
                  {...register('stock', { valueAsNumber: true })}
                  onFocus={handleFocus}
                  style={{ width: '100%', padding: '12px 0', backgroundColor: 'transparent', border: 'none', color: 'var(--text-primary)', fontSize: '15px', outline: 'none' }}
                />
              </div>
              {errors.stock && <p style={{ color: 'var(--accent-danger)', fontSize: '12px', marginTop: '4px' }}>{errors.stock.message}</p>}
            </div>

            {/* Margin Preview */}
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', backgroundColor: 'rgba(16, 82, 186, 0.05)', borderRadius: 'var(--radius-sm)', padding: '12px', border: '1px solid rgba(16, 82, 186, 0.1)' }}>
               <div style={{ textAlign: 'center' }}>
                  <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>Ganancia x Unidad</div>
                  <div style={{ fontSize: '20px', fontWeight: 'bold', color: 'var(--accent-primary)' }}>
                    ${margin.toFixed(2)}
                  </div>
               </div>
            </div>

          </div>

          <div style={{ marginTop: '32px', display: 'flex', gap: '12px' }}>
            <button 
              type="button" onClick={onClose}
              style={{ flex: 1, padding: '16px', backgroundColor: 'var(--bg-tertiary)', border: 'none', borderRadius: 'var(--radius-sm)', color: 'var(--text-secondary)', fontWeight: 'bold', cursor: 'pointer' }}
            >
              CANCELAR
            </button>
            <button 
              type="submit"
              style={{ flex: 2, padding: '16px', backgroundColor: 'var(--accent-primary)', border: 'none', borderRadius: 'var(--radius-sm)', color: 'var(--text-on-accent)', fontWeight: 'bold', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px' }}
            >
              <FloppyDisk size={20} weight="regular" /> GUARDAR PRODUCTO
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

