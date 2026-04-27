'use client';

import React, { useState, useEffect } from 'react';
import { Sidebar } from '@/components/Sidebar';
import { getCustomers, addCustomer, registerPayment, deleteCustomer } from '@/lib/storage';
import { Customer } from '@/types';
import { UserPlus, Search, Phone, Receipt, User, MinusCircle, History, Trash2, CalendarCheck } from 'lucide-react';

export default function Customers() {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [isAdding, setIsAdding] = useState(false);
  const [newName, setNewName] = useState('');
  const [newPhone, setNewPhone] = useState('');

  useEffect(() => {
    const load = async () => { setCustomers(await getCustomers()); };
    load();
  }, []);

  const filtered = customers.filter(c => c.name.toLowerCase().includes(searchQuery.toLowerCase()));

  const handleAdd = async () => {
    if (!newName) return;
    const nc: Customer = { id: Date.now().toString(), name: newName, phone: newPhone, debt: 0, payments: [] };
    await addCustomer(nc);
    setCustomers(await getCustomers());
    setNewName(''); setNewPhone(''); setIsAdding(false);
  };

  const handlePayment = async (id: string) => {
    const amount = prompt("¿Cuánto desea abonar el cliente?");
    if (amount && !isNaN(parseFloat(amount))) {
      await registerPayment(id, parseFloat(amount));
      setCustomers(await getCustomers());
    }
  };

  const handleDelete = async (id: string) => {
    if (confirm("¿Eliminar cliente?")) {
        await deleteCustomer(id);
        setCustomers(await getCustomers());
    }
  };

  return (
    <>
      <Sidebar activeModule="customers" />
      <main className="main-content">
        <header className="top-bar">
          <div className="search-container" style={{ width: '100%', maxWidth: '600px' }}>
            <Search size={18} color="var(--text-muted)" />
            <input 
              type="text" className="search-input" placeholder="Buscar cliente por nombre..." 
              value={searchQuery} onChange={e => setSearchQuery(e.target.value)}
            />
          </div>
          <button className="checkout-btn" style={{ margin: 0, width: 'auto', padding: '10px 20px' }} onClick={() => setIsAdding(true)}>
            <UserPlus size={20} /> NUEVO CLIENTE
          </button>
        </header>

        <div style={{ padding: '24px', overflowY: 'auto', flex: 1 }}>
          {isAdding && (
            <div style={{ backgroundColor: 'var(--bg-secondary)', padding: '24px', borderRadius: 'var(--radius-lg)', marginBottom: '24px', border: '1px solid var(--accent-primary)' }}>
              <h3 style={{ marginBottom: '16px' }}>Agregar Cliente</h3>
              <div style={{ display: 'flex', gap: '16px' }}>
                <input className="search-input" style={{ flex: 2 }} placeholder="Nombre completo" value={newName} onChange={e => setNewName(e.target.value)} />
                <input className="search-input" style={{ flex: 1 }} placeholder="Teléfono" value={newPhone} onChange={e => setNewPhone(e.target.value)} />
                <button className="checkout-btn" style={{ width: 'auto', margin: 0 }} onClick={handleAdd}>GUARDAR</button>
                <button className="icon-btn" onClick={() => setIsAdding(false)}>X</button>
              </div>
            </div>
          )}

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: '24px' }}>
            {filtered.map(c => (
              <div key={c.id} style={{ backgroundColor: 'var(--bg-secondary)', borderRadius: 'var(--radius-lg)', padding: '24px', border: '1px solid var(--border-color)', display: 'flex', flexDirection: 'column', gap: '16px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                    <div style={{ padding: '10px', backgroundColor: 'var(--bg-tertiary)', borderRadius: '50%', color: 'var(--accent-primary)' }}><User size={20} /></div>
                    <div>
                      <div style={{ fontWeight: 'bold', fontSize: '18px' }}>{c.name}</div>
                      <div style={{ fontSize: '12px', color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: '4px' }}>
                        <Phone size={10} /> {c.phone || 'Sin teléfono'}
                      </div>
                    </div>
                  </div>
                  <button className="icon-btn" style={{ color: 'var(--accent-danger)' }} onClick={() => handleDelete(c.id)}><Trash2 size={16} /></button>
                </div>

                <div style={{ backgroundColor: 'var(--bg-primary)', padding: '16px', borderRadius: 'var(--radius-md)', textAlign: 'center' }}>
                    <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginBottom: '4px' }}>DEUDA ACTUAL</div>
                    <div style={{ fontSize: '24px', fontWeight: 'bold', color: c.debt > 0 ? 'var(--accent-danger)' : 'var(--accent-success)' }}>
                        ${c.debt.toFixed(2)}
                    </div>
                </div>

                <div style={{ display: 'flex', gap: '8px' }}>
                    <button className="checkout-btn" style={{ flex: 1, margin: 0, padding: '12px', fontSize: '14px', backgroundColor: 'var(--accent-success)' }} onClick={() => handlePayment(c.id)}>
                      <MinusCircle size={16} /> ABONAR
                    </button>
                </div>

                {c.payments && c.payments.length > 0 && (
                   <div style={{ marginTop: '8px', borderTop: '1px solid var(--border-color)', paddingTop: '12px' }}>
                      <div style={{ fontSize: '11px', color: 'var(--text-muted)', fontWeight: 'bold', marginBottom: '8px', display: 'flex', alignItems: 'center', gap: '4px' }}>
                        <CalendarCheck size={12} /> ÚLTIMOS ABONOS
                      </div>
                      <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                        {c.payments.slice(-3).reverse().map(p => (
                          <div key={p.id} style={{ display: 'flex', justifyContent: 'space-between', fontSize: '12px' }}>
                             <span style={{ color: 'var(--text-secondary)' }}>{new Date(p.date).toLocaleDateString()}</span>
                             <span style={{ color: 'var(--accent-success)', fontWeight: 'bold' }}>-${p.amount.toFixed(2)}</span>
                          </div>
                        ))}
                      </div>
                   </div>
                )}
              </div>
            ))}
          </div>

          {filtered.length === 0 && !isAdding && (
             <div style={{ textAlign: 'center', padding: '100px', color: 'var(--text-muted)' }}>
                <User size={48} style={{ opacity: 0.2, marginBottom: '16px' }} />
                <p>No se encontraron clientes.</p>
             </div>
          )}
        </div>
      </main>
    </>
  );
}
