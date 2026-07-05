// src/lib/storage.ts
import AppDatabase from './database';
import { z } from 'zod';
import type { Product, Customer, CustomerPayment, Sale, SaleItemSnapshot, Shift } from '../types';
import { sendTelemetryReport } from './telemetryService';

// ──────────────────────────────────────────────────────────────────
// SCHEMAS
// ──────────────────────────────────────────────────────────────────
const productSchema = z.object({
  id: z.string(),
  barcode: z.string().nullable().optional(),
  name: z.string(),
  cost: z.number(),
  price: z.number(),
  wholesalePrice: z.number().nullable().optional(),
  wholesaleThreshold: z.number().nullable().optional(),
  stock: z.number(),
  category: z.string(),
  unit: z.string(),
  expiresSoon: z.union([z.number(), z.boolean()]).nullable().optional(),
  created_at: z.string().nullable().optional(),
});

// ──────────────────────────────────────────────────────────────────
// INIT
// ──────────────────────────────────────────────────────────────────
/** Initialize DB (ensure tables). Call once on app start */
export const initDB = async (): Promise<void> => {
  await AppDatabase.getInstance();
};

// ──────────────────────────────────────────────────────────────────
// PRODUCTS
// ──────────────────────────────────────────────────────────────────
/** Get paginated products */
export const getProductsPaginated = async (page: number, size: number): Promise<Product[]> => {
  const offset = (page - 1) * size;
  const db = await AppDatabase.getInstance();
  const rows = await db.rawSelect<Record<string, unknown>>(
    'SELECT id, barcode, name, cost, price, wholesalePrice, wholesaleThreshold, stock, category, unit, expiresSoon FROM products ORDER BY name LIMIT ? OFFSET ?;',
    [size, offset]
  );
  return rows.map((r) => ({
    id: String(r.id),
    barcode: r.barcode ? String(r.barcode) : undefined,
    name: String(r.name),
    cost: Number(r.cost),
    price: Number(r.price),
    wholesalePrice: r.wholesalePrice ? Number(r.wholesalePrice) : undefined,
    wholesaleThreshold: r.wholesaleThreshold ? Number(r.wholesaleThreshold) : undefined,
    stock: Number(r.stock),
    category: String(r.category),
    unit: String(r.unit),
    expiresSoon: r.expiresSoon === 1 || r.expiresSoon === true,
  }));
};

/** Get all products (with safe limit) */
export const getProducts = async (limit = 5000, offset = 0): Promise<Product[]> => {
  const db = await AppDatabase.getInstance();
  const rows = await db.rawSelect<Record<string, unknown>>(
    'SELECT id, barcode, name, cost, price, wholesalePrice, wholesaleThreshold, stock, category, unit, expiresSoon FROM products ORDER BY name LIMIT ? OFFSET ?;',
    [limit, offset]
  );
  return rows.map((r) => ({
    id: String(r.id),
    barcode: r.barcode ? String(r.barcode) : undefined,
    name: String(r.name),
    cost: Number(r.cost),
    price: Number(r.price),
    wholesalePrice: r.wholesalePrice ? Number(r.wholesalePrice) : undefined,
    wholesaleThreshold: r.wholesaleThreshold ? Number(r.wholesaleThreshold) : undefined,
    stock: Number(r.stock),
    category: String(r.category),
    unit: String(r.unit),
    expiresSoon: r.expiresSoon === 1 || r.expiresSoon === true,
  }));
};

/** Add a product */
export const addProduct = async (product: Product): Promise<void> => {
  const db = await AppDatabase.getInstance();
  const id = product.id || `PROD-${Date.now()}`;
  await db.execute(
    'INSERT OR REPLACE INTO products (id, barcode, name, cost, price, wholesalePrice, wholesaleThreshold, stock, category, unit, expiresSoon) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);',
    [id, product.barcode ?? '', product.name, product.cost, product.price, product.wholesalePrice ?? 0, product.wholesaleThreshold ?? 0, product.stock, product.category, product.unit, product.expiresSoon ? 1 : 0]
  );
};

/** Update a product */
export const updateProduct = async (product: Product): Promise<void> => {
  const db = await AppDatabase.getInstance();
  await db.execute(
    'UPDATE products SET barcode=?, name=?, cost=?, price=?, wholesalePrice=?, wholesaleThreshold=?, stock=?, category=?, unit=?, expiresSoon=? WHERE id=?;',
    [product.barcode ?? '', product.name, product.cost, product.price, product.wholesalePrice ?? 0, product.wholesaleThreshold ?? 0, product.stock, product.category, product.unit, product.expiresSoon ? 1 : 0, product.id]
  );
};

/** Delete a product */
export const deleteProduct = async (id: string): Promise<void> => {
  const db = await AppDatabase.getInstance();
  await db.execute('DELETE FROM products WHERE id = ?;', [id]);
};

/** Clear all products from inventory */
export const clearInventory = async (): Promise<void> => {
  const db = await AppDatabase.getInstance();
  await db.execute('DELETE FROM products;');
};

/** Save multiple products in bulk in a single transaction */
export const saveProductsBulk = async (productsList: Product[]): Promise<void> => {
  const db = await AppDatabase.getInstance();
  await db.execute('BEGIN TRANSACTION;');
  try {
    for (const p of productsList) {
      const id = p.id || `PROD-${Date.now()}-${Math.random()}`;
      await db.execute(
        'INSERT OR REPLACE INTO products (id, barcode, name, cost, price, wholesalePrice, wholesaleThreshold, stock, category, unit, expiresSoon) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);',
        [id, p.barcode ?? '', p.name, p.cost, p.price, p.wholesalePrice ?? 0, p.wholesaleThreshold ?? 0, p.stock, p.category, p.unit, p.expiresSoon ? 1 : 0]
      );
    }
    await db.execute('COMMIT;');
  } catch (e) {
    await db.execute('ROLLBACK;');
    throw e;
  }
};



// ──────────────────────────────────────────────────────────────────
// CUSTOMERS & FISCAL DATA
// ──────────────────────────────────────────────────────────────────

export interface ClienteFiscal {
  id: string;
  rfc: string;
  razon_social: string;
  codigo_postal: string;
  regimen_fiscal: string;
  uso_cfdi: string;
}

export const getClienteFiscal = async (customerId: string): Promise<ClienteFiscal | null> => {
  const db = await AppDatabase.getInstance();
  const rows = await db.rawSelect<Record<string, unknown>>(
    'SELECT id, rfc, razon_social, codigo_postal, regimen_fiscal, uso_cfdi FROM clientes_fiscales WHERE id = ? LIMIT 1;',
    [customerId]
  );
  if (rows.length === 0) return null;
  const r = rows[0];
  return {
    id: String(r.id),
    rfc: String(r.rfc),
    razon_social: String(r.razon_social),
    codigo_postal: String(r.codigo_postal),
    regimen_fiscal: String(r.regimen_fiscal),
    uso_cfdi: String(r.uso_cfdi),
  };
};

export const saveClienteFiscal = async (cliente: ClienteFiscal): Promise<void> => {
  const db = await AppDatabase.getInstance();
  await db.execute(
    'INSERT OR REPLACE INTO clientes_fiscales (id, rfc, razon_social, codigo_postal, regimen_fiscal, uso_cfdi) VALUES (?, ?, ?, ?, ?, ?);',
    [cliente.id, cliente.rfc, cliente.razon_social, cliente.codigo_postal, cliente.regimen_fiscal, cliente.uso_cfdi]
  );
};
/** Get all customers */
export const getCustomers = async (limit = 5000, offset = 0): Promise<Customer[]> => {
  const db = await AppDatabase.getInstance();
  const rows = await db.rawSelect<Record<string, unknown>>(
    'SELECT id, name, phone, debt, puntos_saldo FROM customers ORDER BY name LIMIT ? OFFSET ?;',
    [limit, offset]
  );
  if (rows.length === 0) return [];

  const customerIds = rows.map((r) => String(r.id));
  const placeholders = customerIds.map(() => '?').join(',');
  const paymentsRows = await db.rawSelect<Record<string, unknown>>(
    `SELECT id, customerId, date, amount FROM customer_payments WHERE customerId IN (${placeholders}) ORDER BY date DESC;`,
    customerIds
  );

  const paymentsMap: Record<string, { id: string; date: string; amount: number }[]> = {};
  for (const p of paymentsRows) {
    const cId = String(p.customerId);
    if (!paymentsMap[cId]) {
      paymentsMap[cId] = [];
    }
    if (paymentsMap[cId].length < 100) {
      paymentsMap[cId].push({
        id: String(p.id),
        date: String(p.date),
        amount: Number(p.amount),
      });
    }
  }

  return rows.map((r) => {
    const cId = String(r.id);
    return {
      id: cId,
      name: String(r.name),
      phone: r.phone ? String(r.phone) : undefined,
      debt: Number(r.debt),
      puntos_saldo: r.puntos_saldo ? Number(r.puntos_saldo) : 0,
      payments: paymentsMap[cId] || [],
    };
  });
};

/** Add a customer */
export const addCustomer = async (customer: Customer): Promise<void> => {
  const db = await AppDatabase.getInstance();
  await db.execute(
    'INSERT OR REPLACE INTO customers (id, name, phone, debt, puntos_saldo) VALUES (?, ?, ?, ?, ?);',
    [customer.id, customer.name, customer.phone ?? '', customer.debt, customer.puntos_saldo ?? 0]
  );
};

/** Update customer debt */
export const updateCustomerDebt = async (customerId: string, newDebt: number): Promise<void> => {
  const db = await AppDatabase.getInstance();
  await db.execute('UPDATE customers SET debt = ? WHERE id = ?;', [newDebt, customerId]);
};

/** Add customer payment */
export const addCustomerPayment = async (customerId: string, amount: number): Promise<void> => {
  const db = await AppDatabase.getInstance();
  const paymentId = `PAY-${Date.now()}`;
  const date = new Date().toISOString();
  await db.execute(
    'INSERT INTO customer_payments (id, customerId, date, amount) VALUES (?, ?, ?, ?);',
    [paymentId, customerId, date, amount]
  );
  // Reduce debt
  await db.execute('UPDATE customers SET debt = MAX(0, debt - ?) WHERE id = ?;', [amount, customerId]);
};

/** Delete a customer */
export const deleteCustomer = async (id: string): Promise<void> => {
  const db = await AppDatabase.getInstance();
  await db.execute('DELETE FROM customer_payments WHERE customerId = ?;', [id]);
  await db.execute('DELETE FROM customers WHERE id = ?;', [id]);
};

// ──────────────────────────────────────────────────────────────────
// SALES
// ──────────────────────────────────────────────────────────────────
/** Save a sale and deduct stock */
export const saveSale = async (sale: Sale): Promise<void> => {
  const db = await AppDatabase.getInstance();
  await db.execute(
    'INSERT INTO sales (id, date, items, total, totalCost, profit, paidAmount, changeAmount, paymentMethod, customerId, isCredit, isCancelled) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);',
    [sale.id, sale.date, JSON.stringify(sale.items), sale.total, sale.totalCost, sale.profit, sale.paidAmount, sale.changeAmount, sale.paymentMethod, sale.customerId ?? null, sale.isCredit ? 1 : 0, sale.isCancelled ? 1 : 0]
  );
  // Deduct stock for each item
  for (const item of sale.items) {
    await db.execute('UPDATE products SET stock = MAX(0, stock - ?) WHERE id = ?;', [item.quantity, item.productId]);
  }
  // If credit sale, add to customer debt
  if (sale.isCredit && sale.customerId) {
    await db.execute('UPDATE customers SET debt = debt + ? WHERE id = ?;', [sale.total, sale.customerId]);
  }
  
  // Combos engine: record item co-occurrences
  if (sale.items.length > 1) {
    const itemIds = sale.items.map(i => i.productId);
    await updateCoOccurrences(itemIds).catch(console.error);
  }
};

/** Get sales history */
export const getSales = async (limit = 500, offset = 0): Promise<Sale[]> => {
  const db = await AppDatabase.getInstance();
  const rows = await db.rawSelect<Record<string, unknown>>(
    'SELECT id, date, items, total, totalCost, profit, paidAmount, changeAmount, paymentMethod, customerId, isCredit, isCancelled, cancelledAt FROM sales ORDER BY date DESC LIMIT ? OFFSET ?;',
    [limit, offset]
  );
  return rows.map((r) => ({
    id: String(r.id),
    date: String(r.date),
    items: JSON.parse(String(r.items)) as SaleItemSnapshot[],
    total: Number(r.total),
    totalCost: Number(r.totalCost),
    profit: Number(r.profit),
    paidAmount: Number(r.paidAmount),
    changeAmount: Number(r.changeAmount),
    paymentMethod: String(r.paymentMethod) as Sale['paymentMethod'],
    customerId: r.customerId ? String(r.customerId) : undefined,
    isCredit: r.isCredit === 1 || r.isCredit === true,
    isCancelled: r.isCancelled === 1 || r.isCancelled === true,
    cancelledAt: r.cancelledAt ? String(r.cancelledAt) : undefined,
  }));
};

/** Cancel a sale and restore stock */
export const cancelSale = async (saleId: string): Promise<void> => {
  const db = await AppDatabase.getInstance();
  const rows = await db.rawSelect<Record<string, unknown>>(
    'SELECT items, customerId, total, isCredit FROM sales WHERE id = ? LIMIT 1;',
    [saleId]
  );
  if (rows.length === 0) return;
  const sale = rows[0];
  const items = JSON.parse(String(sale.items)) as SaleItemSnapshot[];
  // Restore stock
  for (const item of items) {
    await db.execute('UPDATE products SET stock = stock + ? WHERE id = ?;', [item.quantity, item.productId]);
  }
  // If credit, reduce customer debt
  if (sale.isCredit && sale.customerId) {
    await db.execute('UPDATE customers SET debt = MAX(0, debt - ?) WHERE id = ?;', [Number(sale.total), String(sale.customerId)]);
  }
  await db.execute('UPDATE sales SET isCancelled = 1, cancelledAt = ? WHERE id = ?;', [new Date().toISOString(), saleId]);
};

// ──────────────────────────────────────────────────────────────────
// COMBOS & INTELLIGENCE
// ──────────────────────────────────────────────────────────────────
/** Update co-occurrences of products in a single sale */
export const updateCoOccurrences = async (itemIds: string[]): Promise<void> => {
  if (itemIds.length < 2) return;
  const db = await AppDatabase.getInstance();
  for (let i = 0; i < itemIds.length; i++) {
    for (let j = i + 1; j < itemIds.length; j++) {
      const pA = itemIds[i] < itemIds[j] ? itemIds[i] : itemIds[j];
      const pB = itemIds[i] < itemIds[j] ? itemIds[j] : itemIds[i];
      await db.execute(
        `INSERT INTO co_ocurrencia (producto_a, producto_b, frecuencia) 
         VALUES (?, ?, 1) 
         ON CONFLICT(producto_a, producto_b) DO UPDATE SET frecuencia = frecuencia + 1;`,
        [pA, pB]
      );
    }
  }
};

/** Get recommended product for a given product ID based on combo engine */
export const getComboRecommendation = async (productId: string): Promise<{ recommendedId: string; frequency: number } | null> => {
  const db = await AppDatabase.getInstance();
  // We need to check both where the product is A or B
  const rows = await db.rawSelect<Record<string, unknown>>(
    `SELECT producto_a, producto_b, frecuencia FROM co_ocurrencia 
     WHERE producto_a = ? OR producto_b = ? 
     ORDER BY frecuencia DESC LIMIT 1;`,
    [productId, productId]
  );
  if (rows.length === 0) return null;
  const match = rows[0];
  const recommendedId = String(match.producto_a) === productId ? String(match.producto_b) : String(match.producto_a);
  return { recommendedId, frequency: Number(match.frecuencia) };
};

export const updateCustomerPoints = async (customerId: string, pointsDiff: number): Promise<void> => {
  const db = await AppDatabase.getInstance();
  await db.execute(
    'UPDATE customers SET puntos_saldo = MAX(0, puntos_saldo + ?) WHERE id = ?;',
    [pointsDiff, customerId]
  );
};

export const getPredictiveRestockList = async (): Promise<{productId: string, name: string, stock: number, recommendedOrder: number}[]> => {
  const db = await AppDatabase.getInstance();
  // Simplified predictive restock based on recent sales velocity
  // In a real scenario, we would use moving averages. For SQLite limitations, we do a basic sum over the last 30 days.
  const rows = await db.rawSelect<Record<string, unknown>>(
    `SELECT p.id, p.name, p.stock, SUM(json_extract(value, '$.quantity')) as recent_sales
     FROM products p
     LEFT JOIN sales s ON date(s.date) >= date('now', '-30 days') AND s.isCancelled = 0
     LEFT JOIN json_each(s.items) ON json_extract(value, '$.productId') = p.id
     GROUP BY p.id
     HAVING recent_sales > 0 AND p.stock < (recent_sales / 2.0)
     ORDER BY recent_sales DESC LIMIT 50;`
  );
  
  return rows.map((r) => ({
    productId: String(r.id),
    name: String(r.name),
    stock: Number(r.stock),
    recommendedOrder: Math.ceil(Number(r.recent_sales) / 2.0) - Number(r.stock)
  }));
};

// ──────────────────────────────────────────────────────────────────
// SHIFTS
// ──────────────────────────────────────────────────────────────────
/** Get active (open) shift */
export const getActiveShift = async (): Promise<Shift | null> => {
  const db = await AppDatabase.getInstance();
  const rows = await db.rawSelect<Record<string, unknown>>(
    'SELECT id, startTime, endTime, openingBalance, totalCashSales, totalCreditSales, expectedBalance, actualBalance, diff, isOpen FROM shifts WHERE isOpen = 1 ORDER BY startTime DESC LIMIT 1;',
    []
  );
  if (rows.length === 0) return null;
  const r = rows[0];
  return {
    id: String(r.id),
    startTime: String(r.startTime),
    endTime: r.endTime ? String(r.endTime) : undefined,
    openingBalance: Number(r.openingBalance),
    totalCashSales: Number(r.totalCashSales),
    totalCreditSales: Number(r.totalCreditSales),
    expectedBalance: Number(r.expectedBalance),
    actualBalance: r.actualBalance != null ? Number(r.actualBalance) : undefined,
    diff: r.diff != null ? Number(r.diff) : undefined,
    isOpen: true,
  };
};

/** Open a new shift */
export const openShift = async (openingBalance: number): Promise<Shift> => {
  const db = await AppDatabase.getInstance();
  const id = `SHIFT-${Date.now()}`;
  const startTime = new Date().toISOString();
  await db.execute(
    'INSERT INTO shifts (id, startTime, openingBalance, totalCashSales, totalCreditSales, expectedBalance, isOpen) VALUES (?, ?, ?, 0, 0, ?, 1);',
    [id, startTime, openingBalance, openingBalance]
  );
  return {
    id,
    startTime,
    openingBalance,
    totalCashSales: 0,
    totalCreditSales: 0,
    expectedBalance: openingBalance,
    isOpen: true,
  };
};

/** Close the active shift */
export const closeShift = async (actualBalance: number): Promise<void> => {
  const db = await AppDatabase.getInstance();
  const shift = await getActiveShift();
  if (!shift) return;
  const diff = actualBalance - shift.expectedBalance;
  await db.execute(
    'UPDATE shifts SET isOpen = 0, endTime = ?, actualBalance = ?, diff = ? WHERE id = ?;',
    [new Date().toISOString(), actualBalance, diff, shift.id]
  );
};

/** Get all shifts */
export const getShifts = async (limit = 100, offset = 0): Promise<Shift[]> => {
  const db = await AppDatabase.getInstance();
  const rows = await db.rawSelect<Record<string, unknown>>(
    'SELECT id, startTime, endTime, openingBalance, totalCashSales, totalCreditSales, expectedBalance, actualBalance, diff, isOpen FROM shifts ORDER BY startTime DESC LIMIT ? OFFSET ?;',
    [limit, offset]
  );
  return rows.map((r) => ({
    id: String(r.id),
    startTime: String(r.startTime),
    endTime: r.endTime ? String(r.endTime) : undefined,
    openingBalance: Number(r.openingBalance),
    totalCashSales: Number(r.totalCashSales),
    totalCreditSales: Number(r.totalCreditSales),
    expectedBalance: Number(r.expectedBalance),
    actualBalance: r.actualBalance != null ? Number(r.actualBalance) : undefined,
    diff: r.diff != null ? Number(r.diff) : undefined,
    isOpen: r.isOpen === 1 || r.isOpen === true,
  }));
};

// ──────────────────────────────────────────────────────────────────
// PROMOTIONS
// ──────────────────────────────────────────────────────────────────
export interface Promotion {
  id: string;
  name: string;
  type: 'product' | 'category' | 'multibuy';
  targetId: string;
  discountType?: 'percent' | 'fixed';
  discountValue: number;
  buyQty: number;
  payQty: number;
  isActive: boolean;
}

/** Get all promotions */
export const getPromotions = async (limit = 500, offset = 0): Promise<Promotion[]> => {
  const db = await AppDatabase.getInstance();
  const rows = await db.rawSelect<Record<string, unknown>>(
    'SELECT id, name, type, targetId, discountType, discountValue, buyQty, payQty, isActive FROM promotions ORDER BY name LIMIT ? OFFSET ?;',
    [limit, offset]
  );
  return rows.map((r) => ({
    id: String(r.id),
    name: String(r.name),
    type: String(r.type) as Promotion['type'],
    targetId: String(r.targetId),
    discountType: r.discountType ? String(r.discountType) as 'percent' | 'fixed' : undefined,
    discountValue: Number(r.discountValue),
    buyQty: Number(r.buyQty),
    payQty: Number(r.payQty),
    isActive: r.isActive === 1 || r.isActive === true,
  }));
};

/** Add or update a promotion */
export const savePromotion = async (promo: Promotion): Promise<void> => {
  const db = await AppDatabase.getInstance();
  await db.execute(
    'INSERT OR REPLACE INTO promotions (id, name, type, targetId, discountType, discountValue, buyQty, payQty, isActive) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?);',
    [promo.id, promo.name, promo.type, promo.targetId, promo.discountType ?? null, promo.discountValue, promo.buyQty, promo.payQty, promo.isActive ? 1 : 0]
  );
};

/** Delete a promotion */
export const deletePromotion = async (id: string): Promise<void> => {
  const db = await AppDatabase.getInstance();
  await db.execute('DELETE FROM promotions WHERE id = ?;', [id]);
};

// ──────────────────────────────────────────────────────────────────
// PARKED ORDERS (replaces localStorage)
// ──────────────────────────────────────────────────────────────────
export interface ParkedOrder {
  id: string;
  items: any[];
  timestamp: string;
}

export const getParkedOrders = async (): Promise<ParkedOrder[]> => {
  const db = await AppDatabase.getInstance();
  const rows = await db.rawSelect<Record<string, unknown>>(
    'SELECT id, items, timestamp FROM parked_orders ORDER BY timestamp DESC LIMIT 50;',
    []
  );
  return rows.map((r) => ({
    id: String(r.id),
    items: JSON.parse(String(r.items)),
    timestamp: String(r.timestamp),
  }));
};

export const saveParkedOrder = async (order: ParkedOrder): Promise<void> => {
  const db = await AppDatabase.getInstance();
  await db.execute(
    'INSERT INTO parked_orders (id, items, timestamp) VALUES (?, ?, ?);',
    [order.id, JSON.stringify(order.items), order.timestamp]
  );
};

export const deleteParkedOrder = async (id: string): Promise<void> => {
  const db = await AppDatabase.getInstance();
  await db.execute('DELETE FROM parked_orders WHERE id = ?', [id]);
};

export interface Asistencia {
    id: string;
    usuario_id: string;
    tipo_movimiento: 'ENTRADA' | 'SALIDA';
    timestamp: string;
    ruta_foto_evidencia: string;
}

export const recordAssistance = async (userId: string, type: 'ENTRADA' | 'SALIDA', photoPath: string): Promise<void> => {
    const db = await AppDatabase.getInstance();
    const id = `ASIST-${Date.now()}`;
    const timestamp = new Date().toISOString();
    await db.execute(
        'INSERT INTO asistencias (id, usuario_id, tipo_movimiento, timestamp, ruta_foto_evidencia) VALUES (?, ?, ?, ?, ?)',
        [id, userId, type, timestamp, photoPath]
    );
};

export const getAssistances = async (limit = 100, offset = 0): Promise<Asistencia[]> => {
    const db = await AppDatabase.getInstance();
    const rows = await db.rawSelect<Record<string, unknown>>(
        'SELECT id, usuario_id, tipo_movimiento, timestamp, ruta_foto_evidencia FROM asistencias ORDER BY timestamp DESC LIMIT ? OFFSET ?;',
        [limit, offset]
    );
    return rows.map((r) => ({
        id: String(r.id),
        usuario_id: String(r.usuario_id),
        tipo_movimiento: String(r.tipo_movimiento) as 'ENTRADA' | 'SALIDA',
        timestamp: String(r.timestamp),
        ruta_foto_evidencia: String(r.ruta_foto_evidencia),
    }));
};

// ──────────────────────────────────────────────────────────────────
// USUARIOS (RBAC)
// ──────────────────────────────────────────────────────────────────

export interface Usuario {
    id: string;
    nombre: string;
    rol: 'ADMIN' | 'GERENTE' | 'CAJERO';
    password_hash?: string;
    pin_checador_hash?: string;
    estatus: number;
}

export const getUsersCount = async (): Promise<number> => {
    const db = await AppDatabase.getInstance();
    const rows = await db.rawSelect<{ count: number }>('SELECT COUNT(*) as count FROM usuarios;');
    return rows.length > 0 ? Number(rows[0].count) : 0;
};

export const getUsers = async (): Promise<Usuario[]> => {
    const db = await AppDatabase.getInstance();
    const rows = await db.rawSelect<Record<string, unknown>>('SELECT id, nombre, rol, password_hash, pin_checador_hash, estatus FROM usuarios;');
    return rows.map(r => ({
        id: String(r.id),
        nombre: String(r.nombre),
        rol: String(r.rol) as 'ADMIN' | 'GERENTE' | 'CAJERO',
        password_hash: r.password_hash ? String(r.password_hash) : undefined,
        pin_checador_hash: r.pin_checador_hash ? String(r.pin_checador_hash) : undefined,
        estatus: Number(r.estatus)
    }));
};

export const getUserByName = async (nombre: string): Promise<Usuario | null> => {
    const db = await AppDatabase.getInstance();
    const rows = await db.rawSelect<Record<string, unknown>>('SELECT id, nombre, rol, password_hash, pin_checador_hash, estatus FROM usuarios WHERE nombre = ? LIMIT 1;', [nombre]);
    if (rows.length === 0) return null;
    const r = rows[0];
    return {
        id: String(r.id),
        nombre: String(r.nombre),
        rol: String(r.rol) as 'ADMIN' | 'GERENTE' | 'CAJERO',
        password_hash: r.password_hash ? String(r.password_hash) : undefined,
        pin_checador_hash: r.pin_checador_hash ? String(r.pin_checador_hash) : undefined,
        estatus: Number(r.estatus)
    };
};

export const getManagers = async (): Promise<Usuario[]> => {
    const db = await AppDatabase.getInstance();
    const rows = await db.rawSelect<Record<string, unknown>>("SELECT id, nombre, rol, password_hash, pin_checador_hash, estatus FROM usuarios WHERE rol IN ('ADMIN', 'GERENTE') AND estatus = 1;");
    return rows.map(r => ({
        id: String(r.id),
        nombre: String(r.nombre),
        rol: String(r.rol) as 'ADMIN' | 'GERENTE' | 'CAJERO',
        password_hash: r.password_hash ? String(r.password_hash) : undefined,
        pin_checador_hash: r.pin_checador_hash ? String(r.pin_checador_hash) : undefined,
        estatus: Number(r.estatus)
    }));
};



export const saveUser = async (user: Usuario): Promise<void> => {
    const db = await AppDatabase.getInstance();
    await db.execute(
        'INSERT OR REPLACE INTO usuarios (id, nombre, rol, password_hash, pin_checador_hash, estatus) VALUES (?, ?, ?, ?, ?, ?)',
        [user.id, user.nombre, user.rol, user.password_hash || '', user.pin_checador_hash || '', user.estatus]
    );
};

export const deleteUser = async (id: string): Promise<void> => {
    const db = await AppDatabase.getInstance();
    await db.execute('DELETE FROM usuarios WHERE id = ?;', [id]);
};



// ──────────────────────────────────────────────────────────────────
// SETTINGS (replaces localStorage)
// ──────────────────────────────────────────────────────────────────
/** Get a setting value */
export const getSetting = async (key: string, fallback = ''): Promise<string> => {
  const db = await AppDatabase.getInstance();
  const rows = await db.rawSelect<Record<string, unknown>>(
    'SELECT value FROM settings WHERE key = ? LIMIT 1;',
    [key]
  );
  return rows.length > 0 ? String(rows[0].value) : fallback;
};

/** Set a setting value (upsert) */
export const setSetting = async (key: string, value: string): Promise<void> => {
  const db = await AppDatabase.getInstance();
  await db.execute(
    'INSERT OR REPLACE INTO settings (key, value) VALUES (?, ?);',
    [key, value]
  );
};

// ──────────────────────────────────────────────────────────────────
// AUDIT LOG
// ──────────────────────────────────────────────────────────────────
/** Log an audit event */
export const logAudit = async (level: 'info' | 'warn' | 'error', message: string, meta?: Record<string, unknown>): Promise<void> => {
  try {
    const db = await AppDatabase.getInstance();
    await db.execute(
      'INSERT INTO audit_log (level, message, meta) VALUES (?, ?, ?);',
      [level, message, meta ? JSON.stringify(meta) : null]
    );
    // Disparar reporte asíncrono de telemetría sin bloquear el hilo principal
    sendTelemetryReport(level, message, meta).catch(() => {});
  } catch (e) {
    console.error('Audit log failed:', e);
  }
};

// ──────────────────────────────────────────────────────────────────
// CLEAR ALL (for tests)
// ──────────────────────────────────────────────────────────────────
export const clearAllData = async (): Promise<void> => {
  const db = await AppDatabase.getInstance();
  await db.execute('DELETE FROM products;');
  await db.execute('DELETE FROM customers;');
  await db.execute('DELETE FROM customer_payments;');
  await db.execute('DELETE FROM sales;');
  await db.execute('DELETE FROM shifts;');
  await db.execute('DELETE FROM promotions;');
  await db.execute('DELETE FROM parked_orders;');
  await db.execute('DELETE FROM settings;');
  await db.execute('DELETE FROM audit_log;');
};
