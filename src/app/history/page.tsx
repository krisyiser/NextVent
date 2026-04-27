'use client';

import React, { useState, useEffect, useMemo } from 'react';
import { Sidebar } from '@/components/Sidebar';
import { getSalesHistory, getTopSellingProducts, getShiftHistory } from '@/lib/storage';
import { Sale, Shift } from '@/types';
import { 
  PieChart, Clock, Calendar, ChevronDown, ChevronUp, TrendingUp, 
  DollarSign, ShoppingCart, User, Award, ClipboardCheck, AlertCircle, 
  ArrowDownLeft, ArrowUpRight 
} from 'lucide-react';

export default function History() {
  const [history, setHistory] = useState<Sale[]>([]);
  const [shifts, setShifts] = useState<Shift[]>([]);
  const [view, setView] = useState<'sales' | 'shifts'>('sales');
  const [topSellings, setTopSellings] = useState<any[]>([]);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      setHistory(await getSalesHistory());
      setShifts(await getShiftHistory());
      setTopSellings(await getTopSellingProducts(5));
    };
    load();
  }, []);

  const stats = useMemo(() => {
    return history.reduce((acc, sale) => {
      if (sale.isCancelled) return acc;
      acc.totalSales += sale.total;
      acc.totalProfit += sale.profit;
      acc.totalCost += sale.totalCost;
      if (sale.isCredit) acc.totalCredits += sale.total;
      return acc;
    }, { totalSales: 0, totalProfit: 0, totalCost: 0, totalCredits: 0 });
  }, [history]);

  const handleCancelSale = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    if (confirm("¿Estás seguro de cancelar esta venta? Esto devolverá los productos al stock y anulará la transacción.")) {
        const libs = await import('@/lib/storage');
        await libs.cancelSale(id);
        setHistory(await libs.getSalesHistory());
        setTopSellings(await libs.getTopSellingProducts(5));
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
    <>
      <Sidebar activeModule="history" />
      <main className="main-content" style={{ display: 'flex', flexDirection: 'column' }}>
        <header className="top-bar">
          <div className="ticket-title" style={{ fontSize: '24px', display: 'flex', alignItems: 'center', gap: '12px' }}>
            <ClipboardCheck size={28} className="text-accent" />
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
          </div>
        </header>

        <div className="history-container" style={{ padding: '24px', overflowY: 'auto', flex: 1 }}>
          {view === 'sales' ? (
            <>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '16px', maxWidth: '1000px', margin: '0 auto 32px' }}>
                <div style={{ backgroundColor: 'var(--bg-secondary)', padding: '20px', borderRadius: 'var(--radius-lg)', border: '1px solid var(--border-color)', display: 'flex', alignItems: 'center', gap: '16px' }}>
                    <div style={{ padding: '12px', backgroundColor: 'rgba(59, 130, 246, 0.1)', color: 'var(--accent-primary)', borderRadius: 'var(--radius-md)' }}><DollarSign size={24} /></div>
                    <div>
                      <div style={{ color: 'var(--text-secondary)', fontSize: '12px' }}>VENTAS NO ANULADAS</div>
                      <div style={{ fontSize: '20px', fontWeight: 'bold' }}>${stats.totalSales.toFixed(2)}</div>
                    </div>
                </div>
                <div style={{ backgroundColor: 'var(--bg-secondary)', padding: '20px', borderRadius: 'var(--radius-lg)', border: '1px solid var(--border-color)', display: 'flex', alignItems: 'center', gap: '16px' }}>
                    <div style={{ padding: '12px', backgroundColor: 'rgba(16, 185, 129, 0.1)', color: 'var(--accent-success)', borderRadius: 'var(--radius-md)' }}><TrendingUp size={24} /></div>
                    <div>
                      <div style={{ color: 'var(--text-secondary)', fontSize: '12px' }}>UTILIDAD NETA FINAL</div>
                      <div style={{ fontSize: '20px', fontWeight: 'bold', color: 'var(--accent-success)' }}>${stats.totalProfit.toFixed(2)}</div>
                    </div>
                </div>
                <div style={{ backgroundColor: 'var(--bg-secondary)', padding: '20px', borderRadius: 'var(--radius-lg)', border: '1px solid var(--border-color)', display: 'flex', alignItems: 'center', gap: '16px' }}>
                    <div style={{ padding: '12px', backgroundColor: 'rgba(239, 68, 68, 0.1)', color: 'var(--accent-danger)', borderRadius: 'var(--radius-md)' }}><User size={24} /></div>
                    <div>
                      <div style={{ color: 'var(--text-secondary)', fontSize: '12px' }}>DEUDAS POR COBRAR</div>
                      <div style={{ fontSize: '20px', fontWeight: 'bold', color: 'var(--accent-danger)' }}>${stats.totalCredits.toFixed(2)}</div>
                    </div>
                </div>
              </div>

              <div style={{ maxWidth: '1000px', margin: '0 auto 32px', display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '32px' }}>
                <div style={{ backgroundColor: 'var(--bg-secondary)', borderRadius: 'var(--radius-lg)', padding: '24px', border: '1px solid var(--border-color)' }}>
                  <h3 style={{ fontSize: '18px', fontWeight: 'bold', marginBottom: '20px', display: 'flex', alignItems: 'center', gap: '10px' }}>
                    <Award size={20} className="text-warning" />
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
                    <TrendingUp size={48} style={{ opacity: 0.1, marginBottom: '16px' }} />
                    <p style={{ fontSize: '13px' }}>Análisis de tendencias disponible al acumular más de 100 ventas.</p>
                </div>
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', maxWidth: '800px', margin: '0 auto' }}>
                {history.length === 0 ? (
                  <div className="empty-ticket" style={{ marginTop: '100px' }}>
                    <PieChart size={64} className="empty-icon text-muted" />
                    <p>No hay ventas registradas todavía.</p>
                  </div>
                ) : (
                  history.map(sale => (
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
                            <Calendar size={14} />
                            {formatDate(sale.date)}
                            {sale.isCancelled && <span style={{ marginLeft: '12px' }}>({formatDate(sale.cancelledAt!)})</span>}
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
                            {expandedId === sale.id ? <ChevronUp size={20} /> : <ChevronDown size={20} />}
                          </button>
                        </div>
                      </div>

                      {expandedId === sale.id && (
                        <div style={{ marginTop: '20px', paddingTop: '20px', borderTop: '1px dashed var(--border-color)' }}>
                            {!sale.isCancelled && (
                                <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: '16px' }}>
                                    <button className="checkout-btn" 
                                            style={{ backgroundColor: 'transparent', border: '1px solid var(--accent-danger)', color: 'var(--accent-danger)', fontSize: '12px', padding: '8px 16px', margin: 0, width: 'auto' }}
                                            onClick={(e) => handleCancelSale(sale.id, e)}
                                    >
                                        ANULAR ESTA VENTA
                                    </button>
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
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', maxWidth: '800px', margin: '0 auto' }}>
                {shifts.length === 0 ? (
                  <div className="empty-ticket" style={{ marginTop: '100px' }}>
                    <AlertCircle size={64} className="empty-icon text-muted" />
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
          )}
        </div>
      </main>
    </>
  );
}
