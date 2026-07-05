'use client';

import React, { useState, useEffect } from 'react';
import { AppShell } from '@/components/ui';
import { ProductModal } from '@/components/ProductModal';
import { getProducts, updateProduct, addProduct, deleteProduct, getPredictiveRestockList, clearInventory, saveProductsBulk } from '@/lib/storage';
import { Product } from '@/types';
import { Plus, PencilSimple, Trash, Info, UploadSimple, TrendUp, X, Package } from 'phosphor-react';
import { toast } from 'sonner';


export default function Inventory() {
  const [products, setProducts] = useState<Product[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  
  const [showRestock, setShowRestock] = useState(false);
  const [restockList, setRestockList] = useState<any[]>([]);

  const fileInputRef = React.useRef<HTMLInputElement>(null);
  const csvInputRef = React.useRef<HTMLInputElement>(null);


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

  const handleOpenRestock = async () => {
     const list = await getPredictiveRestockList();
     setRestockList(list);
     setShowRestock(true);
  };

  const handleFileUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;
    try {
        const text = await file.text();
        const parser = new DOMParser();
        const xmlDoc = parser.parseFromString(text, "text/xml");
        const conceptos = xmlDoc.getElementsByTagName('cfdi:Concepto');
        let imported = 0;
        
        for (let i = 0; i < conceptos.length; i++) {
            const node = conceptos[i];
            const noIdentificacion = node.getAttribute('NoIdentificacion');
            const cantidad = parseFloat(node.getAttribute('Cantidad') || '0');
            const descripcion = node.getAttribute('Descripcion') || 'Producto Importado';
            const valorUnitario = parseFloat(node.getAttribute('ValorUnitario') || '0');
            
            const existing = products.find(p => p.barcode === noIdentificacion || p.name === descripcion);
            if (existing) {
                await updateProduct({ ...existing, stock: existing.stock + cantidad, cost: valorUnitario });
            } else {
                await addProduct({
                    id: `PROD-${Date.now()}-${i}`,
                    name: descripcion,
                    barcode: noIdentificacion || undefined,
                    cost: valorUnitario,
                    price: valorUnitario * 1.3, // default 30% margin
                    stock: cantidad,
                    category: 'Importados XML',
                    unit: 'Pza'
                });
            }
            imported++;
        }
        if (imported > 0) {
            toast.success(`Se importaron/actualizaron ${imported} productos desde la factura XML.`);
            setProducts(await getProducts());
        } else {
            toast.info('No se encontraron conceptos (cfdi:Concepto) en el XML.');
        }
    } catch (e) {
        toast.error('Error al procesar el archivo XML.');
    }
    // reset input
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const handleCsvUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;
    try {
        const text = await file.text();
        
        // Strip UTF-8 Byte Order Mark (BOM) if present
        let cleanedText = text;
        if (text.startsWith('\uFEFF')) {
            cleanedText = text.substring(1);
        }

        // Dynamically detect separator (, ; \t or |) by counting occurrences in the header line
        const firstNewLineIdx = cleanedText.indexOf('\n');
        const firstLine = firstNewLineIdx !== -1 ? cleanedText.substring(0, firstNewLineIdx) : cleanedText;
        const commaCount = (firstLine.match(/,/g) || []).length;
        const semicolonCount = (firstLine.match(/;/g) || []).length;
        const tabCount = (firstLine.match(/\t/g) || []).length;
        const pipeCount = (firstLine.match(/\|/g) || []).length;
        
        let separator = ',';
        if (semicolonCount > commaCount && semicolonCount > tabCount && semicolonCount > pipeCount) {
            separator = ';';
        } else if (tabCount > commaCount && tabCount > semicolonCount && tabCount > pipeCount) {
            separator = '\t';
        } else if (pipeCount > commaCount && pipeCount > semicolonCount && pipeCount > tabCount) {
            separator = '|';
        }

        // RFC-4180 compliant CSV parser to handle quotes, newlines, and escaped quotes properly
        const parseCsv = (csvText: string, sep: string) => {
            const rows: string[][] = [];
            let currentRow: string[] = [];
            let currentCell = '';
            let inQuotes = false;
            
            for (let i = 0; i < csvText.length; i++) {
                const char = csvText[i];
                const nextChar = csvText[i + 1];
                
                if (char === '"') {
                    if (inQuotes && nextChar === '"') {
                        currentCell += '"';
                        i++; // skip next quote
                    } else {
                        inQuotes = !inQuotes;
                    }
                } else if (char === sep && !inQuotes) {
                    currentRow.push(currentCell.trim());
                    currentCell = '';
                } else if ((char === '\r' || char === '\n') && !inQuotes) {
                    if (char === '\r' && nextChar === '\n') {
                        i++; // skip \n of \r\n
                    }
                    currentRow.push(currentCell.trim());
                    if (currentRow.length > 0 && (currentRow.length > 1 || currentRow[0] !== '')) {
                        rows.push(currentRow);
                    }
                    currentRow = [];
                    currentCell = '';
                } else {
                    currentCell += char;
                }
            }
            if (currentCell || currentRow.length > 0) {
                currentRow.push(currentCell.trim());
                rows.push(currentRow);
            }
            return rows;
        };

        const rows = parseCsv(cleanedText, separator);
        if (rows.length < 2) {
            toast.error("El archivo CSV está vacío o no contiene suficientes filas.");
            return;
        }

        const normalizeHeader = (str: string) => {
            return str
                .toLowerCase()
                .normalize("NFD")
                .replace(/[\u0300-\u036f]/g, "") // remove accents/diacritics
                .trim();
        };

        const parseNumber = (val: string): number => {
            if (!val) return 0;
            let cleaned = val.replace(/[$\s]/g, ''); // Remove currency and spaces
            if (cleaned.includes(',') && !cleaned.includes('.')) {
                cleaned = cleaned.replace(',', '.'); // Handle Spanish comma decimals (e.g. 12,50)
            } else if (cleaned.includes(',') && cleaned.includes('.')) {
                if (cleaned.indexOf('.') < cleaned.indexOf(',')) {
                    cleaned = cleaned.replace(/\./g, '').replace(',', '.'); // e.g. 1.250,50 -> 1250.50
                } else {
                    cleaned = cleaned.replace(/,/g, ''); // e.g. 1,250.50 -> 1250.50
                }
            } else if (cleaned.includes(',')) {
                cleaned = cleaned.replace(/,/g, ''); // Remove thousands separator
            }
            const parsed = parseFloat(cleaned);
            return isNaN(parsed) ? 0 : parsed;
        };

        const rawHeaders = rows[0];
        const headers = rawHeaders.map(h => normalizeHeader(h));
        const getIndex = (names: string[]) => {
            // 1. Try an exact match first to prevent collisions
            const exactIdx = headers.findIndex(h => names.includes(h));
            if (exactIdx !== -1) return exactIdx;

            // 2. Try substring match, but exclude 'mayoreo'/'wholesale' when searching for retail 'precio'/'price'
            return headers.findIndex(h => {
                return names.some(n => {
                    if (n === 'precio' || n === 'price' || n === 'venta' || n === 'sell') {
                        if (h.includes('mayoreo') || h.includes('wholesale')) {
                            return false;
                        }
                    }
                    return h.includes(n);
                });
            });
        };

        const nameIdx = getIndex(['name', 'nombre', 'descripcion', 'description', 'producto', 'product', 'articulo', 'article']);
        const barcodeIdx = getIndex(['barcode', 'codigo', 'bar', 'code', 'id', 'sku', 'upc', 'ean']);
        const costIdx = getIndex(['cost', 'costo', 'compra', 'buy', 'adquisicion']);
        const priceIdx = getIndex(['price', 'precio', 'venta', 'sell', 'publico']);
        const wholesalePriceIdx = getIndex(['wholesaleprice', 'preciomayoreo', 'mayoreo_precio', 'mayoreo', 'precio_mayo']);
        const wholesaleThresholdIdx = getIndex(['wholesalethreshold', 'cantidadmayoreo', 'mayoreo_cantidad', 'limite_mayoreo', 'limite', 'cant_mayo']);
        const stockIdx = getIndex(['stock', 'inventario', 'cantidad', 'qty', 'quantity', 'existencia', 'existencias', 'cant']);
        const categoryIdx = getIndex(['category', 'categoria', 'cat', 'departamento', 'dept', 'grupo']);
        const unitIdx = getIndex(['unit', 'unidad', 'uni', 'medida']);

        if (nameIdx === -1 || priceIdx === -1) {
            toast.error(
                `Las columnas obligatorias no fueron detectadas. Columnas del CSV: [${rawHeaders.join(', ')}]. ` +
                `Asegúrese de incluir 'nombre' (o 'name') y 'precio' (o 'price').`
            );
            return;
        }

        const productsToSave: Product[] = [];
        let imported = 0;
        let updated = 0;

        for (let i = 1; i < rows.length; i++) {
            const cells = rows[i];
            if (cells.length === 0 || !cells[nameIdx]) continue;

            const name = cells[nameIdx];
            const barcode = barcodeIdx !== -1 ? cells[barcodeIdx] : undefined;
            const cost = costIdx !== -1 ? parseNumber(cells[costIdx]) : 0;
            const price = parseNumber(cells[priceIdx]);
            const wholesalePrice = wholesalePriceIdx !== -1 && cells[wholesalePriceIdx] ? parseNumber(cells[wholesalePriceIdx]) : undefined;
            const wholesaleThreshold = wholesaleThresholdIdx !== -1 && cells[wholesaleThresholdIdx] ? parseNumber(cells[wholesaleThresholdIdx]) : undefined;
            const stock = stockIdx !== -1 ? parseNumber(cells[stockIdx]) : 0;
            const category = categoryIdx !== -1 ? cells[categoryIdx] || 'General' : 'General';
            const unit = unitIdx !== -1 ? cells[unitIdx] || 'Pza' : 'Pza';

            const existing = products.find(p => 
                (barcode && p.barcode === barcode) || 
                (p.name && name && p.name.toLowerCase().trim() === name.toLowerCase().trim())
            );

            if (existing) {
                productsToSave.push({
                    ...existing,
                    barcode: barcode || existing.barcode,
                    name: name || existing.name,
                    cost: costIdx !== -1 ? cost : existing.cost,
                    price: priceIdx !== -1 ? price : existing.price,
                    wholesalePrice: wholesalePriceIdx !== -1 ? wholesalePrice : existing.wholesalePrice,
                    wholesaleThreshold: wholesaleThresholdIdx !== -1 ? wholesaleThreshold : existing.wholesaleThreshold,
                    stock: stockIdx !== -1 ? existing.stock + stock : existing.stock,
                    category: categoryIdx !== -1 ? category : existing.category,
                    unit: unitIdx !== -1 ? unit : existing.unit
                });
                updated++;
            } else {
                productsToSave.push({
                    id: `PROD-${Date.now()}-${i}`,
                    barcode: barcode || undefined,
                    name,
                    cost,
                    price,
                    wholesalePrice,
                    wholesaleThreshold,
                    stock,
                    category,
                    unit
                });
                imported++;
            }
        }

        if (productsToSave.length > 0) {
            await saveProductsBulk(productsToSave);
            toast.success(`CSV Procesado con éxito: ${imported} creados, ${updated} actualizados.`);
        } else {
            toast.info("No se encontraron productos válidos para importar.");
        }
        setProducts(await getProducts());
    } catch (e) {
        console.error("CSV Import error:", e);
        toast.error(`Error al procesar el archivo CSV: ${(e as Error).message}`);
    }

    if (csvInputRef.current) csvInputRef.current.value = '';
  };

  const handleClearInventory = async () => {
    if (confirm('⚠️ ¿ESTÁS ABSOLUTAMENTE SEGURO de borrar TODO el inventario? Esta acción no se puede deshacer.')) {
        const confirmationText = prompt('Escribe "ELIMINAR" para confirmar el borrado completo del inventario:');
        if (confirmationText === 'ELIMINAR') {
            try {
                await clearInventory();
                toast.success("Todo el inventario ha sido borrado correctamente.");
                setProducts([]);
            } catch (e) {
                toast.error("Ocurrió un error al borrar el inventario.");
            }
        } else {
            toast.info("Acción cancelada.");
        }
    }
  };




  return (
    <>
      <AppShell activeModule="inventory">
        <header className="top-bar">
          <div className="ticket-title" style={{ fontSize: '24px', display: 'flex', alignItems: 'center', gap: '12px', minWidth: 'max-content' }}>
            <Package size={28} className="text-accent" weight="regular" />
            Inventario
          </div>
          <div className="search-container" style={{ width: '100%', maxWidth: '600px' }}>
            <input 
              type="text" className="search-input" placeholder="Buscar por nombre, categoría o código..." 
              value={searchQuery} onChange={e => setSearchQuery(e.target.value)}
            />
          </div>
          <div style={{ display: 'flex', gap: '12px' }}>
              <input type="file" accept=".xml" ref={fileInputRef} style={{ display: 'none' }} onChange={handleFileUpload} />
              <input type="file" accept=".csv" ref={csvInputRef} style={{ display: 'none' }} onChange={handleCsvUpload} />
              <button 
                className="nav-item" 
                style={{ 
                  margin: 0, 
                  padding: '10px 16px', 
                  backgroundColor: 'var(--bg-tertiary)', 
                  border: '1px solid var(--border-color)', 
                  borderRadius: 'var(--radius-md)',
                  color: 'var(--text-primary)',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '6px',
                  width: 'auto',
                  height: 'auto'
                }} 
                onClick={() => fileInputRef.current?.click()} 
                title="Importar Factura XML"
              >
                <UploadSimple size={20} weight="regular" /> XML
              </button>
              <button 
                className="nav-item" 
                style={{ 
                  margin: 0, 
                  padding: '10px 16px', 
                  backgroundColor: 'var(--bg-tertiary)', 
                  border: '1px solid var(--border-color)', 
                  borderRadius: 'var(--radius-md)',
                  color: 'var(--text-primary)',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '6px',
                  width: 'auto',
                  height: 'auto'
                }} 
                onClick={() => csvInputRef.current?.click()} 
                title="Importar CSV de Productos"
              >
                <UploadSimple size={20} weight="regular" /> CSV
              </button>
              <button 
                className="nav-item" 
                style={{ 
                  margin: 0, 
                  padding: '10px 16px', 
                  backgroundColor: 'var(--bg-tertiary)', 
                  border: '1px solid var(--border-color)', 
                  borderRadius: 'var(--radius-md)', 
                  color: 'var(--accent-warning)',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '6px',
                  width: 'auto',
                  height: 'auto'
                }} 
                onClick={handleOpenRestock} 
                title="Surtido Predictivo"
              >
                <TrendUp size={20} weight="regular" /> Predictivo
              </button>
              <button className="checkout-btn" style={{ margin: 0, width: 'auto', height: 'auto', padding: '10px 20px', display: 'flex', alignItems: 'center', gap: '6px' }} onClick={handleOpenAddModal}>
                <Plus size={20} weight="bold" /> NUEVO
              </button>
              <button 
                className="nav-item" 
                style={{ 
                  margin: 0, 
                  padding: '10px 16px', 
                  backgroundColor: 'var(--accent-danger)', 
                  border: 'none', 
                  borderRadius: 'var(--radius-md)', 
                  color: 'var(--text-on-danger)',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '6px',
                  width: 'auto',
                  height: 'auto',
                  cursor: 'pointer'
                }} 
                onClick={handleClearInventory} 
                title="Borrar Todo el Inventario"
              >
                <Trash size={20} weight="regular" /> BORRAR
              </button>
          </div>
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
                            <Plus size={14} weight="bold" />
                        </button>
                    </div>
                  </td>
                  <td style={{ padding: '16px' }}>
                    <div style={{ display: 'flex', gap: '8px' }}>
                      <button className="icon-btn" onClick={() => handleOpenEditModal(p)}><PencilSimple size={18} weight="regular" /></button>
                      <button className="icon-btn" onClick={() => handleDelete(p.id)} style={{ color: 'var(--accent-danger)' }}><Trash size={18} weight="regular" /></button>
                    </div>
                  </td>
                </tr>

              ))}
            </tbody>
          </table>
          <div style={{ marginTop: '24px', display: 'flex', gap: '12px', alignItems: 'center', backgroundColor: 'rgba(59, 130, 246, 0.1)', padding: '16px', borderRadius: 'var(--radius-md)', color: 'var(--accent-primary)' }}>
              <Info size={20} weight="regular" />
              <span style={{ fontSize: '14px' }}><b>Margen de Ganancias:</b> El sistema calcula utilidades basadas en la diferencia entre &quot;Costo de Compra&quot; y &quot;Precio de Venta&quot;. Completa todos los costos para ver reportes precisos.</span>
          </div>
        </div>
      </AppShell>
      <ProductModal 
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSave={handleSaveProduct}
        product={editingProduct}
        title={editingProduct ? 'Editar Producto' : 'Nuevo Producto'}
      />

      {showRestock && (
        <div className="modal-overlay" style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, backgroundColor: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000, backdropFilter: 'blur(4px)' }}>
          <div className="modal-content" style={{ backgroundColor: 'var(--bg-secondary)', padding: '32px', borderRadius: 'var(--radius-lg)', width: '100%', maxWidth: '600px', border: '1px solid var(--border-color)', maxHeight: '90vh', overflowY: 'auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
              <h2 style={{ fontSize: '24px', fontWeight: 'bold', display: 'flex', alignItems: 'center', gap: '8px' }}><TrendUp size={28} color="var(--accent-warning)" /> Surtido Predictivo</h2>
              <button onClick={() => setShowRestock(false)} className="icon-btn"><X size={24} /></button>
            </div>
            <p style={{ color: 'var(--text-secondary)', marginBottom: '24px' }}>Sugerencias de reabastecimiento generadas a partir del comportamiento de ventas de los últimos 30 días.</p>
            {restockList.length === 0 ? (
                <div style={{ padding: '24px', textAlign: 'center', color: 'var(--text-muted)' }}>No hay suficientes datos o el stock está sano.</div>
            ) : (
                <table style={{ width: '100%', borderCollapse: 'collapse', backgroundColor: 'var(--bg-tertiary)', borderRadius: 'var(--radius-md)', overflow: 'hidden' }}>
                    <thead>
                        <tr style={{ backgroundColor: 'var(--bg-primary)' }}>
                            <th style={{ padding: '12px', textAlign: 'left' }}>Producto</th>
                            <th style={{ padding: '12px', textAlign: 'center' }}>Stock Actual</th>
                            <th style={{ padding: '12px', textAlign: 'center' }}>Pedido Sugerido</th>
                        </tr>
                    </thead>
                    <tbody>
                        {restockList.map(item => (
                            <tr key={item.productId} style={{ borderTop: '1px solid var(--border-color)' }}>
                                <td style={{ padding: '12px' }}>{item.name}</td>
                                <td style={{ padding: '12px', textAlign: 'center', color: 'var(--accent-danger)' }}>{item.stock}</td>
                                <td style={{ padding: '12px', textAlign: 'center', fontWeight: 'bold', color: 'var(--accent-success)' }}>+{item.recommendedOrder}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}
          </div>
        </div>
      )}
    </>
  );
}
