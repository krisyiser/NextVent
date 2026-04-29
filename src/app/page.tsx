'use client';

import React, { useState, useMemo, useEffect } from 'react';
import { Sidebar } from '@/components/Sidebar';
import { TopBar } from '@/components/TopBar';
import { ProductCard } from '@/components/ProductCard';
import { TicketSection } from '@/components/TicketSection';
import { CheckoutModal } from '@/components/CheckoutModal';
import { QuantityModal } from '@/components/QuantityModal';
import { ShiftModal } from '@/components/ShiftModal';
import { CATEGORIES } from '@/constants';
import { Product, TicketItem, Sale, Shift } from '@/types';
import { getProducts, saveSale, getActiveShift } from '@/lib/storage';

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


  // Modals state
  const [isCheckoutOpen, setIsCheckoutOpen] = useState(false);
  const [isQuantityOpen, setIsQuantityOpen] = useState(false);
  const [isShiftOpen, setIsShiftOpen] = useState(false);
  const [pendingProduct, setPendingProduct] = useState<Product | null>(null);
  
  // Persistence Loading
  useEffect(() => {
    const load = async () => {
        setProducts(await getProducts());
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

    const items = ticket.map(item => {
      let priceToUse = item.price;
      if (item.wholesalePrice && item.wholesaleThreshold && item.quantity >= item.wholesaleThreshold) {
        priceToUse = item.wholesalePrice;
      }
      
      total += priceToUse * item.quantity;
      totalCost += item.cost * item.quantity;
      count += item.unit === 'Kg' ? 1 : item.quantity;

      return { ...item, priceToUse };
    });

    return { total, totalCost, count, items };
  }, [ticket]);

  const handleProductSelect = (product: Product) => {
    const existing = ticket.find(item => item.id === product.id);
    const currentQty = existing ? existing.quantity : 0;

    if (currentQty >= product.stock || product.stock <= 0) {
      alert(`No puedes agregar más. Stock agotado para ${product.name} (${product.stock} ${product.unit} disponibles).`);
      return;
    }

    if (product.unit === 'Kg' || (product.category === 'Abarrotes' && product.unit !== 'Pza')) {
      setPendingProduct(product);
      setIsQuantityOpen(true);
    } else {
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
            alert(`Stock agotado. No puedes agregar más de ${product.stock} ${product.unit} de este producto.`);
        }
        return prev;
      }


      setSearchQuery(''); 
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

  const onCompleteSale = async (sale: Sale) => {
    await saveSale(sale);
    setProducts(await getProducts());
    clearTicket();
    setIsCheckoutOpen(false);
    alert(`¡Venta #${sale.id} exitosa!\nCambio para cliente: $${sale.changeAmount.toFixed(2)}\n\n✅ Venta procesada y stock actualizado correctamente.`);
  };

  return (
    <>
      <Sidebar activeModule="pos" />

      <main className="main-content">
        <TopBar 
          searchQuery={searchQuery}
          setSearchQuery={setSearchQuery}
          onScan={handleManualScan}
          isMobileMode={isMobileMode}
          setIsMobileMode={setIsMobileMode}
          isScannerDetected={isScannerDetected}
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
            onClearTicket={clearTicket}
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
    </>
  );
}
