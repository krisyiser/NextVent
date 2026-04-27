'use client';

import React, { useState, useEffect } from 'react';
import { X, Banknote, CreditCard, User, UserPlus, CheckCircle2 } from 'lucide-react';
import { Customer, TicketItem, Sale } from '@/types';
import { getCustomers, addCustomer } from '@/lib/storage';

type CheckoutModalProps = {
  isOpen: boolean;
  onClose: () => void;
  total: number;
  totalCost: number;
  items: TicketItem[];
  onComplete: (sale: Sale) => void;
};

export const CheckoutModal = ({ isOpen, onClose, total, totalCost, items, onComplete }: CheckoutModalProps) => {
  const [paidAmount, setPaidAmount] = useState<string>('');
  const [isCredit, setIsCredit] = useState(false);
  const [selectedCustomerId, setSelectedCustomerId] = useState<string>('');
  const [customers, setCustomers] = useState<Customer[]>([]);

  useEffect(() => {
    if (isOpen) {
      const load = async () => { setCustomers(await getCustomers()); };
      load();
      setPaidAmount('');
      setIsCredit(false);
      setSelectedCustomerId('');
    }
  }, [isOpen]);

  const handleQuickAddCustomer = async () => {
    const name = window.prompt("Nombre del nuevo cliente:");
    if (!name) return;
    const phone = window.prompt("Número de teléfono (Opcional):") || '';
    const newCustomer: Customer = { id: `CUS-${Date.now()}`, name, phone, debt: 0, payments: [] };
    await addCustomer(newCustomer);
    setCustomers(await getCustomers());
    setSelectedCustomerId(newCustomer.id);
  };

  if (!isOpen) return null;

  const change = Math.max(0, parseFloat(paidAmount || '0') - total);
  const isPaidEnough = parseFloat(paidAmount || '0') >= total || isCredit;

  const handleFinalize = () => {
    if (!isPaidEnough) return;
    if (isCredit && !selectedCustomerId) {
      alert("Selecciona un cliente para el fiado");
      return;
    }

    // Generate accurate snapshot of all products in the sale
    const snapshotItems = items.map(item => {
      let priceToUse = item.price;
      let isWholesale = false;
      if (item.wholesalePrice && item.wholesaleThreshold && item.quantity >= item.wholesaleThreshold) {
        priceToUse = item.wholesalePrice;
        isWholesale = true;
      }

      return {
        productId: item.id,
        name: item.name,
        cost: item.cost,
        price: priceToUse,
        quantity: item.quantity,
        unit: item.unit,
        total: priceToUse * item.quantity,
        isWholesale
      };
    });

    const totalCost = snapshotItems.reduce((acc, item) => acc + (item.cost * item.quantity), 0);

    const sale: Sale = {
      id: `SALE-${Date.now()}`,
      date: new Date().toISOString(),
      items: snapshotItems,
      total,
      totalCost,
      profit: total - totalCost,
      paidAmount: isCredit ? 0 : parseFloat(paidAmount || '0'),
      changeAmount: isCredit ? 0 : change,
      customerId: isCredit ? selectedCustomerId : undefined,
      isCredit,
      isCancelled: false
    };

    onComplete(sale);
  };

  return (
    <div className="modal-overlay" style={{
      position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
      backgroundColor: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000,
      backdropFilter: 'blur(4px)'
    }}>
      <div className="modal-content" style={{
        backgroundColor: 'var(--bg-secondary)', padding: '32px', borderRadius: 'var(--radius-lg)',
        width: '100%', maxWidth: '500px', border: '1px solid var(--border-color)', boxShadow: 'var(--shadow-lg)'
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
          <h2 style={{ fontSize: '24px', fontWeight: 'bold' }}>Finalizar Venta</h2>
          <button onClick={onClose} className="icon-btn"><X size={24} /></button>
        </div>

        <div style={{ textAlign: 'center', marginBottom: '32px', padding: '20px', backgroundColor: 'var(--bg-primary)', borderRadius: 'var(--radius-md)' }}>
          <div style={{ color: 'var(--text-secondary)', fontSize: '14px', marginBottom: '8px' }}>TOTAL A COBRAR</div>
          <div style={{ fontSize: '48px', fontWeight: 'bold', color: 'var(--accent-success)' }}>${total.toFixed(2)}</div>
        </div>

        <div style={{ display: 'flex', gap: '12px', marginBottom: '24px' }}>
          <button 
            onClick={() => setIsCredit(false)}
            style={{
              flex: 1, padding: '16px', borderRadius: 'var(--radius-md)', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '8px',
              backgroundColor: !isCredit ? 'var(--accent-primary)' : 'var(--bg-tertiary)', border: 'none', color: '#fff', cursor: 'pointer', transition: 'var(--transition)'
            }}
          >
            <Banknote size={24} />
            Efectivo
          </button>
          <button 
            onClick={() => setIsCredit(true)}
            style={{
              flex: 1, padding: '16px', borderRadius: 'var(--radius-md)', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '8px',
              backgroundColor: isCredit ? 'var(--accent-danger)' : 'var(--bg-tertiary)', border: 'none', color: '#fff', cursor: 'pointer', transition: 'var(--transition)'
            }}
          >
            <CreditCard size={24} />
            Fiado (Crédito)
          </button>
        </div>

        {!isCredit ? (
          <div style={{ marginBottom: '24px' }}>
            <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>Paga con:</label>
            <div style={{ display: 'flex', alignItems: 'center', backgroundColor: 'var(--bg-tertiary)', borderRadius: 'var(--radius-md)', padding: '0 16px' }}>
              <span style={{ fontSize: '24px', color: 'var(--text-secondary)' }}>$</span>
              <input 
                autoFocus
                type="number"
                value={paidAmount}
                onChange={e => setPaidAmount(e.target.value)}
                style={{
                  width: '100%', padding: '16px', backgroundColor: 'transparent', border: 'none', color: '#fff', fontSize: '24px', outline: 'none'
                }}
                placeholder="0.00"
              />
            </div>
            {parseFloat(paidAmount || '0') > 0 && (
              <div style={{ marginTop: '16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ color: 'var(--text-secondary)' }}>Su cambio:</span>
                <span style={{ fontSize: '24px', fontWeight: 'bold', color: change > 0 ? 'var(--accent-warning)' : 'var(--text-muted)' }}>
                  ${change.toFixed(2)}
                </span>
              </div>
            )}
          </div>
        ) : (
          <div style={{ marginBottom: '24px' }}>
            <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>Cliente:</label>
            <select 
              value={selectedCustomerId}
              onChange={e => setSelectedCustomerId(e.target.value)}
              style={{
                width: '100%', padding: '16px', backgroundColor: 'var(--bg-tertiary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)', color: '#fff', outline: 'none'
              }}
            >
              <option value="">Seleccionar cliente...</option>
              {customers.map(c => (
                <option key={c.id} value={c.id}>{c.name} (Saldo: ${c.debt.toFixed(2)})</option>
              ))}
            </select>
            <button type="button" onClick={handleQuickAddCustomer} style={{ marginTop: '12px', display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--accent-primary)', background: 'none', border: 'none', cursor: 'pointer', fontSize: '14px' }}>
              <UserPlus size={16} /> Agregar nuevo cliente
            </button>
          </div>
        )}

        <button 
          onClick={handleFinalize}
          disabled={!isPaidEnough}
          style={{
            width: '100%', padding: '20px', borderRadius: 'var(--radius-md)', border: 'none',
            backgroundColor: isPaidEnough ? 'var(--accent-success)' : 'var(--bg-tertiary)',
            color: '#fff', fontSize: '18px', fontWeight: 'bold', cursor: isPaidEnough ? 'pointer' : 'not-allowed',
            display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '12px', transition: 'var(--transition)'
          }}
        >
          <CheckCircle2 size={24} />
          {isCredit ? 'REGISTRAR DEUDA' : 'FINALIZAR Y COBRAR'}
        </button>
      </div>
    </div>
  );
};
