'use client';

import React, { useEffect, useState } from 'react';
import { TicketItem } from '@/types';
import { ShoppingCart } from 'phosphor-react';

export default function ClientView() {
  const [cart, setCart] = useState<TicketItem[]>([]);
  const [reconnectTrigger, setReconnectTrigger] = useState(0);

  useEffect(() => {
    // Determine the IP. If running on same machine, localhost. Else use current host.
    const host = window.location.hostname;
    // Connect to Axum WebSocket on port 8080
    const socket = new WebSocket(`ws://${host}:8080/ws`);
    
    socket.onmessage = (event) => {
        try {
            const data = JSON.parse(event.data);
            if (Array.isArray(data)) {
                setCart(data);
            }
        } catch (e) {
            console.error("Invalid WS message", e);
        }
    };

    socket.onclose = () => {
        console.log("WS closed. Reconnecting...");
        setTimeout(() => setReconnectTrigger(prev => prev + 1), 5000); // trigger reconnect
    };

    return () => socket.close();
  }, [reconnectTrigger]); // Reconnect logic if reconnectTrigger changes

  const total = cart.reduce((acc, item) => acc + (item.price * item.quantity), 0);

  return (
    <div style={{ display: 'flex', height: '100vh', backgroundColor: 'var(--bg-primary)', color: '#fff', fontFamily: 'sans-serif' }}>
        <div style={{ flex: 1, padding: '40px', display: 'flex', flexDirection: 'column' }}>
            <h1 style={{ fontSize: '32px', fontWeight: 'bold', display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '32px' }}>
                <ShoppingCart size={40} color="var(--accent-primary)" />
                Tu Compra
            </h1>

            <div style={{ flex: 1, overflowY: 'auto' }}>
                {cart.length === 0 ? (
                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%', color: 'var(--text-muted)' }}>
                        <ShoppingCart size={120} />
                        <h2 style={{ marginTop: '24px', fontSize: '24px' }}>Esperando artículos...</h2>
                    </div>
                ) : (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
                        {cart.map(item => (
                            <div key={item.id} style={{ display: 'flex', justifyContent: 'space-between', padding: '24px', backgroundColor: 'var(--bg-secondary)', borderRadius: 'var(--radius-lg)', boxShadow: 'var(--shadow-md)' }}>
                                <div>
                                    <h3 style={{ fontSize: '24px', fontWeight: 'bold', margin: 0 }}>{item.name}</h3>
                                    <div style={{ color: 'var(--text-secondary)', fontSize: '18px', marginTop: '8px' }}>
                                        {item.quantity} {item.unit} x ${item.price.toFixed(2)}
                                    </div>
                                </div>
                                <div style={{ fontSize: '32px', fontWeight: 'bold', color: 'var(--accent-primary)' }}>
                                    ${(item.price * item.quantity).toFixed(2)}
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>

            <div style={{ marginTop: '32px', padding: '32px', backgroundColor: 'var(--bg-tertiary)', borderRadius: 'var(--radius-lg)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ fontSize: '24px', color: 'var(--text-secondary)' }}>Total a pagar</span>
                <span style={{ fontSize: '64px', fontWeight: 'bold', color: 'var(--accent-success)' }}>${total.toFixed(2)}</span>
            </div>
        </div>

        <div style={{ width: '400px', backgroundColor: 'var(--bg-secondary)', borderLeft: '1px solid var(--border-color)', display: 'flex', flexDirection: 'column', padding: '40px' }}>
             <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', textAlign: 'center' }}>
                 <h2 style={{ fontSize: '28px', fontWeight: 'bold', marginBottom: '16px', color: 'var(--accent-primary)' }}>NextVent POS</h2>
                 <p style={{ color: 'var(--text-secondary)', fontSize: '18px', lineHeight: '1.5' }}>Gracias por tu preferencia.</p>
             </div>
        </div>
    </div>
  );
}
