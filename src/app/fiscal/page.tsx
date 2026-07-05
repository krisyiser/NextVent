'use client';
import React, { useState, useEffect } from 'react';
import { AppShell } from '@/components/ui';
import { Receipt, Buildings, CloudArrowUp, CheckCircle, Warning, MagnifyingGlass, ShoppingCart } from 'phosphor-react';
import { toast } from 'sonner';
import { getSetting } from '@/lib/storage';
import { mapearFacturaGlobal } from '@/lib/facturamaMapper';
import { enviarAlSAT, procesarExitoFiscal } from '@/lib/facturamaService';
import AppDatabase from '@/lib/database';

export default function FiscalPage() {
  const [sales, setSales] = useState<any[]>([]);
  const [selectedSales, setSelectedSales] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [storeName, setStoreName] = useState('');
  const [rfc, setRfc] = useState('');
  const [cpEmisor, setCpEmisor] = useState('');

  useEffect(() => {
    loadSettings();
    loadPendingSales();
  }, []);

  const loadSettings = async () => {
    setStoreName(await getSetting('pos_store_name', 'Mi Tienda'));
    setRfc(await getSetting('pos_store_rfc', ''));
    setCpEmisor(await getSetting('pos_store_address', '')); // Assuming CP is part of address or should be a setting
  };

  const loadPendingSales = async () => {
    try {
      const db = await AppDatabase.getInstance();
      const rows = await db.rawSelect<any>("SELECT * FROM sales WHERE estado_fiscal = 'PENDIENTE' OR estado_fiscal IS NULL ORDER BY date DESC LIMIT 100");
      setSales(rows);
    } catch (e) {
      toast.error('Error al cargar las ventas pendientes');
    }
  };

  const handleToggleSelect = (id: string) => {
    if (selectedSales.includes(id)) {
      setSelectedSales(selectedSales.filter(sid => sid !== id));
    } else {
      setSelectedSales([...selectedSales, id]);
    }
  };

  const handleSelectAll = () => {
    if (selectedSales.length === sales.length) {
      setSelectedSales([]);
    } else {
      setSelectedSales(sales.map(s => s.id));
    }
  };

  const handleGenerateGlobalInvoice = async () => {
    if (selectedSales.length === 0) {
      toast.error('Selecciona al menos una venta para la factura global.');
      return;
    }
    
    if (!rfc) {
      toast.error('Por favor, configura el RFC de tu empresa en Configuración > Empresa > Identidad.');
      return;
    }

    try {
      setIsLoading(true);
      
      const apiUser = await getSetting('pos_facturama_user', '');
      const apiSecret = await getSetting('pos_facturama_secret', '');
      
      if (!apiUser || !apiSecret) {
        toast.error('Por favor, configura tus credenciales de Facturama en Configuración > Empresa > Facturación CFDI.');
        setIsLoading(false);
        return;
      }

      const ventasPublico = sales.filter(s => selectedSales.includes(s.id));
      
      const fecha = new Date();
      const mesActual = (fecha.getMonth() + 1).toString().padStart(2, '0');
      const anioActual = fecha.getFullYear().toString();
      
      const cfdiPayload = mapearFacturaGlobal(ventasPublico, "01", mesActual, anioActual, "11000"); // CP hardcoded for now, should extract from settings
      
      const result = await enviarAlSAT(cfdiPayload, apiUser, apiSecret);
      
      // Update each sale
      for (const sale of ventasPublico) {
        await procesarExitoFiscal(sale.id, result);
      }
      
      toast.success('Factura Global Timbrada Exitosamente y Guardada en Documentos.');
      
      // Refresh list
      setSelectedSales([]);
      await loadPendingSales();
    } catch (error: any) {
      console.error(error);
      toast.error(`Error al timbrar: ${error.message}`);
    } finally {
      setIsLoading(false);
    }
  };

  const totalSeleccionado = sales.filter(s => selectedSales.includes(s.id)).reduce((acc, curr) => acc + curr.total, 0);

  return (
    <AppShell activeModule="fiscal" mainStyle={{ display: 'flex', flexDirection: 'column' }}>
        <header className="top-bar">
          <div className="ticket-title" style={{ fontSize: '24px', display: 'flex', alignItems: 'center', gap: '12px', minWidth: 'max-content' }}>
            <Receipt size={28} className="text-accent" weight="regular" />
            Facturación y Herramientas Fiscales
          </div>
        </header>
        <div className="content-area">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 350px', gap: '24px', height: '100%' }}>
            
            {/* Lista de Ventas Pendientes */}
            <div style={{ backgroundColor: 'var(--bg-primary)', borderRadius: 'var(--radius-lg)', border: '1px solid var(--border-color)', display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
              <div style={{ padding: '20px', borderBottom: '1px solid var(--border-color)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div>
                  <h2 style={{ fontSize: '18px', fontWeight: 'bold', display: 'flex', alignItems: 'center', gap: '8px' }}><Receipt size={24} className="text-accent" /> Ventas al Público en General</h2>
                  <p style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>Selecciona los tickets para generar la Factura Global (CFDI 4.0).</p>
                </div>
                <button 
                  onClick={handleSelectAll}
                  style={{ padding: '8px 16px', backgroundColor: 'var(--bg-secondary)', border: '1px solid var(--border-color)', borderRadius: '4px', color: 'var(--text-primary)', cursor: 'pointer', fontSize: '13px', fontWeight: 600 }}
                >
                  {selectedSales.length === sales.length && sales.length > 0 ? 'Deseleccionar Todos' : 'Seleccionar Todos'}
                </button>
              </div>

              <div style={{ flex: 1, overflowY: 'auto', padding: '20px' }}>
                {sales.length === 0 ? (
                  <div style={{ textAlign: 'center', padding: '40px', color: 'var(--text-muted)' }}>
                    <Receipt size={48} style={{ margin: '0 auto 16px', opacity: 0.5 }} />
                    <p>No hay ventas pendientes de facturar.</p>
                  </div>
                ) : (
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                    {sales.map(sale => {
                      const isSelected = selectedSales.includes(sale.id);
                      return (
                        <div 
                          key={sale.id}
                          onClick={() => handleToggleSelect(sale.id)}
                          style={{ 
                            padding: '16px', borderRadius: '8px', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                            border: isSelected ? '2px solid var(--accent-primary)' : '1px solid var(--border-color)',
                            backgroundColor: isSelected ? 'rgba(15, 82, 186, 0.05)' : 'var(--bg-secondary)',
                            transition: 'all 0.15s'
                          }}
                        >
                          <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
                            <div style={{ width: '24px', height: '24px', borderRadius: '50%', border: '2px solid', borderColor: isSelected ? 'var(--accent-primary)' : 'var(--text-muted)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                              {isSelected && <div style={{ width: '12px', height: '12px', borderRadius: '50%', backgroundColor: 'var(--accent-primary)' }} />}
                            </div>
                            <div>
                              <div style={{ fontWeight: 600, fontSize: '15px' }}>Ticket: {sale.id}</div>
                              <div style={{ fontSize: '13px', color: 'var(--text-secondary)', display: 'flex', gap: '12px' }}>
                                <span>{new Date(sale.date).toLocaleString()}</span>
                                <span>•</span>
                                <span>{(typeof sale.items === 'string' ? JSON.parse(sale.items) : sale.items).length} artículos</span>
                              </div>
                            </div>
                          </div>
                          <div style={{ fontSize: '18px', fontWeight: 'bold', color: 'var(--accent-success)' }}>
                            ${sale.total.toFixed(2)}
                          </div>
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
            </div>

            {/* Panel de Resumen y Acciones */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
              
              <div style={{ backgroundColor: 'var(--bg-primary)', padding: '24px', borderRadius: 'var(--radius-lg)', border: '1px solid var(--border-color)' }}>
                <h3 style={{ fontSize: '16px', fontWeight: 'bold', marginBottom: '16px', display: 'flex', alignItems: 'center', gap: '8px' }}><Buildings size={20} className="text-accent" /> Datos del Emisor</h3>
                <div style={{ fontSize: '13px', color: 'var(--text-secondary)', marginBottom: '8px' }}><strong>Empresa:</strong> {storeName || 'No configurado'}</div>
                <div style={{ fontSize: '13px', color: 'var(--text-secondary)', marginBottom: '8px' }}><strong>RFC:</strong> {rfc || 'No configurado'}</div>
                <div style={{ fontSize: '13px', color: 'var(--text-secondary)' }}><strong>Régimen:</strong> 612 (Personas Físicas con Actividades Empresariales y Profesionales)</div>
              </div>

              <div style={{ backgroundColor: 'var(--bg-primary)', padding: '24px', borderRadius: 'var(--radius-lg)', border: '1px solid var(--border-color)', flex: 1, display: 'flex', flexDirection: 'column' }}>
                <h3 style={{ fontSize: '16px', fontWeight: 'bold', marginBottom: '24px', borderBottom: '1px solid var(--border-color)', paddingBottom: '16px' }}>Resumen de Facturación</h3>
                
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '12px', fontSize: '14px', color: 'var(--text-secondary)' }}>
                  <span>Tickets Seleccionados:</span>
                  <span style={{ fontWeight: 'bold', color: 'var(--text-primary)' }}>{selectedSales.length}</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '12px', fontSize: '14px', color: 'var(--text-secondary)' }}>
                  <span>Subtotal:</span>
                  <span style={{ fontWeight: 'bold', color: 'var(--text-primary)' }}>${(totalSeleccionado / 1.16).toFixed(2)}</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '24px', fontSize: '14px', color: 'var(--text-secondary)' }}>
                  <span>IVA (16%):</span>
                  <span style={{ fontWeight: 'bold', color: 'var(--text-primary)' }}>${(totalSeleccionado - (totalSeleccionado / 1.16)).toFixed(2)}</span>
                </div>
                
                <div style={{ display: 'flex', justifyContent: 'space-between', padding: '16px 0', borderTop: '1px solid var(--border-color)', borderBottom: '1px solid var(--border-color)', marginBottom: 'auto' }}>
                  <span style={{ fontSize: '16px', fontWeight: 'bold' }}>TOTAL GLOBAL:</span>
                  <span style={{ fontSize: '24px', fontWeight: 'bold', color: 'var(--accent-success)' }}>${totalSeleccionado.toFixed(2)}</span>
                </div>

                <button
                  onClick={handleGenerateGlobalInvoice}
                  disabled={selectedSales.length === 0 || isLoading}
                  style={{
                    marginTop: '24px', width: '100%', padding: '16px', borderRadius: 'var(--radius-md)', border: 'none',
                    backgroundColor: selectedSales.length > 0 && !isLoading ? 'var(--accent-primary)' : 'var(--bg-tertiary)',
                    color: selectedSales.length > 0 && !isLoading ? 'var(--text-on-accent)' : 'var(--text-muted)',
                    fontSize: '15px', fontWeight: 'bold', cursor: selectedSales.length > 0 && !isLoading ? 'pointer' : 'not-allowed',
                    display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px', transition: 'var(--transition)'
                  }}
                >
                  {isLoading ? (
                    <>Timbrando en SAT...</>
                  ) : (
                    <><CloudArrowUp size={20} weight="bold" /> Timbrar Factura Global</>
                  )}
                </button>
                <div style={{ fontSize: '11px', color: 'var(--text-muted)', textAlign: 'center', marginTop: '12px' }}>
                  El XML y PDF serán descargados automáticamente y se enviarán al portal de Facturama.
                </div>
              </div>
            </div>

          </div>
        </div>
    </AppShell>
  );
}
