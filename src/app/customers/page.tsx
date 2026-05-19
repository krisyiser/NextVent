'use client';

import React, { useState, useEffect, useMemo } from 'react';
import { Sidebar } from '@/components/Sidebar';
import { Search, Plus, User, Phone, CreditCard, History, Trash2, CheckCircle2, X } from 'lucide-react';
import { Customer } from '@/types';
import { getCustomers, addCustomer, deleteCustomer, registerPayment } from '@/lib/storage';

export default function CustomersPage() {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedCustomer, setSelectedCustomer] = useState<Customer | null>(null);
  const [paymentAmount, setPaymentAmount] = useState('');
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);
  
  // New Customer Form
  const [newName, setNewName] = useState('');
  const [newPhone, setNewPhone] = useState('');

  useEffect(() => {
    loadCustomers();
  }, []);

  const loadCustomers = async () => {
    setCustomers(await getCustomers());
  };

  const filteredCustomers = useMemo(() => {
    return customers.filter(c => 
      c.name.toLowerCase().includes(searchQuery.toLowerCase()) || 
      (c.phone && c.phone.includes(searchQuery))
    );
  }, [customers, searchQuery]);

  const handleAddCustomer = async (e: React.FormEvent) => {
    e.preventDefault();
    const newCustomer: Customer = {
      id: `CUST-${Date.now()}`,
      name: newName,
      phone: newPhone,
      debt: 0,
      payments: []
    };
    await addCustomer(newCustomer);
    setNewName('');
    setNewPhone('');
    setIsModalOpen(false);
    loadCustomers();
  };

  const handleDelete = async (id: string) => {
    await deleteCustomer(id);
    setDeleteConfirmId(null);
    setSelectedCustomer(null);
    loadCustomers();
  };

  const handlePayment = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedCustomer || !paymentAmount) return;
    
    const amount = parseFloat(paymentAmount);
    if (isNaN(amount) || amount <= 0) return;

    await registerPayment(selectedCustomer.id, amount);
    setPaymentAmount('');
    setSelectedCustomer(null);
    loadCustomers();
    alert('Abono registrado correctamente.');
  };

  return (
    <>
      <Sidebar activeModule="customers" />

      <main className="main-content">
        <header className="top-bar">
          <h1 className="page-title">Directorio de Clientes</h1>
          
          <div className="top-bar-actions">
             <button className="checkout-btn" style={{ margin: 0 }} onClick={() => setIsModalOpen(true)}>
               <Plus size={20} /> NUEVO CLIENTE
             </button>
          </div>
        </header>

        <div className="pos-layout" style={{ display: 'grid', gridTemplateColumns: '1fr 350px', gap: '24px' }}>
          
          {/* Main List */}
          <section className="products-section" style={{ height: 'calc(100vh - 120px)' }}>
             <div className="search-container" style={{ marginBottom: '20px', width: '100%' }}>
                <Search size={18} color="var(--text-muted)" />
                <input
                  type="text"
                  className="search-input"
                  placeholder="Buscar cliente por nombre o teléfono..."
                  value={searchQuery}
                  onChange={e => setSearchQuery(e.target.value)}
                />
             </div>

             <div className="customers-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: '16px' }}>
                {filteredCustomers.map(customer => (
                  <div 
                    key={customer.id} 
                    className={`product-card ${selectedCustomer?.id === customer.id ? 'active' : ''}`}
                    onClick={() => setSelectedCustomer(customer)}
                    style={{ padding: '20px', height: 'auto', cursor: 'pointer', border: selectedCustomer?.id === customer.id ? '2px solid var(--accent-primary)' : '1px solid var(--border-color)' }}
                  >
                    <div style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
                       <div style={{ width: '48px', height: '48px', borderRadius: '50%', backgroundColor: 'rgba(59, 130, 246, 0.1)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                          <User size={24} className="text-accent-primary" />
                       </div>
                       <div style={{ flex: 1 }}>
                          <h3 style={{ fontSize: '18px', fontWeight: 'bold' }}>{customer.name}</h3>
                          <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '13px', color: 'var(--text-muted)' }}>
                             <Phone size={12} /> {customer.phone || 'Sin teléfono'}
                          </div>
                       </div>
                    </div>

                    <div style={{ marginTop: '20px', padding: '12px', backgroundColor: 'var(--bg-tertiary)', borderRadius: 'var(--radius-md)' }}>
                       <div style={{ fontSize: '12px', color: 'var(--text-muted)', marginBottom: '4px' }}>Deuda Actual</div>
                       <div style={{ fontSize: '20px', fontWeight: 'bold', color: customer.debt > 0 ? 'var(--accent-danger)' : 'var(--accent-success)' }}>
                          ${customer.debt.toFixed(2)}
                       </div>
                    </div>
                  </div>
                ))}
             </div>
          </section>

          {/* Details Panel */}
          <aside className="ticket-section" style={{ height: 'calc(100vh - 120px)' }}>
            {selectedCustomer ? (
              <div style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                <div className="ticket-header">
                  <div className="ticket-title">Detalles del Cliente</div>
                </div>

                <div className="ticket-items" style={{ flex: 1, overflowY: 'auto', padding: '0 4px' }}>
                   <div style={{ textAlign: 'center', padding: '24px 0' }}>
                      <User size={64} className="text-accent-primary" style={{ margin: '0 auto 16px' }} />
                      <h2 style={{ fontSize: '22px', fontWeight: 'bold' }}>{selectedCustomer.name}</h2>
                      <p style={{ color: 'var(--text-muted)' }}>{selectedCustomer.phone}</p>
                   </div>

                   <div style={{ marginBottom: '24px' }}>
                      <h4 style={{ fontSize: '14px', fontWeight: '600', marginBottom: '12px', display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <History size={16} /> ÚLTIMOS ABONOS
                      </h4>
                      {selectedCustomer.payments && selectedCustomer.payments.length > 0 ? (
                        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                           {selectedCustomer.payments.slice(-5).reverse().map(p => (
                             <div key={p.id} style={{ display: 'flex', justifyContent: 'space-between', padding: '10px', backgroundColor: 'var(--bg-tertiary)', borderRadius: 'var(--radius-sm)', fontSize: '13px' }}>
                                <span>{new Date(p.date).toLocaleDateString()}</span>
                                <span className="text-accent-success" style={{ fontWeight: '600' }}>+${p.amount.toFixed(2)}</span>
                             </div>
                           ))}
                        </div>
                      ) : (
                        <p style={{ fontSize: '12px', color: 'var(--text-muted)', textAlign: 'center' }}>Sin historial de abonos.</p>
                      )}
                   </div>

                   {selectedCustomer.debt > 0 && (
                     <form onSubmit={handlePayment} style={{ marginBottom: '24px', padding: '16px', backgroundColor: 'rgba(59, 130, 246, 0.05)', borderRadius: 'var(--radius-md)', border: '1px solid var(--accent-primary)' }}>
                        <h4 style={{ fontSize: '14px', fontWeight: '600', marginBottom: '12px' }}>REGISTRAR ABONO</h4>
                        <div style={{ display: 'flex', gap: '8px' }}>
                           <input 
                             type="number" step="0.01"
                             placeholder="Monto $"
                             className="search-input"
                             style={{ backgroundColor: 'var(--bg-secondary)', height: '40px' }}
                             value={paymentAmount}
                             onChange={e => setPaymentAmount(e.target.value)}
                           />
                           <button type="submit" className="checkout-btn" style={{ margin: 0, height: '40px', padding: '0 16px' }}>ABONAR</button>
                        </div>
                     </form>
                   )}
                </div>

                <div className="ticket-summary">
                   {deleteConfirmId === selectedCustomer.id ? (
                     <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                        <p style={{ fontSize: '12px', color: 'var(--accent-danger)', textAlign: 'center', fontWeight: 'bold' }}>¿Confirmar eliminación total?</p>
                        <div style={{ display: 'flex', gap: '8px' }}>
                            <button 
                                onClick={() => setDeleteConfirmId(null)}
                                style={{ flex: 1, padding: '10px', backgroundColor: 'var(--bg-tertiary)', color: '#fff', border: 'none', borderRadius: 'var(--radius-md)', cursor: 'pointer' }}
                            >Cancelar</button>
                            <button 
                                onClick={() => handleDelete(selectedCustomer.id)}
                                style={{ flex: 1, padding: '10px', backgroundColor: 'var(--accent-danger)', color: '#fff', border: 'none', borderRadius: 'var(--radius-md)', fontWeight: 'bold', cursor: 'pointer' }}
                            >SÍ, ELIMINAR</button>
                        </div>
                     </div>
                   ) : (
                    <button 
                      onClick={() => setDeleteConfirmId(selectedCustomer.id)}
                      style={{ width: '100%', padding: '12px', backgroundColor: 'rgba(239, 68, 68, 0.1)', color: 'var(--accent-danger)', border: '1px solid var(--accent-danger)', borderRadius: 'var(--radius-md)', fontWeight: 'bold', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px' }}
                    >
                      <Trash2 size={16} /> ELIMINAR CLIENTE
                    </button>
                   )}
                </div>
              </div>
            ) : (
              <div className="empty-ticket">
                <User size={48} className="empty-icon text-muted" />
                <p>Selecciona un cliente para ver su historial y saldar deudas.</p>
              </div>
            )}
          </aside>
        </div>
      </main>

      {/* New Customer Modal */}
      {isModalOpen && (
        <div className="modal-overlay" style={{ position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.8)', zIndex: 1200, display: 'flex', alignItems: 'center', justifyContent: 'center', backdropFilter: 'blur(8px)' }}>
           <div className="modal-content" style={{ backgroundColor: 'var(--bg-secondary)', padding: '32px', borderRadius: 'var(--radius-lg)', width: '100%', maxWidth: '400px', border: '1px solid var(--border-color)' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
                <h2 style={{ fontSize: '24px', fontWeight: 'bold', display: 'flex', alignItems: 'center', gap: '12px' }}>
                  <Plus className="text-accent-primary" /> Nuevo Cliente
                </h2>
                <button onClick={() => setIsModalOpen(false)} className="icon-btn"><X size={24} /></button>
              </div>

              <form onSubmit={handleAddCustomer} style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
                 <div>
                    <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>Nombre Completo</label>
                    <input 
                      required
                      type="text"
                      className="search-input"
                      value={newName}
                      onChange={e => setNewName(e.target.value)}
                      placeholder="Ej. Juan Pérez"
                    />
                 </div>
                 <div>
                    <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>Teléfono / WhatsApp</label>
                    <input 
                      type="text"
                      className="search-input"
                      value={newPhone}
                      onChange={e => setNewPhone(e.target.value)}
                      placeholder="Ej. 2281234567"
                    />
                 </div>

                 <button type="submit" className="checkout-btn" style={{ width: '100%', padding: '16px', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px', fontWeight: 'bold', marginTop: '12px' }}>
                    <CheckCircle2 size={20} /> GUARDAR CLIENTE
                 </button>
              </form>
           </div>
        </div>
      )}
    </>
  );
}
