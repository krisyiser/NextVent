'use client';

import React, { useState } from 'react';
import { X, Money, CheckCircle, Warning } from 'phosphor-react';
import { closeShift } from '@/lib/storage';
import { Shift } from '@/types';
import { toast } from 'sonner';
import { captureAuditImage } from '@/lib/webcam';
import { useRouter } from 'next/navigation';

type BlindCloseShiftModalProps = {
  isOpen: boolean;
  onClose: () => void;
  shift: Shift | null;
};

const DENOMINATIONS = [1000, 500, 200, 100, 50, 20, 10, 5, 2, 1, 0.5];

export const BlindCloseShiftModal = ({ isOpen, onClose, shift }: BlindCloseShiftModalProps) => {
  const router = useRouter();
  const [counts, setCounts] = useState<Record<number, number>>(DENOMINATIONS.reduce((acc, curr) => ({ ...acc, [curr]: 0 }), {}));

  if (!isOpen || !shift) return null;

  const totalCalculated = Object.entries(counts).reduce((acc, [denom, count]) => acc + (parseFloat(denom) * count), 0);

  const handleCloseShift = async () => {
    try {
        // Blind reconciliation
        const diff = totalCalculated - shift.expectedBalance;
        const isDescuadre = Math.abs(diff) > 10; // Tolerance of +/- $10
        
        if (isDescuadre) {
            // Trigger webcam snapshot and Audit Log
            toast.warning(`Descuadre de caja detectado: $${diff.toFixed(2)}. Guardando bitácora de auditoría visual...`, { duration: 8000 });
            const ts = new Date().getTime();
            await captureAuditImage(`descuadre_turno_${shift.id}_${ts}`);
        } else {
            toast.success(`Arqueo de caja correcto. Turno cerrado exitosamente.`);
        }

        await closeShift(totalCalculated);
        router.push('/login'); 
    } catch (e) {
        console.error("Error al cerrar turno", e);
        toast.error("Error interno al intentar cerrar el turno.");
    }
  };

  return (
    <div className="modal-overlay" style={{
      position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
      backgroundColor: 'rgba(15, 23, 42, 0.95)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 10000,
      backdropFilter: 'blur(8px)'
    }}>
      <div className="modal-content" style={{
        backgroundColor: 'var(--bg-secondary)', padding: '32px', borderRadius: 'var(--radius-lg)',
        width: '100%', maxWidth: '600px', border: '1px solid var(--border-color)', maxHeight: '90vh', overflowY: 'auto'
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
          <div>
              <h2 style={{ fontSize: '24px', fontWeight: 'bold' }}>Arqueo a Ciegas</h2>
              <p style={{ color: 'var(--text-secondary)', fontSize: '14px' }}>Turno #{shift.id}</p>
          </div>
          <button onClick={onClose} className="icon-btn"><X size={24} /></button>
        </div>

        <div style={{ padding: '16px', backgroundColor: 'var(--bg-tertiary)', borderRadius: 'var(--radius-md)', marginBottom: '24px', textAlign: 'center' }}>
            <p style={{ color: 'var(--text-secondary)', fontSize: '14px', marginBottom: '8px' }}>Efectivo Físico Contado</p>
            <h3 style={{ fontSize: '32px', fontWeight: 'bold', color: 'var(--accent-primary)' }}>${totalCalculated.toFixed(2)}</h3>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '16px', marginBottom: '32px' }}>
            {DENOMINATIONS.map(d => (
                <div key={d} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', backgroundColor: 'var(--bg-tertiary)', padding: '12px', borderRadius: 'var(--radius-md)' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <Money size={20} color="var(--text-secondary)" />
                        <span style={{ fontWeight: 'bold' }}>${d}</span>
                    </div>
                    <input 
                        type="number" 
                        min="0"
                        value={counts[d] || ''}
                        onChange={(e) => setCounts({ ...counts, [d]: parseInt(e.target.value) || 0 })}
                        placeholder="0"
                        style={{ width: '80px', padding: '8px', backgroundColor: 'var(--bg-primary)', border: '1px solid var(--border-color)', borderRadius: '4px', color: 'var(--text-primary)', textAlign: 'right', outline: 'none' }}
                    />
                </div>
            ))}
        </div>

        <button 
          onClick={handleCloseShift}
          style={{
            width: '100%', padding: '16px', borderRadius: 'var(--radius-md)', border: 'none',
            backgroundColor: 'var(--accent-danger)', color: 'var(--text-on-danger)', fontSize: '16px', fontWeight: 'bold', cursor: 'pointer',
            display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '12px', transition: 'var(--transition)'
          }}
        >
          <CheckCircle size={24} weight="regular" />
          CONFIRMAR CORTE DE CAJA
        </button>
      </div>
    </div>
  );
};
