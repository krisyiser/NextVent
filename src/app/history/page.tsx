'use client';

import React, { useState, useEffect, useMemo } from 'react';
import { AppShell } from '@/components/ui';
import { getSales, getSetting, getShifts, cancelSale, getAssistances, Asistencia } from '@/lib/storage';
import { Sale, Shift } from '@/types';
import { invoke } from '@tauri-apps/api/core';
import { 
  ChartPieSlice, Clock, Calendar, CaretDown, CaretUp, TrendUp, 
  CurrencyDollar, ShoppingCart, User, Medal, ClipboardText, WarningCircle, 
  ArrowDownLeft, ArrowUpRight, Camera
} from 'phosphor-react';

export default function History() {
  const [history, setHistory] = useState<Sale[]>([]);
  const [shifts, setShifts] = useState<Shift[]>([]);
  const [assistances, setAssistances] = useState<Asistencia[]>([]);
  const [view, setView] = useState<'sales' | 'shifts' | 'assistances'>('sales');
  const [topSellings, setTopSellings] = useState<any[]>([]);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [dateFilter, setDateFilter] = useState<'today' | 'week' | 'all'>('all');
  const [cancelConfirmId, setCancelConfirmId] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      const sales = await getSales();
      setHistory(sales);
      setShifts(await getShifts());
      setAssistances(await getAssistances());
      
      // Calculate top 5 selling products dynamically from sales
      const productCounts: Record<string, { name: string, qty: number }> = {};
      sales.forEach(sale => {
        if (sale.isCancelled) return;
        sale.items.forEach(item => {
          if (!productCounts[item.productId]) {
            productCounts[item.productId] = { name: item.name, qty: 0 };
          }
          productCounts[item.productId].qty += item.quantity;
        });
      });
      const sorted = Object.values(productCounts)
        .sort((a, b) => b.qty - a.qty)
        .slice(0, 5);
      setTopSellings(sorted);
    };
    load();
  }, []);

  const filteredHistory = useMemo(() => {
    if (dateFilter === 'all') return history;
    const now = new Date();
    const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const startOfWeek = new Date(now.setDate(now.getDate() - now.getDay()));

    return history.filter(sale => {
        const saleDate = new Date(sale.date);
        if (dateFilter === 'today') return saleDate >= startOfToday;
        if (dateFilter === 'week') return saleDate >= startOfWeek;
        return true;
    });
  }, [history, dateFilter]);

  const stats = useMemo(() => {
    return history.reduce((acc, sale) => {
      if (sale.isCancelled) return acc;
      acc.totalSales += sale.total;
      acc.totalProfit += sale.profit;
      acc.totalCost += sale.totalCost;
      if (sale.isCredit) acc.totalCredits += sale.total;
      
      const method = sale.paymentMethod || 'Cash';
      acc.byMethod[method] = (acc.byMethod[method] || 0) + sale.total;
      
      return acc;
    }, { totalSales: 0, totalProfit: 0, totalCost: 0, totalCredits: 0, byMethod: {} as Record<string, number> });
  }, [history]);

  const handleCancelSale = async (id: string) => {
    await cancelSale(id);
    setCancelConfirmId(null);
    const sales = await getSales();
    setHistory(sales);
    
    // Recalculate top 5 selling products dynamically
    const productCounts: Record<string, { name: string, qty: number }> = {};
    sales.forEach(sale => {
      if (sale.isCancelled) return;
      sale.items.forEach(item => {
        if (!productCounts[item.productId]) {
          productCounts[item.productId] = { name: item.name, qty: 0 };
        }
        productCounts[item.productId].qty += item.quantity;
      });
    });
    const sorted = Object.values(productCounts)
      .sort((a, b) => b.qty - a.qty)
      .slice(0, 5);
    setTopSellings(sorted);
  };

  const handleReprint = async (sale: Sale) => {
      try {
          const ticketText = `TICKET #${sale.id}\nTotal: $${sale.total}\n`;
          // Create dummy buffer representing ESC/POS
          const buffer = Array.from(new TextEncoder().encode(ticketText));
          await invoke('print_receipt', { buffer, isReprint: true });
          alert('Ticket enviado a impresión (Copia de Seguridad). Revisa el log de impresora.');
      } catch (e) {
          console.error(e);
      }
  };

  const formatDate = (isoStr: string) => {
    const d = new Date(isoStr);
    return d.toLocaleString('es-MX', { 
        day: '2-digit', month: 'short', year: 'numeric', 
        hour: '2-digit', minute: '2-digit' 
    });
  };

  return (
    <AppShell activeModule="history" mainStyle={{ display: 'flex', flexDirection: 'column' }}>
        <header className="top-bar">
          <div className="ticket-title" style={{ fontSize: '24px', display: 'flex', alignItems: 'center', gap: '12px' }}>
            <ClipboardText size={28} className="text-accent" weight="regular" />
            Auditoría de Operaciones
          </div>
          <div style={{ display: 'flex', gap: '8px', backgroundColor: 'var(--bg-secondary)', padding: '6px', borderRadius: 'var(--radius-md)', border: '1px solid var(--border-color)' }}>
              <button 
                onClick={() => setView('sales')}
                style={{ 
                  padding: '8px 20px', borderRadius: 'var(--radius-sm)', border: 'none', cursor: 'pointer', fontSize: '13px', fontWeight: 'bold',
                  backgroundColor: view === 'sales' ? 'var(--accent-primary)' : 'transparent',
                  color: view === 'sales' ? '#fff' : 'var(--text-secondary)',
                  transition: 'var(--transition)'
                }}
              >VENTAS</button>
              <button 
                onClick={() => setView('shifts')}
                style={{ 
                  padding: '8px 20px', borderRadius: 'var(--radius-sm)', border: 'none', cursor: 'pointer', fontSize: '13px', fontWeight: 'bold',
                  backgroundColor: view === 'shifts' ? 'var(--accent-primary)' : 'transparent',
                  color: view === 'shifts' ? '#fff' : 'var(--text-secondary)',
                  transition: 'var(--transition)'
                }}
              >CORTES DE CAJA</button>
              <button 
                onClick={() => setView('assistances')}
                style={{ 
                  padding: '8px 20px', borderRadius: 'var(--radius-sm)', border: 'none', cursor: 'pointer', fontSize: '13px', fontWeight: 'bold',
                  backgroundColor: view === 'assistances' ? 'var(--accent-primary)' : 'transparent',
                  color: view === 'assistances' ? '#fff' : 'var(--text-secondary)',
                  transition: 'var(--transition)'
                }}
              >BITÁCORA ASISTENCIAS</button>
          </div>
        </header>

        <div className="history-container" style={{ padding: '24px', overflowY: 'auto', flex: 1 }}>
          {view === 'sales' ? (
            <>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '16px', maxWidth: '1000px', margin: '0 auto 32px' }}>
                <div style={{ backgroundColor: 'var(--bg-secondary)', padding: '20px', borderRadius: 'var(--radius-lg)', border: '1px solid var(--border-color)', display: 'flex', alignItems: 'center', gap: '16px' }}>
                    <div style={{ padding: '12px', backgroundColor: 'rgba(59, 130, 246, 0.1)', color: 'var(--accent-primary)', borderRadius: 'var(--radius-md)' }}><CurrencyDollar size={24} weight="regular" /></div>
                    <div>
                      <div style={{ color: 'var(--text-secondary)', fontSize: '12px' }}>VENTAS TOTALES</div>
                      <div style={{ fontSize: '20px', fontWeight: 'bold' }}>${stats.totalSales.toFixed(2)}</div>
                    </div>
                </div>
                <div style={{ backgroundColor: 'var(--bg-secondary)', padding: '20px', borderRadius: 'var(--radius-lg)', border: '1px solid var(--border-color)', display: 'flex', alignItems: 'center', gap: '16px' }}>
                    <div style={{ padding: '12px', backgroundColor: 'rgba(16, 185, 129, 0.1)', color: 'var(--accent-success)', borderRadius: 'var(--radius-md)' }}><TrendUp size={24} weight="regular" /></div>
                    <div>
                      <div style={{ color: 'var(--text-secondary)', fontSize: '12px' }}>UTILIDAD NETA</div>
                      <div style={{ fontSize: '20px', fontWeight: 'bold', color: 'var(--accent-success)' }}>${stats.totalProfit.toFixed(2)}</div>
                    </div>
                </div>
                
                {/* Payment Method Breakdown */}
                <div style={{ backgroundColor: 'var(--bg-secondary)', padding: '20px', borderRadius: 'var(--radius-lg)', border: '1px solid var(--border-color)', display: 'flex', flexDirection: 'column', gap: '8px' }}>
                    <div style={{ color: 'var(--text-secondary)', fontSize: '10px', fontWeight: 'bold', textTransform: 'uppercase' }}>Por Método de Pago</div>
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: '12px' }}>
                        <div style={{ fontSize: '13px' }}>💵 <span style={{ color: 'var(--text-muted)' }}>Efec:</span> <b>${(stats.byMethod['Cash'] || 0).toFixed(0)}</b></div>
                        <div style={{ fontSize: '13px' }}>💳 <span style={{ color: 'var(--text-muted)' }}>Tarj:</span> <b>${(stats.byMethod['Card'] || 0).toFixed(0)}</b></div>
                        <div style={{ fontSize: '13px' }}>📱 <span style={{ color: 'var(--text-muted)' }}>Trans:</span> <b>${(stats.byMethod['Transfer'] || 0).toFixed(0)}</b></div>
                    </div>
                </div>

                <div style={{ backgroundColor: 'var(--bg-secondary)', padding: '20px', borderRadius: 'var(--radius-lg)', border: '1px solid var(--border-color)', display: 'flex', alignItems: 'center', gap: '16px' }}>
                    <div style={{ padding: '12px', backgroundColor: 'rgba(239, 68, 68, 0.1)', color: 'var(--accent-danger)', borderRadius: 'var(--radius-md)' }}><User size={24} weight="regular" /></div>
                    <div>
                      <div style={{ color: 'var(--text-secondary)', fontSize: '12px' }}>POR COBRAR (FIADO)</div>
                      <div style={{ fontSize: '20px', fontWeight: 'bold', color: 'var(--accent-danger)' }}>${stats.totalCredits.toFixed(2)}</div>
                    </div>
                </div>
              </div>

              <div style={{ maxWidth: '1000px', margin: '0 auto 32px', display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '32px' }}>
                <div style={{ backgroundColor: 'var(--bg-secondary)', borderRadius: 'var(--radius-lg)', padding: '24px', border: '1px solid var(--border-color)' }}>
                  <h3 style={{ fontSize: '18px', fontWeight: 'bold', marginBottom: '20px', display: 'flex', alignItems: 'center', gap: '10px' }}>
                    <Medal size={20} className="text-warning" weight="regular" />
                    Top 5 Más Vendidos
                  </h3>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                      {topSellings.length === 0 ? <div style={{ color: 'var(--text-muted)', fontSize: '13px' }}>Sin datos aún.</div> : 
                        topSellings.map((p, i) => (
                          <div key={i} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: '14px' }}>
                            <div style={{ display: 'flex', gap: '12px', alignItems: 'center' }}>
                                <span style={{ color: 'var(--text-muted)', fontWeight: 'bold' }}>#{i+1}</span>
                                <span>{p.name}</span>
                            </div>
                            <div style={{ fontWeight: 'bold', color: 'var(--accent-primary)' }}>{p.qty.toFixed(0)} <span style={{ fontSize: '11px', fontWeight: 'normal' }}>uds</span></div>
                          </div>
                        ))
                      }
                  </div>
                </div>
                <div style={{ backgroundColor: 'var(--bg-secondary)', borderRadius: 'var(--radius-lg)', padding: '24px', border: '1px solid var(--border-color)', display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', textAlign: 'center', color: 'var(--text-secondary)' }}>
                    <TrendUp size={48} style={{ opacity: 0.1, marginBottom: '16px' }} weight="regular" />
                    <p style={{ fontSize: '13px' }}>Análisis de tendencias disponible al acumular más de 100 ventas.</p>
                </div>
              </div>

              <div style={{ maxWidth: '800px', margin: '0 auto 16px', display: 'flex', justifyContent: 'center', gap: '12px' }}>
                <button 
                    onClick={() => setDateFilter('today')}
                    style={{ fontSize: '12px', padding: '6px 16px', borderRadius: '20px', border: '1px solid var(--border-color)', backgroundColor: dateFilter === 'today' ? 'var(--accent-primary)' : 'transparent', color: dateFilter === 'today' ? '#ffffff' : 'var(--text-secondary)', cursor: 'pointer', fontWeight: 'bold' }}
                >Hoy</button>
                <button 
                    onClick={() => setDateFilter('week')}
                    style={{ fontSize: '12px', padding: '6px 16px', borderRadius: '20px', border: '1px solid var(--border-color)', backgroundColor: dateFilter === 'week' ? 'var(--accent-primary)' : 'transparent', color: dateFilter === 'week' ? '#ffffff' : 'var(--text-secondary)', cursor: 'pointer', fontWeight: 'bold' }}
                >Semana</button>
                <button 
                    onClick={() => setDateFilter('all')}
                    style={{ fontSize: '12px', padding: '6px 16px', borderRadius: '20px', border: '1px solid var(--border-color)', backgroundColor: dateFilter === 'all' ? 'var(--accent-primary)' : 'transparent', color: dateFilter === 'all' ? '#ffffff' : 'var(--text-secondary)', cursor: 'pointer', fontWeight: 'bold' }}
                >Todo</button>
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', maxWidth: '800px', margin: '0 auto' }}>
                {filteredHistory.length === 0 ? (
                  <div className="empty-ticket" style={{ marginTop: '100px' }}>
                    <ChartPieSlice size={64} className="empty-icon text-muted" weight="regular" />
                    <p>No hay ventas registradas para este periodo.</p>
                  </div>
                ) : (
                  filteredHistory.map(sale => (
                    <div key={sale.id} 
                         style={{ 
                           backgroundColor: 'var(--bg-secondary)', borderRadius: 'var(--radius-lg)', border: '1px solid var(--border-color)',
                           padding: '20px', transition: 'var(--transition)', 
                           opacity: sale.isCancelled ? 0.5 : 1,
                           position: 'relative',
                           borderColor: sale.isCancelled ? 'var(--accent-danger)' : 'var(--border-color)'
                         }}
                    >
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', cursor: 'pointer' }} 
                           onClick={() => setExpandedId(expandedId === sale.id ? null : sale.id)}>
                        <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                            <span style={{ fontWeight: 'bold', fontSize: '18px', textDecoration: sale.isCancelled ? 'line-through' : 'none' }}>#{sale.id}</span>
                            {sale.isCredit && <span style={{ backgroundColor: 'var(--accent-danger)', color: 'white', fontSize: '10px', padding: '2px 6px', borderRadius: '4px', fontWeight: 'bold' }}>FIADO</span>}
                            {sale.isCancelled && <span style={{ backgroundColor: 'var(--text-muted)', color: 'white', fontSize: '10px', padding: '2px 6px', borderRadius: '4px', fontWeight: 'bold' }}>ANULADA</span>}
                          </div>
                          <div style={{ color: 'var(--text-secondary)', fontSize: '13px', display: 'flex', alignItems: 'center', gap: '6px' }}>
                            <Calendar size={14} weight="regular" />
                            {formatDate(sale.date)}
                            {sale.isCancelled && <span style={{ marginLeft: '12px' }}>({formatDate(sale.cancelledAt!)})</span>}
                            <span style={{ marginLeft: '12px', fontSize: '14px' }}>
                                {sale.paymentMethod === 'Cash' ? '💵' : sale.paymentMethod === 'Card' ? '💳' : sale.paymentMethod === 'Transfer' ? '📱' : '📝'}
                            </span>
                          </div>
                        </div>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '20px' }}>
                          <div style={{ textAlign: 'right' }}>
                            <div style={{ fontSize: '20px', fontWeight: 'bold', color: sale.isCancelled ? 'var(--text-muted)' : 'var(--text-primary)', textDecoration: sale.isCancelled ? 'line-through' : 'none' }}>
                              ${sale.total.toFixed(2)}
                            </div>
                            {!sale.isCancelled && (
                              <div style={{ fontSize: '12px', color: 'var(--accent-success)' }}>
                                +${sale.profit.toFixed(2)} utilidad
                              </div>
                            )}
                          </div>
                          <button className="icon-btn">
                            {expandedId === sale.id ? <CaretUp size={20} weight="regular" /> : <CaretDown size={20} weight="regular" />}
                          </button>
                        </div>
                      </div>

                      {expandedId === sale.id && (
                        <div style={{ marginTop: '20px', paddingTop: '20px', borderTop: '1px dashed var(--border-color)' }}>
                            {!sale.isCancelled && (
                                <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: '16px' }}>
                                    {cancelConfirmId === sale.id ? (
                                        <div style={{ display: 'flex', alignItems: 'center', gap: '12px', backgroundColor: 'rgba(239, 68, 68, 0.1)', padding: '8px 16px', borderRadius: 'var(--radius-md)' }}>
                                            <span style={{ fontSize: '12px', color: 'var(--accent-danger)', fontWeight: 'bold' }}>¿Anular venta?</span>
                                            <button onClick={() => setCancelConfirmId(null)} style={{ fontSize: '12px', background: 'none', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer' }}>No</button>
                                            <button onClick={() => handleCancelSale(sale.id)} style={{ fontSize: '12px', background: 'var(--accent-danger)', border: 'none', color: 'var(--text-on-danger)', padding: '4px 12px', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold' }}>SÍ, ANULAR</button>
                                        </div>
                                    ) : (
                                        <div style={{ display: 'flex', gap: '8px' }}>
                                            <button className="checkout-btn" 
                                                    style={{ backgroundColor: 'transparent', border: '1px solid var(--text-secondary)', color: 'var(--text-secondary)', fontSize: '12px', padding: '8px 16px', margin: 0, width: 'auto' }}
                                                    onClick={(e) => { e.stopPropagation(); handleReprint(sale); }}
                                            >
                                                REIMPRIMIR TICKET
                                            </button>
                                            <button className="checkout-btn" 
                                                    style={{ backgroundColor: 'transparent', border: '1px solid var(--accent-danger)', color: 'var(--accent-danger)', fontSize: '12px', padding: '8px 16px', margin: 0, width: 'auto' }}
                                                    onClick={(e) => { e.stopPropagation(); setCancelConfirmId(sale.id); }}
                                            >
                                                ANULAR ESTA VENTA
                                            </button>
                                        </div>
                                    )}
                                </div>
                            )}
                          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', marginBottom: '16px' }}>
                            {sale.items.map((item, idx) => (
                               <div key={idx} style={{ display: 'flex', justifyContent: 'space-between', fontSize: '14px' }}>
                                 <div style={{ display: 'flex', gap: '10px' }}>
                                   <span style={{ color: 'var(--text-muted)' }}>{item.quantity}x</span>
                                   <span>{item.name}</span>
                                 </div>
                                 <div style={{ display: 'flex', gap: '20px' }}>
                                    <div style={{ color: 'var(--text-muted)', fontSize: '12px' }}>Costo: ${item.cost.toFixed(2)}</div>
                                    <div style={{ fontWeight: '500' }}>${item.price.toFixed(2)}</div>
                                 </div>
                               </div>
                            ))}
                          </div>
                          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '12px', padding: '16px', backgroundColor: 'var(--bg-primary)', borderRadius: 'var(--radius-md)', fontSize: '13px' }}>
                             <div><b>Paga con:</b> ${sale.paidAmount.toFixed(2)}</div>
                             <div><b>Cambio:</b> ${sale.changeAmount.toFixed(2)}</div>
                             <div style={{ textAlign: 'right', color: 'var(--accent-success)' }}><b>Utilidad Venta:</b> ${sale.profit.toFixed(2)}</div>
                          </div>
                        </div>
                      )}
                    </div>
                  ))
                )}
              </div>
            </>
          ) : view === 'shifts' ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', maxWidth: '800px', margin: '0 auto' }}>
                {shifts.length === 0 ? (
                  <div className="empty-ticket" style={{ marginTop: '100px' }}>
                    <WarningCircle size={64} className="empty-icon text-muted" weight="regular" />
                    <p>No hay cortes de caja registrados.</p>
                  </div>
                ) : (
                  shifts.map(shift => (
                    <div key={shift.id} style={{ backgroundColor: 'var(--bg-secondary)', borderRadius: 'var(--radius-lg)', border: '1px solid var(--border-color)', padding: '24px' }}>
                       <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '20px' }}>
                          <div>
                             <div style={{ fontWeight: 'bold', fontSize: '18px' }}>Turno: {shift.id}</div>
                             <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                                {formatDate(shift.startTime)} - {shift.endTime ? formatDate(shift.endTime) : 'En curso'}
                             </div>
                          </div>
                          <div style={{ 
                             padding: '8px 16px', borderRadius: 'var(--radius-pill)', fontWeight: 'bold', fontSize: '12px',
                             backgroundColor: (shift.diff || 0) === 0 ? 'rgba(16, 185, 129, 0.1)' : 'rgba(239, 68, 68, 0.1)',
                             color: (shift.diff || 0) === 0 ? 'var(--accent-success)' : 'var(--accent-danger)',
                             border: '1px solid currentColor'
                          }}>
                             {(shift.diff || 0) === 0 ? 'CUADRADO' : `DISCREPANCIA: $${shift.diff?.toFixed(2)}`}
                          </div>
                       </div>

                       <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '16px' }}>
                          <div style={{ backgroundColor: 'var(--bg-primary)', padding: '16px', borderRadius: 'var(--radius-md)' }}>
                             <div style={{ fontSize: '10px', color: 'var(--text-muted)' }}>FONDO INICIAL</div>
                             <div style={{ fontSize: '18px', fontWeight: 'bold' }}>${shift.openingBalance.toFixed(2)}</div>
                          </div>
                          <div style={{ backgroundColor: 'var(--bg-primary)', padding: '16px', borderRadius: 'var(--radius-md)' }}>
                             <div style={{ fontSize: '10px', color: 'var(--text-muted)' }}>VENTAS EFECTIVO</div>
                             <div style={{ fontSize: '18px', fontWeight: 'bold' }}>+${shift.totalCashSales.toFixed(2)}</div>
                          </div>
                          <div style={{ backgroundColor: 'var(--bg-primary)', padding: '16px', borderRadius: 'var(--radius-md)' }}>
                             <div style={{ fontSize: '10px', color: 'var(--text-muted)' }}>ESPERADO TOTAL</div>
                             <div style={{ fontSize: '18px', fontWeight: 'bold', color: 'var(--accent-primary)' }}>${shift.expectedBalance.toFixed(2)}</div>
                          </div>
                          <div style={{ backgroundColor: 'var(--bg-primary)', padding: '16px', borderRadius: 'var(--radius-md)' }}>
                             <div style={{ fontSize: '10px', color: 'var(--text-muted)' }}>REAL EN CAJA</div>
                             <div style={{ fontSize: '18px', fontWeight: 'bold' }}>${shift.actualBalance?.toFixed(2)}</div>
                          </div>
                          <div style={{ backgroundColor: 'var(--bg-primary)', padding: '16px', borderRadius: 'var(--radius-md)' }}>
                             <div style={{ fontSize: '10px', color: 'var(--text-muted)' }}>VENTAS FIADO</div>
                             <div style={{ fontSize: '18px', fontWeight: 'bold', color: 'var(--accent-danger)' }}>${shift.totalCreditSales.toFixed(2)}</div>
                          </div>
                       </div>
                    </div>
                  ))
                )}
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', maxWidth: '800px', margin: '0 auto' }}>
                <h3 style={{ fontSize: '18px', fontWeight: 'bold', marginBottom: '16px' }}>Registro de Asistencias</h3>
                {assistances.length === 0 ? (
                  <div className="empty-ticket" style={{ marginTop: '50px' }}>
                    <WarningCircle size={64} className="empty-icon text-muted" weight="regular" />
                    <p>No hay asistencias registradas.</p>
                  </div>
                ) : (
                  <div style={{ backgroundColor: 'var(--bg-secondary)', borderRadius: 'var(--radius-lg)', border: '1px solid var(--border-color)', overflow: 'hidden' }}>
                    <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '14px' }}>
                      <thead style={{ backgroundColor: 'var(--bg-primary)', color: 'var(--text-secondary)', fontSize: '12px', textTransform: 'uppercase' }}>
                        <tr>
                          <th style={{ padding: '16px', borderBottom: '1px solid var(--border-color)' }}>ID Empleado</th>
                          <th style={{ padding: '16px', borderBottom: '1px solid var(--border-color)' }}>Tipo</th>
                          <th style={{ padding: '16px', borderBottom: '1px solid var(--border-color)' }}>Fecha y Hora</th>
                          <th style={{ padding: '16px', borderBottom: '1px solid var(--border-color)' }}>Evidencia</th>
                        </tr>
                      </thead>
                      <tbody>
                        {assistances.map(a => (
                          <tr key={a.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                            <td style={{ padding: '16px', fontWeight: 'bold' }}>{a.usuario_id}</td>
                            <td style={{ padding: '16px' }}>
                              <span style={{ 
                                padding: '4px 10px', 
                                borderRadius: '12px', 
                                fontSize: '11px', 
                                fontWeight: 'bold',
                                backgroundColor: a.tipo_movimiento === 'ENTRADA' ? 'rgba(16, 185, 129, 0.1)' : 'rgba(239, 68, 68, 0.1)',
                                color: a.tipo_movimiento === 'ENTRADA' ? 'var(--accent-success)' : 'var(--accent-danger)'
                              }}>
                                {a.tipo_movimiento}
                              </span>
                            </td>
                            <td style={{ padding: '16px' }}>{formatDate(a.timestamp)}</td>
                            <td style={{ padding: '16px' }}>
                              {a.ruta_foto_evidencia && a.ruta_foto_evidencia !== 'NO_PHOTO' ? (
                                <a href={a.ruta_foto_evidencia} target="_blank" rel="noreferrer" style={{ display: 'flex', alignItems: 'center', gap: '6px', color: 'var(--accent-primary)', textDecoration: 'none', fontSize: '13px' }}>
                                  <Camera size={18} /> Ver Foto
                                </a>
                              ) : (
                                <span style={{ color: 'var(--text-muted)', fontSize: '13px' }}>Sin foto</span>
                              )}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
            </div>
          )}
        </div>
    </AppShell>
  );
}
