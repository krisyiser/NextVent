'use client';

import React, { useState, useEffect } from 'react';
import { Sidebar } from '@/components/Sidebar';
import { ProductModal } from '@/components/ProductModal';
import { getProducts, updateProduct, addProduct, deleteProduct } from '@/lib/storage';
import { Product } from '@/types';
import { Plus, Edit2, Trash2, Info } from 'lucide-react';


export default function Inventory() {
  const [products, setProducts] = useState<Product[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);


  useEffect(() => {
    const load = async () => { setProducts(await getProducts()); };
    load();
  }, []);

  const filteredProducts = products.filter(p => 
    p.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    p.category.toLowerCase().includes(searchQuery.toLowerCase()) ||
    (p.barcode && p.barcode.includes(searchQuery))
  );

  const handleOpenAddModal = () => {
    setEditingProduct(null);
    setIsModalOpen(true);
  };

  const handleOpenEditModal = (product: Product) => {
    setEditingProduct(product);
    setIsModalOpen(true);
  };

  const handleSaveProduct = async (product: Product) => {
    if (editingProduct) {
      await updateProduct(product);
    } else {
      await addProduct(product);
    }
    setProducts(await getProducts());
    setIsModalOpen(false);
    setEditingProduct(null);
  };



  const handleDelete = async (id: string) => {
    if (confirm('¿Estás seguro de eliminar este producto?')) {
      await deleteProduct(id);
      setProducts(await getProducts());
    }
  };




  return (
    <>
      <Sidebar activeModule="inventory" />
      <main className="main-content">
        <header className="top-bar">
          <div className="search-container" style={{ width: '100%', maxWidth: '600px' }}>
            <input 
              type="text" className="search-input" placeholder="Buscar por nombre, categoría o código..." 
              value={searchQuery} onChange={e => setSearchQuery(e.target.value)}
            />
          </div>
          <button className="checkout-btn" style={{ margin: 0, width: 'auto', padding: '10px 20px' }} onClick={handleOpenAddModal}>
            <Plus size={20} /> NUEVO PRODUCTO
          </button>
        </header>

        <div className="inventory-container" style={{ padding: '24px', overflowY: 'auto' }}>
          <table className="inventory-table" style={{ width: '100%', borderCollapse: 'collapse', backgroundColor: 'var(--bg-secondary)', borderRadius: 'var(--radius-lg)' }}>
            <thead>
              <tr style={{ textAlign: 'left', borderBottom: '1px solid var(--border-color)', backgroundColor: 'var(--bg-tertiary)' }}>
                <th style={{ padding: '16px' }}>Cod / Nombre</th>
                <th style={{ padding: '16px' }}>Categoría</th>
                <th style={{ padding: '16px' }}>Costo (Compra)</th>
                <th style={{ padding: '16px' }}>Precio (Venta)</th>
                <th style={{ padding: '16px' }}>Mayoreo (Precio / Cant)</th>
                <th style={{ padding: '16px' }}>Stock</th>
                <th style={{ padding: '16px' }}>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {filteredProducts.map(p => (
                <tr key={p.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                  <td style={{ padding: '16px' }}>
                    <div>
                      <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{p.barcode || 'S/N'}</div>
                      <div style={{ fontWeight: '600' }}>{p.name}</div>
                    </div>
                  </td>
                  <td style={{ padding: '16px' }}>{p.category}</td>
                  <td style={{ padding: '16px' }}>${p.cost.toFixed(2)}</td>
                  <td style={{ padding: '16px' }}>${p.price.toFixed(2)}</td>
                  <td style={{ padding: '16px' }}>
                    {p.wholesalePrice ? (
                      <div style={{ fontSize: '13px' }}>
                        <span style={{ color: 'var(--accent-warning)' }}>${p.wholesalePrice.toFixed(2)}</span> / {p.wholesaleThreshold} {p.unit}
                      </div>
                    ) : '---'}
                  </td>
                  <td style={{ padding: '16px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <div style={{ color: p.stock <= 5 ? 'var(--accent-danger)' : 'inherit' }}>
                        {p.stock} <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{p.unit}</span>
                        </div>
                        <button 
                            className="qty-btn" 
                            style={{ width: '24px', height: '24px', fontSize: '12px', borderRadius: '4px' }}
                            onClick={() => {
                                const val = prompt(`Agregar stock para ${p.name}:`);
                                if (val) {
                                    const amount = parseFloat(val);
                                    if (!isNaN(amount)) {
                                        handleSaveProduct({ ...p, stock: p.stock + amount });
                                    }
                                }
                            }}
                        >
                            <Plus size={14} />
                        </button>
                    </div>
                  </td>
                  <td style={{ padding: '16px' }}>
                    <div style={{ display: 'flex', gap: '8px' }}>
                      <button className="icon-btn" onClick={() => handleOpenEditModal(p)}><Edit2 size={18} /></button>
                      <button className="icon-btn" onClick={() => handleDelete(p.id)} style={{ color: 'var(--accent-danger)' }}><Trash2 size={18} /></button>
                    </div>
                  </td>
                </tr>

              ))}
            </tbody>
          </table>
          <div style={{ marginTop: '24px', display: 'flex', gap: '12px', alignItems: 'center', backgroundColor: 'rgba(59, 130, 246, 0.1)', padding: '16px', borderRadius: 'var(--radius-md)', color: 'var(--accent-primary)' }}>
              <Info size={20} />
              <span style={{ fontSize: '14px' }}><b>Margen de Ganancias:</b> El sistema calcula utilidades basadas en la diferencia entre "Costo de Compra" y "Precio de Venta". Completa todos los costos para ver reportes precisos.</span>
          </div>
        </div>
      </main>

      <ProductModal 
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSave={handleSaveProduct}
        product={editingProduct}
        title={editingProduct ? 'Editar Producto' : 'Nuevo Producto'}
      />
    </>
  );
}
