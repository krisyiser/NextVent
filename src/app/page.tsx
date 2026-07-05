'use client';

import React, { useState, useMemo, useEffect, useCallback } from 'react';
import { AppShell } from '@/components/ui';
import { TopBar } from '@/components/TopBar';
import { ProductCard } from '@/components/ProductCard';
import { TicketSection } from '@/components/TicketSection';
import { CheckoutModal } from '@/components/CheckoutModal';
import { DiscountModal } from '@/components/DiscountModal';
import { QuantityModal } from '@/components/QuantityModal';
import { ShiftModal } from '@/components/ShiftModal';
import { CATEGORIES } from '@/constants';
import { Product, TicketItem, Sale, Shift } from '@/types';
import { getProducts, saveSale, getActiveShift, getPromotions, getParkedOrders, saveParkedOrder, deleteParkedOrder, getComboRecommendation, type Promotion } from '@/lib/storage';
import { ReceiptTemplate } from '@/components/ReceiptTemplate';
import { toast } from 'sonner';
import { invoke } from '@tauri-apps/api/core';
import { useAuth } from '@/auth/AuthProvider';
import { ManagerOverrideModal } from '@/components/ManagerOverrideModal';

export default function POS() {
  const [activeCategory, setActiveCategory] = useState('Todos');
  const [searchQuery, setSearchQuery] = useState('');
  const [ticket, setTicket] = useState<TicketItem[]>([]);
  const [total, setTotal] = useState(0);
  const [totalCost, setTotalCost] = useState(0);

  // Broadcast cart for second screen
  useEffect(() => {
      const broadcast = async () => {
          try {
              await invoke('broadcast_cart', { cartJson: JSON.stringify(ticket) });
          } catch (e) {
              // Ignore if not running in Tauri
          }
      };
      broadcast();
  }, [ticket]);
  const [isMobileMode, setIsMobileMode] = useState(false);
  const [products, setProducts] = useState<Product[]>([]);
  const [scanBuffer, setScanBuffer] = useState('');
  const [isScannerDetected, setIsScannerDetected] = useState(false);
  const [currentShift, setCurrentShift] = useState<Shift | null>(null);
  const [lastSale, setLastSale] = useState<Sale | null>(null);
  const [globalDiscount, setGlobalDiscount] = useState(0);
  const [isKioskMode, setIsKioskMode] = useState(false);
  
  const { auth } = useAuth();
  const role = auth.role || 'CAJERO';

  const [isOverrideModalOpen, setIsOverrideModalOpen] = useState(false);
  const [overrideAction, setOverrideAction] = useState<{name: string, callback: () => void} | null>(null);

  const handleRequireOverride = (actionName: string, callback: () => void) => {
      if (role === 'CAJERO') {
          setOverrideAction({ name: actionName, callback });
          setIsOverrideModalOpen(true);
      } else {
          callback();
      }
  };

  const toggleKiosk = useCallback(async () => {
    try {
      const { getCurrentWindow } = await import('@tauri-apps/api/window');
      const appWindow = getCurrentWindow();
      const isFS = await appWindow.isFullscreen();
      await appWindow.setFullscreen(!isFS);
      setIsKioskMode(!isFS);
    } catch (e) {
      console.warn("Tauri fullscreen not available, using fallback", e);
      setIsKioskMode(prev => !prev);
    }
  }, []);

  const [promotions, setPromotions] = useState<Promotion[]>([]);
  const [lastAddedId, setLastAddedId] = useState<string | null>(null);

  const playBeep = (type: 'success' | 'error' = 'success') => {
    try {
        const audioCtx = new (window.AudioContext || (window as any).webkitAudioContext)();
        const oscillator = audioCtx.createOscillator();
        const gainNode = audioCtx.createGain();

        oscillator.connect(gainNode);
        gainNode.connect(audioCtx.destination);

        oscillator.type = 'sine';
        oscillator.frequency.setValueAtTime(type === 'success' ? 880 : 220, audioCtx.currentTime);
        gainNode.gain.setValueAtTime(0.1, audioCtx.currentTime);
        gainNode.gain.exponentialRampToValueAtTime(0.01, audioCtx.currentTime + 0.1);

        oscillator.start();
        oscillator.stop(audioCtx.currentTime + 0.1);
    } catch (e) {
        console.error("Audio feedback failed", e);
    }
  };


  // Modals state
  const [isCheckoutOpen, setIsCheckoutOpen] = useState(false);
  const [isDiscountModalOpen, setIsDiscountModalOpen] = useState(false);
  const [isQuantityOpen, setIsQuantityOpen] = useState(false);
  const [isShiftOpen, setIsShiftOpen] = useState(false);
  const [pendingProduct, setPendingProduct] = useState<Product | null>(null);

  // Parked Orders state (SQLite-backed)
  const [parkedOrders, setParkedOrders] = useState<{ id: string, items: TicketItem[], timestamp: string }[]>([]);

  useEffect(() => {
    (async () => {
      try {
        const orders = await getParkedOrders();
        setParkedOrders(orders);
      } catch (_) { /* DB not ready */ }
    })();
  }, []);

  const parkCurrentSale = async () => {
    if (ticket.length === 0) return;
    const newOrder = {
      id: `ORDER-${Date.now()}`,
      items: [...ticket],
      timestamp: new Date().toLocaleTimeString()
    };
    const updated = [newOrder, ...parkedOrders];
    setParkedOrders(updated);
    await saveParkedOrder(newOrder);
    setTicket([]);
    toast.info('Venta pausada');
  };

  const resumeOrder = async (orderId: string) => {
    const order = parkedOrders.find(o => o.id === orderId);
    if (order) {
      if (ticket.length > 0) {
        await parkCurrentSale();
      }
      setTicket(order.items);
      const updated = parkedOrders.filter(o => o.id !== orderId);
      setParkedOrders(updated);
      await deleteParkedOrder(orderId);
      toast.info('Venta reanudada');
    }
  };
  
  // Persistence Loading
  useEffect(() => {
    const load = async () => {
        try {
            const prods = await getProducts();
            setProducts(prods);
        } catch (err: any) {
            toast.error(`Error cargando productos: ${err.message || err}`);
            console.error(err);
        }
        try {
            setPromotions(await getPromotions());
        } catch (err: any) {
            toast.error(`Error cargando promociones: ${err.message || err}`);
        }
        try {
            const shift = await getActiveShift();
            if (!shift) {
                setIsShiftOpen(true);
            } else {
                setCurrentShift(shift);
            }
        } catch (err: any) {
            toast.error(`Error cargando turno: ${err.message || err}`);
        }
    };
    load();
  }, []);

  // Global Scanner Listener (Advanced Detection)
  useEffect(() => {
    let timeout: any;
    let lastKeyTime = Date.now();
    let currentBuffer = ''; // Use a local variable for immediate processing
    
    const handleKeyDown = (e: KeyboardEvent) => {
      if (isCheckoutOpen || isQuantityOpen) return;
      
      const currentTime = Date.now();
      const diff = currentTime - lastKeyTime;
      lastKeyTime = currentTime;

      // Detect scanner (extremely fast burst of keys)
      const isFastBurst = diff < 25 && diff > 0;
      if (isFastBurst) {
          setIsScannerDetected(prev => prev ? prev : true);
      }

      // If user is typing manually in another input, ignore global scan
      // UNLESS it's a fast burst (scanner input)
      if (!isFastBurst && diff >= 25 && (document.activeElement?.tagName === 'INPUT' || document.activeElement?.tagName === 'SELECT')) {
          return;
      }

      // Zero-mouse shortcuts
      if (e.key === 'F1') { e.preventDefault(); document.getElementById('pos-search-input')?.focus(); return; }
      if (e.key === 'F11') { e.preventDefault(); toggleKiosk(); return; }
      if (e.key === 'F5') { e.preventDefault(); if (ticket.length > 0) setIsCheckoutOpen(true); return; }
      if (e.key === '+') { e.preventDefault(); if (lastAddedId) updateQuantityRequest(lastAddedId, 1); return; }
      if (e.key === '-') { e.preventDefault(); if (lastAddedId) updateQuantityRequest(lastAddedId, -1); return; }

      if (e.key === 'Enter') {
        if (currentBuffer.length >= 3) {
          const product = products.find(p => p.barcode === currentBuffer || p.id === currentBuffer);
          if (product) {
            handleProductSelect(product);
          }
          currentBuffer = '';
          setScanBuffer('');
        }
      } else if (e.key.length === 1) {
        currentBuffer += e.key;
        setScanBuffer(currentBuffer);
        
        clearTimeout(timeout);
        timeout = setTimeout(() => {
            // If there's a pause after a long string, try to process it even without Enter
            if (currentBuffer.length >= 8) {
                const product = products.find(p => p.barcode === currentBuffer || p.id === currentBuffer);
                if (product) {
                    handleProductSelect(product);
                    currentBuffer = '';
                    setScanBuffer('');
                }
            }
            // Auto-clear buffer if no activity
            if (currentBuffer.length > 0) {
                 // Don't clear immediately if it's a slow scan, but industrial ones are done by now
                 setTimeout(() => {
                    currentBuffer = '';
                    setScanBuffer('');
                 }, 100);
            }
        }, 40); 
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [products, isCheckoutOpen, isQuantityOpen]);



  const handleManualScan = (barcode: string) => {
    const product = products.find(p => p.barcode === barcode || p.id === barcode);
    if (product) {
        handleProductSelect(product);
    } else {
        // Maybe visual feedback for not found?
    }
  };


  // Filter products based on category and search
  const filteredProducts = useMemo(() => {
    return products.filter(p => {
      const matchCategory = activeCategory === 'Todos' || p.category === activeCategory;
      const matchSearch = p.name.toLowerCase().includes(searchQuery.toLowerCase());
      return matchCategory && matchSearch;
    });
  }, [activeCategory, searchQuery, products]);

  // Calculate totals including Wholesale logic
  const ticketData = useMemo(() => {
    let total = 0;
    let totalCost = 0;
    let count = 0;

    const alerts: { type: 'info' | 'success', message: string }[] = [];

    const items = ticket.map(item => {
      let priceToUse = item.price;
      let multiBuyDiscount = 0;
      
      // 1. Check for Active Promotions
      const activePromos = promotions.filter(p => p.isActive);
      
      // Specific product promo (Regular or Multi-buy)
      const productPromo = activePromos.find(p => (p.type === 'product' || p.type === 'multibuy') && p.targetId === item.id);
      // Category promo
      const categoryPromo = activePromos.find(p => p.type === 'category' && p.targetId === item.category);

      if (productPromo) {
        if (productPromo.type === 'multibuy') {
          const sets = Math.floor(item.quantity / productPromo.buyQty);
          if (sets > 0) {
            multiBuyDiscount = (item.price * (productPromo.buyQty - productPromo.payQty)) * sets;
            alerts.push({ type: 'success', message: `¡Promoción ${productPromo.buyQty}x${productPromo.payQty} aplicada en ${item.name}!` });
          } else if (item.quantity === productPromo.buyQty - 1) {
            alerts.push({ type: 'info', message: `¡Agrega 1 ${item.name} más para aplicar la promo ${productPromo.buyQty}x${productPromo.payQty}!` });
          }
        } else {
          if (productPromo.discountType === 'percent') {
            priceToUse = item.price * (1 - productPromo.discountValue / 100);
          } else {
            priceToUse = Math.max(0, item.price - productPromo.discountValue);
          }
          alerts.push({ type: 'success', message: `Descuento aplicado en ${item.name}` });
        }
      } else if (categoryPromo) {
        if (categoryPromo.discountType === 'percent') {
          priceToUse = item.price * (1 - categoryPromo.discountValue / 100);
        } else {
          priceToUse = Math.max(0, item.price - categoryPromo.discountValue);
        }
      } else if (item.wholesalePrice && item.wholesaleThreshold && item.quantity >= item.wholesaleThreshold) {
        priceToUse = item.wholesalePrice;
      }
      
      total += (priceToUse * item.quantity) - multiBuyDiscount;
      totalCost += item.cost * item.quantity;
      count += item.unit === 'Kg' ? 1 : item.quantity;

      return { ...item, priceToUse, multiBuyDiscount };
    });

    const finalTotal = Math.max(0, total - globalDiscount);

    return { total: finalTotal, rawTotal: total, totalCost, count, items, alerts };
  }, [ticket, globalDiscount, promotions]);

  const checkCombos = useCallback(async (productId: string) => {
    try {
      const rec = await getComboRecommendation(productId);
      if (rec && rec.frequency >= 5) {
        const recProduct = products.find(p => p.id === rec.recommendedId);
        if (recProduct) {
          toast(`💡 Combo detectado: Recomienda ofrecer ${recProduct.name}`, { duration: 4000 });
        }
      }
    } catch (e) {
      // ignore
    }
  }, [products]);

  const addToTicket = useCallback((product: Product, quantity: number) => {
    setTicket(prev => {
      const existing = prev.find(item => item.id === product.id);
      const currentQty = existing ? existing.quantity : 0;
      
      if (currentQty + quantity > product.stock) {
        // Silently return if already at limit to avoid multiple alerts from scanner
        if (currentQty < product.stock) {
            playBeep('error');
            toast.error(`Stock agotado. Máximo ${product.stock} ${product.unit} disponibles.`);
        }
        return prev;
      }


      setSearchQuery(''); 
      setLastAddedId(product.id);
      setTimeout(() => setLastAddedId(null), 2000); // give 2 seconds for +/- shortcuts
      checkCombos(product.id);

      if (existing) {
        return prev.map(item => item.id === product.id ? { ...item, quantity: item.quantity + quantity } : item);
      }
      return [...prev, { ...product, quantity }];
    });
  }, [checkCombos]);

  const handleProductSelect = useCallback((product: Product) => {
    const existing = ticket.find(item => item.id === product.id);
    const currentQty = existing ? existing.quantity : 0;

    if (currentQty >= product.stock || product.stock <= 0) {
      playBeep('error');
      toast.error(`Stock agotado para ${product.name} (${product.stock} ${product.unit} disponibles)`);
      return;
    }

    if (product.unit === 'Kg' || (product.category === 'Abarrotes' && product.unit !== 'Pza')) {
      setPendingProduct(product);
      setIsQuantityOpen(true);
    } else {
      playBeep('success');
      addToTicket(product, 1);
    }
    setSearchQuery('');
  }, [ticket, addToTicket]);

  const updateQuantityRequest = (id: string, delta: number) => {
    if (delta < 0 && role === 'CAJERO') {
      const item = ticket.find(i => i.id === id);
      if (item) {
        handleRequireOverride(`Eliminar 1 ${item.name}`, () => updateQuantityCore(id, delta));
        return;
      }
    }
    updateQuantityCore(id, delta);
  };

  const updateQuantityCore = (id: string, delta: number) => {
    setTicket(prev => prev.map(item => {
      if (item.id === id) {
        const newQty = Math.max(0, item.quantity + delta);
        return { ...item, quantity: newQty };
      }
      return item;
    }).filter(item => item.quantity > 0));
  };

  const clearTicketRequest = () => {
    if (role === 'CAJERO') {
      handleRequireOverride('Cancelar Ticket Completo', clearTicketCore);
      return;
    }
    clearTicketCore();
  };

  const clearTicketCore = () => setTicket([]);

  const handleApplyDiscount = (val: number) => {
    setGlobalDiscount(val);
  };

  const onCompleteSale = async (sale: Sale) => {
    await saveSale(sale);
    setProducts(await getProducts());
    setLastSale(sale);
    clearTicketCore();
    setIsCheckoutOpen(false);
    
    // Trigger print after modal closes and lastSale renders
    setTimeout(() => {
        // window.print(); // Disabled to allow the WA toast action
    }, 500);

    const ticketText = `*TICKET DE COMPRA*\nNextVent POS\n\n` + 
                       sale.items.map(i => `${i.quantity}x ${i.name} - $${i.total.toFixed(2)}`).join('\n') +
                       `\n\n*Total: $${sale.total.toFixed(2)}*\nGracias por su compra.`;

    toast.success(`¡Venta #${sale.id.split('-')[1]} exitosa! Cambio: $${sale.changeAmount.toFixed(2)}`, {
      duration: 10000,
      action: {
        label: '📱 Enviar WhatsApp',
        onClick: () => {
          // Si es fiado y tiene cliente, se asume que podemos buscar su teléfono, pero por ahora pedimos el número o abre el chat genérico.
          // Para simplificar, abre el chat sin número para que el usuario elija el contacto, o usa Tauri IPC
          invoke('set_whatsapp_layout', { open: true, width: 450.0, pinned: true });
          invoke('send_whatsapp_message', { phone: '', text: ticketText });
        }
      }
    });
  };

  return (
    <>
      <AppShell activeModule="pos">
        <TopBar 
          searchQuery={searchQuery}
          setSearchQuery={setSearchQuery}
          onScan={handleManualScan}
          isMobileMode={isMobileMode}
          setIsMobileMode={setIsMobileMode}
          isScannerDetected={isScannerDetected}
          toggleKiosk={toggleKiosk}
          isKioskMode={isKioskMode}
        />



        <div className="pos-layout">
          {/* Products Grid Area */}
          <section className="products-section">
            <div className="categories">
              {CATEGORIES.map(cat => (
                <button
                  key={cat}
                  className={`category-chip ${activeCategory === cat ? 'active' : ''}`}
                  onClick={() => setActiveCategory(cat)}
                >
                  {cat}
                </button>
              ))}
            </div>

            <div className="products-grid">
              {filteredProducts.map(product => (
                <ProductCard 
                  key={product.id}
                  product={product}
                  onAdd={handleProductSelect}
                />
              ))}
            </div>
          </section>

          <TicketSection 
            ticket={ticketData.items}
            ticketTotal={ticketData.total}
            ticketCount={ticketData.count}
            updateQuantity={updateQuantityRequest}
            onCompleteSale={() => setIsCheckoutOpen(true)}
            onClearTicket={clearTicketRequest}
            parkedOrders={parkedOrders}
            onParkSale={parkCurrentSale}
            onResumeOrder={resumeOrder}
            globalDiscount={globalDiscount}
            onOpenDiscount={() => setIsDiscountModalOpen(true)}
            lastAddedId={lastAddedId}
            alerts={ticketData.alerts}
          />

        </div>
      </AppShell>
      <CheckoutModal 
        isOpen={isCheckoutOpen}
        onClose={() => setIsCheckoutOpen(false)}
        total={ticketData.total}
        totalCost={ticketData.totalCost}
        items={ticket}
        onComplete={onCompleteSale}
      />

      <DiscountModal 
        isOpen={isDiscountModalOpen}
        onClose={() => setIsDiscountModalOpen(false)}
        currentDiscount={globalDiscount}
        subtotal={ticketData.rawTotal}
        onApply={setGlobalDiscount}
      />

      <QuantityModal 
        isOpen={isQuantityOpen}
        onClose={() => setIsQuantityOpen(false)}
        product={pendingProduct}
        onConfirm={(q) => addToTicket(pendingProduct!, q)}
      />

      <ShiftModal 
        isOpen={isShiftOpen}
        onOpened={(shift) => {
            setCurrentShift(shift);
            setIsShiftOpen(false);
        }}
      />

      {isOverrideModalOpen && overrideAction && (
        <ManagerOverrideModal 
          isOpen={isOverrideModalOpen} 
          actionName={overrideAction.name}
          onCancel={() => { setIsOverrideModalOpen(false); setOverrideAction(null); }}
          onSuccess={() => { setIsOverrideModalOpen(false); overrideAction.callback(); setOverrideAction(null); }}
        />
      )}

      {lastSale && <ReceiptTemplate sale={lastSale} />}
    </>
  );
}
