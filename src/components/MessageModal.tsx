'use client';

import React, { useState, useEffect } from 'react';
import { X, ChatCircle, PaperPlaneTilt, UsersThree } from 'phosphor-react';
import { getCustomers } from '@/lib/storage';
import { Customer } from '@/types';
import { toast } from 'sonner';

type MessageModalProps = {
  isOpen: boolean;
  onClose: () => void;
};

export const MessageModal = ({ isOpen, onClose }: MessageModalProps) => {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [selectedCustomerId, setSelectedCustomerId] = useState('');
  const [message, setMessage] = useState('Hola, nos comunicamos de su Punto de Venta para informarle que...');

  useEffect(() => {
    if (isOpen) {
      const load = async () => { setCustomers(await getCustomers()); };
      load();
    }
  }, [isOpen]);

  const handleSend = () => {
    const customer = customers.find(c => c.id === selectedCustomerId);
    if (!customer) {
        // Send without selecting if no customer chosen
        window.open(`https://wa.me/?text=${encodeURIComponent(message)}`, '_blank');
        return;
    }
    
    if (!customer.phone) {
        toast.error('El cliente seleccionado no tiene un número de teléfono registrado.');
        return;
    }

    // Clean phone number (remove spaces, etc.)
    const phone = customer.phone.replace(/[\s\-\(\)]/g, '');
    window.open(`https://wa.me/${phone}?text=${encodeURIComponent(message)}`, '_blank');
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
            <ChatCircle size={28} weight="regular" className="text-success" /> Enviar Mensaje
          </h2>
          <button onClick={onClose} className="icon-btn"><X size={24} /></button>
        </div>

        <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <div className="form-group">
            <label style={{ color: 'var(--text-secondary)', marginBottom: '8px', display: 'flex', alignItems: 'center', gap: '4px' }}>
              <UsersThree size={16} weight="regular" /> Seleccionar Cliente (Opcional)
            </label>
            <select 
              value={selectedCustomerId} 
              onChange={e => setSelectedCustomerId(e.target.value)} 
              className="search-input" 
              style={{ backgroundColor: 'var(--bg-primary)' }} 
            >
              <option value="">-- Sin destinatario específico --</option>
              {customers.map(c => (
                <option key={c.id} value={c.id}>{c.name} {c.phone ? `(${c.phone})` : ''}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label style={{ color: 'var(--text-secondary)', marginBottom: '8px' }}>Texto del Mensaje</label>
            <textarea 
              rows={4}
              value={message} 
              onChange={e => setMessage(e.target.value)} 
              className="search-input" 
              style={{ backgroundColor: 'var(--bg-primary)', resize: 'vertical' }} 
            />
            <div style={{ display: 'flex', gap: '8px', marginTop: '8px', flexWrap: 'wrap' }}>
                <button className="icon-btn" style={{ fontSize: '11px', padding: '4px 8px', borderRadius: '4px', border: '1px solid var(--border-color)' }} onClick={() => setMessage('Hola, te recordamos que tienes un saldo pendiente de pago. ¡Saludos!')}>
                    Plantilla: Cobro
                </button>
                <button className="icon-btn" style={{ fontSize: '11px', padding: '4px 8px', borderRadius: '4px', border: '1px solid var(--border-color)' }} onClick={() => setMessage('Hola, tu pedido ya está listo para pasar a recoger. ¡Te esperamos!')}>
                    Plantilla: Pedido Listo
                </button>
                <button className="icon-btn" style={{ fontSize: '11px', padding: '4px 8px', borderRadius: '4px', border: '1px solid var(--border-color)' }} onClick={() => setMessage('Hola, ¡tenemos nuevas promociones este mes en nuestra tienda!')}>
                    Plantilla: Promoción
                </button>
            </div>
          </div>

          <button onClick={handleSend} className="checkout-btn" style={{ marginTop: '16px', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px', backgroundColor: 'var(--accent-success)' }}>
            <PaperPlaneTilt size={20} weight="regular" /> ABRIR EN WHATSAPP
          </button>
        </div>
      </div>
    </div>
  );
};
