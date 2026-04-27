'use client';

import React from 'react';
import { ShoppingCart, Minus, Plus, Trash2, Wallet } from 'lucide-react';
import { TicketItem } from '@/types';

type TicketSectionProps = {
  ticket: TicketItem[];
  ticketTotal: number;
  ticketCount: number;
  updateQuantity: (id: string, delta: number) => void;
  onCompleteSale: () => void;
  onClearTicket: () => void;
};

export const TicketSection = ({
  ticket,
  ticketTotal,
  ticketCount,
  updateQuantity,
  onCompleteSale,
  onClearTicket
}: TicketSectionProps) => {
  return (
    <aside className="ticket-section">
      <div className="ticket-header">
        <div className="ticket-title">Ticket de Venta</div>
        <div className="ticket-id">#TKT-{Math.floor(1000 + Math.random() * 9000)}</div>
      </div>

      <div className="ticket-items">
        {ticket.length === 0 ? (
          <div className="empty-ticket">
            <ShoppingCart size={48} className="empty-icon text-muted" />
            <p>Escanea o selecciona un producto para comenzar a cobrar.</p>
          </div>
        ) : (
          ticket.map(item => (
            <div key={item.id} className="ticket-item">
              <div className="ticket-item-info">
                <div className="ticket-item-name">{item.name}</div>
                <div className="ticket-item-qty-controls">
                  <button className="qty-btn" onClick={() => updateQuantity(item.id, -1)}>
                    <Minus size={14} />
                  </button>
                  <span className="qty-value">{item.quantity}</span>
                  <button className="qty-btn" onClick={() => updateQuantity(item.id, 1)}>
                    <Plus size={14} />
                  </button>
                </div>
                <button className="ticket-item-remove" onClick={() => updateQuantity(item.id, -item.quantity)}>
                  <Trash2 size={12} style={{ display: 'inline', marginRight: 4 }} />
                  ELIMINAR
                </button>
              </div>

              <div className="ticket-item-price-container">
                <div className="ticket-item-total">
                  ${(item.price * item.quantity).toFixed(2)}
                </div>
                <div className="ticket-item-unit">
                  ${item.price.toFixed(2)} x {item.unit}
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      <div className="ticket-summary">
        <div className="summary-row">
          <span>Subtotal ({ticketCount} art.)</span>
          <span>${ticketTotal.toFixed(2)}</span>
        </div>
        <div className="summary-row">
          <span>IVA (16%)</span>
          <span>$0.00</span>
        </div>
        <div className="summary-row" style={{ color: 'var(--accent-danger)' }}>
          <span>Descuentos</span>
          <span>-$0.00</span>
        </div>

        <div className="summary-total">
          <span>TOTAL</span>
          <span className="text-accent">${ticketTotal.toFixed(2)}</span>
        </div>

        <div style={{ display: 'flex', gap: '8px', width: '100%' }}>
          <button
            className="icon-btn"
            onClick={onClearTicket}
            disabled={ticket.length === 0}
            title="Cancelar Venta"
            style={{ 
              opacity: ticket.length === 0 ? 0.5 : 1, 
              cursor: ticket.length === 0 ? 'not-allowed' : 'pointer',
              color: 'var(--accent-danger)',
              backgroundColor: 'var(--bg-tertiary)'
            }}
          >
            <Trash2 size={20} />
          </button>
          <button
            className="checkout-btn"
            onClick={onCompleteSale}
            disabled={ticket.length === 0}
            style={{ flex: 1, margin: 0, opacity: ticket.length === 0 ? 0.5 : 1, cursor: ticket.length === 0 ? 'not-allowed' : 'pointer' }}
          >
            <Wallet size={20} />
            COBRAR AHORA
          </button>
        </div>
      </div>
    </aside>
  );
};
