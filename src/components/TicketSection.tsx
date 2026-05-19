'use client';

import React, { useState, useEffect } from 'react';
import { ShoppingCart, Minus, Plus, Trash2, Wallet, Pause, Percent, Tag, CheckCircle2 } from 'lucide-react';
import { TicketItem } from '@/types';

type TicketSectionProps = {
  ticket: (TicketItem & { priceToUse?: number })[];
  ticketTotal: number;
  ticketCount: number;
  updateQuantity: (id: string, delta: number) => void;
  onCompleteSale: () => void;
  onClearTicket: () => void;
  parkedOrders: { id: string, timestamp: string, items: any[] }[];
  onParkSale: () => void;
  onResumeOrder: (orderId: string) => void;
  globalDiscount: number;
  onOpenDiscount: () => void;
  lastAddedId: string | null;
  alerts?: { type: 'info' | 'success', message: string }[];
};


export const TicketSection = ({
  ticket,
  ticketTotal,
  ticketCount,
  updateQuantity,
  onCompleteSale,
  onClearTicket,
  parkedOrders,
  onParkSale,
  onResumeOrder,
  globalDiscount,
  onOpenDiscount,
  lastAddedId,
  alerts = []
}: TicketSectionProps) => {
  const [ticketId, setTicketId] = useState<string>('----');

  useEffect(() => {
    setTicketId(`${Math.floor(1000 + Math.random() * 9000)}`);
  }, []);

  return (
    <aside className="ticket-section">
      <div className="ticket-header">
        <div>
            <div className="ticket-title">Ticket de Venta</div>
            <div className="ticket-id">#TKT-{ticketId}</div>
        </div>
        {parkedOrders.length > 0 && (
            <div style={{ backgroundColor: 'var(--accent-primary)', color: '#fff', padding: '4px 8px', borderRadius: '12px', fontSize: '12px', fontWeight: 'bold' }}>
                {parkedOrders.length} Pendientes
            </div>
        )}
      </div>

      <div className="ticket-items">
        {ticket.length === 0 && parkedOrders.length > 0 && (
            <div className="parked-orders-list" style={{ padding: '16px' }}>
                <p style={{ fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '12px' }}>Ventas en espera:</p>
                {parkedOrders.map(order => (
                    <div 
                        key={order.id} 
                        onClick={() => onResumeOrder(order.id)}
                        style={{ 
                            backgroundColor: 'var(--bg-tertiary)', padding: '12px', borderRadius: 'var(--radius-md)', marginBottom: '8px', 
                            cursor: 'pointer', border: '1px solid var(--border-color)', display: 'flex', justifyContent: 'space-between', alignItems: 'center'
                        }}
                    >
                        <div>
                            <div style={{ fontWeight: 'bold', fontSize: '14px' }}>Pedido {order.timestamp}</div>
                            <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{order.items.length} productos</div>
                        </div>
                        <Plus size={16} color="var(--accent-primary)" />
                    </div>
                ))}
            </div>
        )}

        {ticket.length === 0 && parkedOrders.length === 0 ? (
          <div className="empty-ticket">
            <ShoppingCart size={48} className="empty-icon text-muted" />
            <p>Escanea o selecciona un producto para comenzar a cobrar.</p>
          </div>
        ) : (
          ticket.map(item => (
            <div key={item.id} className={`ticket-item ${lastAddedId === item.id ? 'item-flash' : ''}`}>
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
                  ${((item.priceToUse || item.price) * item.quantity).toFixed(2)}
                </div>
                <div className="ticket-item-unit">
                  {item.priceToUse && item.priceToUse < item.price && (
                    <span style={{ textDecoration: 'line-through', fontSize: '10px', opacity: 0.5, marginRight: 4 }}>
                        ${item.price.toFixed(2)}
                    </span>
                  )}
                  ${(item.priceToUse || item.price).toFixed(2)} x {item.unit}
                </div>
              </div>

            </div>
          ))
        )}
      </div>

      <div className="ticket-summary">
        {/* Alertas de Promociones */}
        {alerts.length > 0 && (
          <div style={{ marginBottom: '16px', display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {Array.from(new Set(alerts.map(a => a.message))).map(msg => {
                  const alert = alerts.find(a => a.message === msg);
                  return (
                      <div key={msg} style={{ 
                          padding: '10px 14px', borderRadius: 'var(--radius-md)', fontSize: '13px', fontWeight: 'bold',
                          display: 'flex', alignItems: 'center', gap: '8px',
                          backgroundColor: alert?.type === 'success' ? 'rgba(34, 197, 94, 0.15)' : 'rgba(234, 179, 8, 0.15)',
                          color: alert?.type === 'success' ? '#22c55e' : '#eab308',
                          border: `1px solid ${alert?.type === 'success' ? 'rgba(34, 197, 94, 0.2)' : 'rgba(234, 179, 8, 0.2)'}`,
                          animation: 'slideIn 0.3s ease-out'
                      }}>
                          {alert?.type === 'success' ? <CheckCircle2 size={16} /> : <Tag size={16} />}
                          {msg}
                      </div>
                  );
              })}
          </div>
        )}

        <div className="summary-row">
          <span>Subtotal ({ticketCount} art.)</span>
          <span>${(ticketTotal + globalDiscount).toFixed(2)}</span>
        </div>
        <div className="summary-row" style={{ color: 'var(--accent-danger)' }}>
          <span>Descuentos Mayoreo</span>
          <span>-${ticket.reduce((acc, item) => acc + (item.price - (item.priceToUse || item.price)) * item.quantity, 0).toFixed(2)}</span>
        </div>
        {globalDiscount > 0 && (
            <div className="summary-row" style={{ color: 'var(--accent-warning)', borderTop: '1px dashed var(--border-color)', paddingTop: '8px', marginTop: '8px' }}>
                <span>Descuento Global</span>
                <span>-${globalDiscount.toFixed(2)}</span>
            </div>
        )}

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
            className="icon-btn"
            onClick={onParkSale}
            disabled={ticket.length === 0}
            title="Poner en Espera (Park)"
            style={{ 
              opacity: ticket.length === 0 ? 0.5 : 1, 
              cursor: ticket.length === 0 ? 'not-allowed' : 'pointer',
              color: 'var(--accent-warning)',
              backgroundColor: 'var(--bg-tertiary)'
            }}
          >
            <Pause size={20} />
          </button>

          <button
            className="icon-btn"
            onClick={onOpenDiscount}
            disabled={ticket.length === 0}
            title="Aplicar Descuento Global"
            style={{ 
              opacity: ticket.length === 0 ? 0.5 : 1, 
              cursor: ticket.length === 0 ? 'not-allowed' : 'pointer',
              color: 'var(--accent-primary)',
              backgroundColor: 'var(--bg-tertiary)'
            }}
          >
            <Percent size={20} />
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
