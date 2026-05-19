'use client';

import React, { useState, useMemo, useEffect } from 'react';
import { Sidebar } from '@/components/Sidebar';
import { TopBar } from '@/components/TopBar';
import { ProductCard } from '@/components/ProductCard';
import { TicketSection } from '@/components/TicketSection';
import { CheckoutModal } from '@/components/CheckoutModal';
import { DiscountModal } from '@/components/DiscountModal';
import { QuantityModal } from '@/components/QuantityModal';
import { ShiftModal } from '@/components/ShiftModal';
import { CATEGORIES } from '@/constants';
import { Product, TicketItem, Sale, Shift } from '@/types';
import { getProducts, saveSale, getActiveShift, getPromotions } from '@/lib/storage';

import { ReceiptTemplate } from '@/components/ReceiptTemplate';

export type TopBarProps = {
  searchQuery: string;
  setSearchQuery: (query: string) => void;
  onScan: (barcode: string) => void;
  isMobileMode: boolean;
  setIsMobileMode: (mode: boolean) => void;
  isScannerDetected: boolean;
};



export default function POS() {
  const [activeCategory, setActiveCategory] = useState('Todos');
  const [searchQuery, setSearchQuery] = useState('');
  const [ticket, setTicket] = useState<TicketItem[]>([]);
  const [isMobileMode, setIsMobileMode] = useState(false);
  const [products, setProducts] = useState<Product[]>([]);
  const [scanBuffer, setScanBuffer] = useState('');
  const [isScannerDetected, setIsScannerDetected] = useState(false);
  const [currentShift, setCurrentShift] = useState<Shift | null>(null);
  const [lastSale, setLastSale] = useState<Sale | null>(null);
  const [globalDiscount, setGlobalDiscount] = useState(0);
  const [isKioskMode, setIsKioskMode] = useState(false);
  const [promotions, setPromotions] = useState<any[]>([]);
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

  // Parked Orders state
  const [parkedOrders, setParkedOrders] = useState<{ id: string, items: TicketItem[], timestamp: string }[]>([]);

  useEffect(() => {
    const saved = localStorage.getItem('parked_orders');
    if (saved) {
      setParkedOrders(JSON.parse(saved));
    }
  }, []);

  const parkCurrentSale = () => {
    if (ticket.length === 0) return;
    const newOrder = {
      id: `ORDER-${Date.now()}`,
      items: [...ticket],
      timestamp: new Date().toLocaleTimeString()
    };
    const updated = [newOrder, ...parkedOrders];
    setParkedOrders(updated);
    localStorage.setItem('parked_orders', JSON.stringify(updated));
    setTicket([]);
  };

  const resumeOrder = (orderId: string) => {
    const order = parkedOrders.find(o => o.id === orderId);
    if (order) {
      if (ticket.length > 0) {
        // Option 1: Merge. Option 2: Park current then resume.
        // Let's Park current if not empty
        parkCurrentSale();
      }
      setTicket(order.items);
      const updated = parkedOrders.filter(o => o.id !== orderId);
      setParkedOrders(updated);
      localStorage.setItem('parked_orders', JSON.stringify(updated));
    }
  };
  
  // Persistence Loading
  useEffect(() => {
    const load = async () => {
        setProducts(await getProducts());
        setPromotions(await getPromotions());
        const shift = await getActiveShift();
        if (!shift) {
            setIsShiftOpen(true);
        } else {
            setCurrentShift(shift);
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
          setIsScannerDetected(true);
      }

      // If user is typing manually in another input, ignore global scan
      // UNLESS it's a fast burst (scanner input)
      if (!isFastBurst && diff >= 25 && (document.activeElement?.tagName === 'INPUT' || document.activeElement?.tagName === 'SELECT')) {
          return;
      }

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

  const handleProductSelect = (product: Product) => {
    const existing = ticket.find(item => item.id === product.id);
    const currentQty = existing ? existing.quantity : 0;

    if (currentQty >= product.stock || product.stock <= 0) {
      playBeep('error');
      alert(`No puedes agregar más. Stock agotado para ${product.name} (${product.stock} ${product.unit} disponibles).`);
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
  };


  const addToTicket = (product: Product, quantity: number) => {
    setTicket(prev => {
      const existing = prev.find(item => item.id === product.id);
      const currentQty = existing ? existing.quantity : 0;
      
      if (currentQty + quantity > product.stock) {
        // Silently return if already at limit to avoid multiple alerts from scanner
        if (currentQty < product.stock) {
            playBeep('error');
            alert(`Stock agotado. No puedes agregar más de ${product.stock} ${product.unit} de este producto.`);
        }
        return prev;
      }


      setSearchQuery(''); 
      setLastAddedId(product.id);
      setTimeout(() => setLastAddedId(null), 1000);

      if (existing) {
        return prev.map(item => item.id === product.id ? { ...item, quantity: item.quantity + quantity } : item);
      }
      return [...prev, { ...product, quantity }];
    });
  };

  const updateQuantity = (id: string, delta: number) => {
    setTicket(prev => prev.map(item => {
      if (item.id === id) {
        const newQty = Math.max(0, item.quantity + delta);
        return { ...item, quantity: newQty };
      }
      return item;
    }).filter(item => item.quantity > 0));
  };

  const clearTicket = () => setTicket([]);

  const handleApplyDiscount = (val: number) => {
    setGlobalDiscount(val);
  };

  const onCompleteSale = async (sale: Sale) => {
    await saveSale(sale);
    setProducts(await getProducts());
    setLastSale(sale);
    clearTicket();
    setIsCheckoutOpen(false);
    
    // Trigger print after modal closes and lastSale renders
    setTimeout(() => {
        window.print();
    }, 500);

    alert(`¡Venta #${sale.id.split('-')[1]} exitosa!\nCambio para cliente: $${sale.changeAmount.toFixed(2)}\n\n✅ Venta procesada y stock actualizado correctamente.`);
  };

  return (
    <div className={isKioskMode ? 'kiosk-mode' : ''} style={{ display: 'flex', width: '100%', height: '100vh' }}>
      <Sidebar activeModule="pos" />

      <main className="main-content">
        <TopBar 
          searchQuery={searchQuery}
          setSearchQuery={setSearchQuery}
          onScan={handleManualScan}
          isMobileMode={isMobileMode}
          setIsMobileMode={setIsMobileMode}
          isScannerDetected={isScannerDetected}
          toggleKiosk={() => setIsKioskMode(!isKioskMode)}
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
            updateQuantity={updateQuantity}
            onCompleteSale={() => setIsCheckoutOpen(true)}
            onClearTicket={() => { clearTicket(); setGlobalDiscount(0); }}
            parkedOrders={parkedOrders}
            onParkSale={parkCurrentSale}
            onResumeOrder={resumeOrder}
            globalDiscount={globalDiscount}
            onOpenDiscount={() => setIsDiscountModalOpen(true)}
            lastAddedId={lastAddedId}
            alerts={ticketData.alerts}
          />

        </div>
      </main>

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

      {lastSale && <ReceiptTemplate sale={lastSale} />}
    </div>
  );
}
