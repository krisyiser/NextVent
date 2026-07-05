'use client';

import React, { useState, useEffect } from 'react';
import { AppShell } from '@/components/ui';
import { Tag, Plus, Trash, Package, Stack, Percent, CurrencyDollar, CheckCircle, X } from 'phosphor-react';
import { getPromotions, savePromotion, deletePromotion, getProducts } from '@/lib/storage';
import { CATEGORIES } from '@/constants';

export default function PromotionsPage() {
  const [promotions, setPromotions] = useState<any[]>([]);
  const [products, setProducts] = useState<any[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);
  
  const [formData, setFormData] = useState({
    id: '',
    name: '',
    type: 'product', // 'product', 'category', 'multibuy'
    targetId: '',
    discountValue: '',
    discountType: 'percent', // 'percent', 'fixed'
    buyQty: '',
    payQty: '',
    isActive: true
  });

  const loadData = async () => {
    setPromotions(await getPromotions());
    setProducts(await getProducts());
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    const promo = {
        id: formData.id || `PROMO-${Date.now()}`,
        name: formData.name,
        type: formData.type as 'product' | 'category' | 'multibuy',
        targetId: formData.targetId,
        discountType: formData.discountType as 'percent' | 'fixed',
        discountValue: parseFloat(formData.discountValue as string) || 0,
        buyQty: parseInt(formData.buyQty as string) || 1,
        payQty: parseInt(formData.payQty as string) || 1,
        isActive: formData.isActive
    };
    await savePromotion(promo);
    setIsModalOpen(false);
    loadData();
  };

  const handleDelete = async (id: string) => {
    await deletePromotion(id);
    setDeleteConfirmId(null);
    loadData();
  };

  return (
    <>
      <AppShell activeModule="promotions">
        <header className="top-bar">
          <div className="ticket-title" style={{ fontSize: '24px', display: 'flex', alignItems: 'center', gap: '12px' }}>
            <Tag size={28} className="text-accent" weight="regular" />
            Gestión de Promociones y Descuentos
          </div>
          <button className="checkout-btn" onClick={() => {
              setFormData({ id: '', name: '', type: 'product', targetId: '', discountValue: '', discountType: 'percent', buyQty: '', payQty: '', isActive: true });
              setIsModalOpen(true);
          }} style={{ width: 'auto', padding: '10px 24px' }}>
            <Plus size={20} weight="regular" /> NUEVA PROMOCIÓN
          </button>
        </header>

        <div style={{ padding: '24px' }}>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: '20px' }}>
            {promotions.map(promo => (
              <div key={promo.id} style={{ backgroundColor: 'var(--bg-secondary)', borderRadius: 'var(--radius-lg)', border: '1px solid var(--border-color)', padding: '20px', position: 'relative' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '12px' }}>
                    <div>
                        <div style={{ fontSize: '12px', color: 'var(--text-muted)', textTransform: 'uppercase', fontWeight: 'bold' }}>
                            {promo.type === 'product' ? 'Producto' : promo.type === 'category' ? 'Categoría' : 'Multi-compra'}
                        </div>
                        <div style={{ fontSize: '18px', fontWeight: 'bold' }}>{promo.name}</div>
                    </div>
                    {deleteConfirmId === promo.id ? (
                        <div style={{ display: 'flex', gap: '4px' }}>
                            <button onClick={() => setDeleteConfirmId(null)} className="icon-btn" style={{ fontSize: '10px' }}>X</button>
                            <button onClick={() => handleDelete(promo.id)} className="icon-btn" style={{ color: 'var(--accent-danger)' }}><CheckCircle size={16} weight="regular" /></button>
                        </div>
                    ) : (
                        <button onClick={() => setDeleteConfirmId(promo.id)} className="icon-btn" style={{ color: 'var(--accent-danger)' }}>
                            <Trash size={18} weight="regular" />
                        </button>
                    )}
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '16px' }}>
                    <div style={{ 
                        padding: '12px', backgroundColor: promo.type === 'multibuy' ? 'rgba(234, 179, 8, 0.1)' : 'rgba(59, 130, 246, 0.1)', 
                        color: promo.type === 'multibuy' ? 'var(--accent-warning)' : 'var(--accent-primary)', borderRadius: 'var(--radius-md)',
                        fontSize: '20px', fontWeight: 'bold'
                    }}>
                        {promo.type === 'multibuy' ? `${promo.buyQty}x${promo.payQty}` : promo.discountType === 'percent' ? `-${promo.discountValue}%` : `-$${promo.discountValue}`}
                    </div>
                    <div style={{ fontSize: '14px', color: 'var(--text-secondary)' }}>
                        {promo.type === 'category' ? (
                            `Aplica a todos en: ${promo.targetId}`
                        ) : (
                            products.find(p => p.id === promo.targetId)?.name || 'Producto no encontrado'
                        )}
                    </div>
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <div style={{ width: '8px', height: '8px', borderRadius: '50%', backgroundColor: promo.isActive ? 'var(--accent-success)' : 'var(--text-muted)' }}></div>
                    <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>{promo.isActive ? 'Activa' : 'Inactiva'}</span>
                </div>
              </div>
            ))}
            
            {promotions.length === 0 && (
                <div style={{ gridColumn: '1 / -1', textAlign: 'center', padding: '60px', color: 'var(--text-muted)' }}>
                    <Tag size={48} style={{ marginBottom: '16px', opacity: 0.3 }} weight="regular" />
                    <p>No hay promociones activas. Crea una para aumentar tus ventas.</p>
                </div>
            )}
          </div>
        </div>
      </AppShell>
      {/* Modal Nueva Promo */}
      {isModalOpen && (
          <div className="modal-overlay" style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, backgroundColor: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1100, backdropFilter: 'blur(4px)' }}>
              <div className="modal-content" style={{ backgroundColor: 'var(--bg-secondary)', padding: '32px', borderRadius: 'var(--radius-lg)', width: '100%', maxWidth: '500px', border: '1px solid var(--border-color)' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
                      <h2 style={{ fontSize: '20px', fontWeight: 'bold' }}>Configurar Promoción</h2>
                      <button onClick={() => setIsModalOpen(false)} className="icon-btn"><X size={24} weight="regular" /></button>
                  </div>

                  <form onSubmit={handleSave}>
                      <div style={{ marginBottom: '16px' }}>
                          <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>Nombre de la Campaña</label>
                          <input 
                              required
                              value={formData.name}
                              onChange={e => setFormData({ ...formData, name: e.target.value })}
                              style={{ width: '100%', padding: '12px', backgroundColor: 'var(--bg-tertiary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)', color: 'var(--text-primary)' }}
                              placeholder="Ej: Ofertas de Verano"
                          />
                      </div>

                      <div style={{ display: 'flex', gap: '16px', marginBottom: '16px' }}>
                          <div style={{ flex: 1 }}>
                              <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>Tipo</label>
                              <select 
                                  value={formData.type}
                                  onChange={e => setFormData({ ...formData, type: e.target.value, targetId: '' })}
                                  style={{ width: '100%', padding: '12px', backgroundColor: 'var(--bg-tertiary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)', color: 'var(--text-primary)' }}
                              >
                                  <option value="product">Producto Específico</option>
                                  <option value="category">Categoría Completa</option>
                                  <option value="multibuy">Multi-compra (NxM)</option>
                              </select>
                          </div>
                      </div>

                      <div style={{ marginBottom: '16px' }}>
                          <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>
                              {formData.type === 'category' ? 'Seleccionar Categoría' : 'Seleccionar Producto'}
                          </label>
                          {formData.type === 'category' ? (
                              <select 
                                  required
                                  value={formData.targetId}
                                  onChange={e => setFormData({ ...formData, targetId: e.target.value })}
                                  style={{ width: '100%', padding: '12px', backgroundColor: 'var(--bg-tertiary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)', color: 'var(--text-primary)' }}
                              >
                                  <option value="">Seleccione...</option>
                                  {CATEGORIES.filter(c => c !== 'Todos').map(c => <option key={c} value={c}>{c}</option>)}
                              </select>
                          ) : (
                              <select 
                                  required
                                  value={formData.targetId}
                                  onChange={e => setFormData({ ...formData, targetId: e.target.value })}
                                  style={{ width: '100%', padding: '12px', backgroundColor: 'var(--bg-tertiary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)', color: 'var(--text-primary)' }}
                              >
                                  <option value="">Seleccione...</option>
                                  {products.map(p => <option key={p.id} value={p.id}>{p.name} (${p.price})</option>)}
                              </select>
                          )}
                      </div>

                      {formData.type === 'multibuy' ? (
                          <div style={{ display: 'flex', gap: '16px', marginBottom: '24px' }}>
                              <div style={{ flex: 1 }}>
                                  <label style={{ display: 'block', fontSize: '12px', color: 'var(--text-secondary)', marginBottom: '4px' }}>Lleva (Cant.)</label>
                                  <input 
                                      required type="number" value={formData.buyQty}
                                      onChange={e => setFormData({ ...formData, buyQty: e.target.value })}
                                      style={{ width: '100%', padding: '12px', backgroundColor: 'var(--bg-tertiary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)', color: 'var(--text-primary)' }}
                                      placeholder="Ej: 2"
                                  />
                              </div>
                              <div style={{ flex: 1 }}>
                                  <label style={{ display: 'block', fontSize: '12px', color: 'var(--text-secondary)', marginBottom: '4px' }}>Paga (Cant.)</label>
                                  <input 
                                      required type="number" value={formData.payQty}
                                      onChange={e => setFormData({ ...formData, payQty: e.target.value })}
                                      style={{ width: '100%', padding: '12px', backgroundColor: 'var(--bg-tertiary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)', color: 'var(--text-primary)' }}
                                      placeholder="Ej: 1"
                                  />
                              </div>
                          </div>
                      ) : (
                          <div style={{ display: 'flex', gap: '16px', marginBottom: '24px' }}>
                              <div style={{ flex: 1 }}>
                                  <label style={{ display: 'block', fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '8px' }}>Descuento</label>
                                  <div style={{ display: 'flex', alignItems: 'center', backgroundColor: 'var(--bg-tertiary)', borderRadius: 'var(--radius-md)', border: '1px solid var(--border-color)', padding: '0 12px' }}>
                                      <input 
                                          required
                                          type="number"
                                          value={formData.discountValue}
                                          onChange={e => setFormData({ ...formData, discountValue: e.target.value })}
                                          style={{ width: '100%', padding: '12px 0', backgroundColor: 'transparent', border: 'none', color: 'var(--text-primary)' }}
                                      />
                                      <select 
                                          value={formData.discountType}
                                          onChange={e => setFormData({ ...formData, discountType: e.target.value })}
                                          style={{ background: 'none', border: 'none', color: 'var(--accent-primary)', fontWeight: 'bold', outline: 'none' }}
                                      >
                                          <option value="percent">%</option>
                                          <option value="fixed">$</option>
                                      </select>
                                  </div>
                              </div>
                          </div>
                      )}

                      <button type="submit" style={{ width: '100%', padding: '16px', backgroundColor: 'var(--accent-primary)', border: 'none', borderRadius: 'var(--radius-md)', color: 'var(--text-on-accent)', fontWeight: 'bold', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '10px' }}>
                          <CheckCircle size={20} weight="regular" /> GUARDAR PROMOCIÓN
                      </button>
                  </form>
              </div>
          </div>
      )}
    </>
  );
}
