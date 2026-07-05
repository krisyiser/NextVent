// src/lib/database.ts
import Database from '@tauri-apps/plugin-sql';
import { z } from 'zod';
import { toast } from 'sonner';

/**
 * LocalStorage mock database for browser-only environment compatibility.
 * Persists tables to localStorage under 'mock_db_<tableName>' as JSON arrays.
 */
class MockLocalStorageDatabase {
  private getTable(name: string): any[] {
    if (typeof window === 'undefined') return [];
    try {
      const data = localStorage.getItem(`mock_db_${name}`);
      return data ? JSON.parse(data) : [];
    } catch {
      return [];
    }
  }

  private saveTable(name: string, data: any[]): void {
    if (typeof window === 'undefined') return;
    try {
      localStorage.setItem(`mock_db_${name}`, JSON.stringify(data));
    } catch (e) {
      console.error(`Failed to save table ${name} to localStorage`, e);
    }
  }

  async select<T>(sql: string, params: unknown[] = []): Promise<T[]> {
    const normalized = sql.trim().toLowerCase();
    
    // Settings query
    if (normalized.includes('from settings')) {
      const settings = this.getTable('settings');
      if (normalized.includes('where key = ?')) {
        const key = params[0] as string;
        const found = settings.find(s => s.key === key);
        return (found ? [found] : []) as unknown as T[];
      }
      return settings as unknown as T[];
    }

    // Products query
    if (normalized.includes('from products')) {
      let products = this.getTable('products');
      // Sort by name
      products.sort((a, b) => String(a.name).localeCompare(String(b.name)));
      
      // Check limits
      const limitMatch = sql.match(/limit\s+(\?|\d+)/i);
      const offsetMatch = sql.match(/offset\s+(\?|\d+)/i);
      
      let limit = products.length;
      let offset = 0;
      
      if (limitMatch) {
        limit = limitMatch[1] === '?' ? (params[0] as number) : parseInt(limitMatch[1], 10);
      }
      if (offsetMatch) {
        offset = offsetMatch[1] === '?' ? (params[params.length - 1] as number) : parseInt(offsetMatch[1], 10);
      }
      
      return products.slice(offset, offset + limit) as unknown as T[];
    }

    // Customers query
    if (normalized.includes('from customers')) {
      const customers = this.getTable('customers');
      customers.sort((a, b) => String(a.name).localeCompare(String(b.name)));
      
      const limitMatch = sql.match(/limit\s+(\?|\d+)/i);
      const offsetMatch = sql.match(/offset\s+(\?|\d+)/i);
      let limit = customers.length;
      let offset = 0;
      if (limitMatch) {
        limit = limitMatch[1] === '?' ? (params[0] as number) : parseInt(limitMatch[1], 10);
      }
      if (offsetMatch) {
        offset = offsetMatch[1] === '?' ? (params[params.length - 1] as number) : parseInt(offsetMatch[1], 10);
      }
      return customers.slice(offset, offset + limit) as unknown as T[];
    }

    // Clientes fiscales query
    if (normalized.includes('from clientes_fiscales')) {
      const clientes = this.getTable('clientes_fiscales');
      return clientes as unknown as T[];
    }

    // Customer payments query
    if (normalized.includes('from customer_payments')) {
      const payments = this.getTable('customer_payments');
      if (normalized.includes('where customerid = ?')) {
        const custId = params[0] as string;
        const filtered = payments.filter(p => p.customerId === custId);
        filtered.sort((a, b) => String(b.date).localeCompare(String(a.date)));
        return filtered.slice(0, 100) as unknown as T[];
      }
      return payments as unknown as T[];
    }

    // Sales query
    if (normalized.includes('from sales')) {
      const sales = this.getTable('sales');
      if (normalized.includes('where id = ?')) {
        const id = params[0] as string;
        const found = sales.find(s => s.id === id);
        return (found ? [found] : []) as unknown as T[];
      }
      sales.sort((a, b) => String(b.date).localeCompare(String(a.date)));
      const limitMatch = sql.match(/limit\s+(\?|\d+)/i);
      const offsetMatch = sql.match(/offset\s+(\?|\d+)/i);
      let limit = sales.length;
      let offset = 0;
      if (limitMatch) {
        limit = limitMatch[1] === '?' ? (params[0] as number) : parseInt(limitMatch[1], 10);
      }
      if (offsetMatch) {
        offset = offsetMatch[1] === '?' ? (params[params.length - 1] as number) : parseInt(offsetMatch[1], 10);
      }
      return sales.slice(offset, offset + limit) as unknown as T[];
    }

    // Shifts query
    if (normalized.includes('from shifts')) {
      const shifts = this.getTable('shifts');
      if (normalized.includes('where isopen = 1')) {
        const openShift = shifts.find(s => s.isOpen === 1 || s.isOpen === true);
        return (openShift ? [openShift] : []) as unknown as T[];
      }
      shifts.sort((a, b) => String(b.startTime).localeCompare(String(a.startTime)));
      const limitMatch = sql.match(/limit\s+(\?|\d+)/i);
      const offsetMatch = sql.match(/offset\s+(\?|\d+)/i);
      let limit = shifts.length;
      let offset = 0;
      if (limitMatch) {
        limit = limitMatch[1] === '?' ? (params[0] as number) : parseInt(limitMatch[1], 10);
      }
      if (offsetMatch) {
        offset = offsetMatch[1] === '?' ? (params[params.length - 1] as number) : parseInt(offsetMatch[1], 10);
      }
      return shifts.slice(offset, offset + limit) as unknown as T[];
    }

    // Promotions query
    if (normalized.includes('from promotions')) {
      const promotions = this.getTable('promotions');
      promotions.sort((a, b) => String(a.name).localeCompare(String(b.name)));
      const limitMatch = sql.match(/limit\s+(\?|\d+)/i);
      const offsetMatch = sql.match(/offset\s+(\?|\d+)/i);
      let limit = promotions.length;
      let offset = 0;
      if (limitMatch) {
        limit = limitMatch[1] === '?' ? (params[0] as number) : parseInt(limitMatch[1], 10);
      }
      if (offsetMatch) {
        offset = offsetMatch[1] === '?' ? (params[params.length - 1] as number) : parseInt(offsetMatch[1], 10);
      }
      return promotions.slice(offset, offset + limit) as unknown as T[];
    }

    // Parked orders query
    if (normalized.includes('from parked_orders')) {
      const orders = this.getTable('parked_orders');
      orders.sort((a, b) => String(b.timestamp).localeCompare(String(a.timestamp)));
      return orders.slice(0, 50) as unknown as T[];
    }

    // Co-occurrences query
    if (normalized.includes('from co_ocurrencia')) {
      const co = this.getTable('co_ocurrencia');
      const prod = params[0] as string;
      const filtered = co.filter(c => c.producto_a === prod || c.producto_b === prod);
      filtered.sort((a, b) => (b.frecuencia || 0) - (a.frecuencia || 0));
      return filtered.slice(0, 1) as unknown as T[];
    }

    // Usuarios query
    if (normalized.includes('from usuarios')) {
      const usuarios = this.getTable('usuarios');
      if (normalized.includes('count(*)')) {
        return [{ count: usuarios.length }] as unknown as T[];
      }
      if (normalized.includes('where nombre = ?')) {
        const nombre = params[0] as string;
        const found = usuarios.find(u => u.nombre === nombre);
        return (found ? [found] : []) as unknown as T[];
      }
      if (normalized.includes('where rol in')) {
        const filtered = usuarios.filter(u => u.rol === 'ADMIN' || u.rol === 'GERENTE');
        return filtered as unknown as T[];
      }
      return usuarios as unknown as T[];
    }

    return [] as T[];
  }

  async execute(sql: string, params: unknown[] = []): Promise<{ rowsAffected: number; lastInsertId: number }> {
    const normalized = sql.trim().toLowerCase();

    // 1. Transaction markers
    if (normalized.startsWith('begin') || normalized.startsWith('commit') || normalized.startsWith('rollback')) {
      return { rowsAffected: 0, lastInsertId: 0 };
    }

    // 2. Settings writes
    if (normalized.startsWith('insert or replace into settings') || normalized.startsWith('insert into settings')) {
      const settings = this.getTable('settings');
      const key = params[0] as string;
      const value = params[1] as string;
      const index = settings.findIndex(s => s.key === key);
      if (index >= 0) {
        settings[index].value = value;
      } else {
        settings.push({ key, value });
      }
      this.saveTable('settings', settings);
      return { rowsAffected: 1, lastInsertId: 0 };
    }

    // 3. Products writes
    if (normalized.startsWith('insert or replace into products') || normalized.startsWith('insert into products')) {
      const products = this.getTable('products');
      const id = params[0] as string;
      const barcode = params[1] as string;
      const name = params[2] as string;
      const cost = params[3] as number;
      const price = params[4] as number;
      const wholesalePrice = params[5] as number;
      const wholesaleThreshold = params[6] as number;
      const stock = params[7] as number;
      const category = params[8] as string;
      const unit = params[9] as string;
      const expiresSoon = params[10] as number;
      
      const newProd = { id, barcode, name, cost, price, wholesalePrice, wholesaleThreshold, stock, category, unit, expiresSoon };
      const idx = products.findIndex(p => p.id === id);
      if (idx >= 0) {
        products[idx] = newProd;
      } else {
        products.push(newProd);
      }
      this.saveTable('products', products);
      return { rowsAffected: 1, lastInsertId: 0 };
    }
    if (normalized.startsWith('update products set')) {
      const products = this.getTable('products');
      if (normalized.includes('where id=?;') || normalized.includes('where id = ?')) {
        const barcode = params[0] as string;
        const name = params[1] as string;
        const cost = params[2] as number;
        const price = params[3] as number;
        const wholesalePrice = params[4] as number;
        const wholesaleThreshold = params[5] as number;
        const stock = params[6] as number;
        const category = params[7] as string;
        const unit = params[8] as string;
        const expiresSoon = params[9] as number;
        const id = params[10] as string;

        const idx = products.findIndex(p => p.id === id);
        if (idx >= 0) {
          products[idx] = { ...products[idx], barcode, name, cost, price, wholesalePrice, wholesaleThreshold, stock, category, unit, expiresSoon };
          this.saveTable('products', products);
          return { rowsAffected: 1, lastInsertId: 0 };
        }
      } else if (normalized.includes('stock = max(0, stock - ?)')) {
        const qty = params[0] as number;
        const id = params[1] as string;
        const idx = products.findIndex(p => p.id === id);
        if (idx >= 0) {
          products[idx].stock = Math.max(0, (products[idx].stock || 0) - qty);
          this.saveTable('products', products);
          return { rowsAffected: 1, lastInsertId: 0 };
        }
      } else if (normalized.includes('stock = stock + ?')) {
        const qty = params[0] as number;
        const id = params[1] as string;
        const idx = products.findIndex(p => p.id === id);
        if (idx >= 0) {
          products[idx].stock = (products[idx].stock || 0) + qty;
          this.saveTable('products', products);
          return { rowsAffected: 1, lastInsertId: 0 };
        }
      }
      return { rowsAffected: 0, lastInsertId: 0 };
    }
    if (normalized.startsWith('delete from products')) {
      if (normalized.includes('where id = ?')) {
        const id = params[0] as string;
        let products = this.getTable('products');
        products = products.filter(p => p.id !== id);
        this.saveTable('products', products);
        return { rowsAffected: 1, lastInsertId: 0 };
      } else {
        this.saveTable('products', []);
        return { rowsAffected: 1, lastInsertId: 0 };
      }
    }

    // 4. Customers writes
    if (normalized.startsWith('insert or replace into customers')) {
      const customers = this.getTable('customers');
      const id = params[0] as string;
      const name = params[1] as string;
      const phone = params[2] as string;
      const debt = params[3] as number;
      const puntos_saldo = params[4] as number;
      const idx = customers.findIndex(c => c.id === id);
      const newCust = { id, name, phone, debt, puntos_saldo };
      if (idx >= 0) {
        customers[idx] = newCust;
      } else {
        customers.push(newCust);
      }
      this.saveTable('customers', customers);
      return { rowsAffected: 1, lastInsertId: 0 };
    }
    if (normalized.startsWith('update customers set debt = max(0, debt - ?)')) {
      const amt = params[0] as number;
      const id = params[1] as string;
      const customers = this.getTable('customers');
      const idx = customers.findIndex(c => c.id === id);
      if (idx >= 0) {
        customers[idx].debt = Math.max(0, (customers[idx].debt || 0) - amt);
        this.saveTable('customers', customers);
        return { rowsAffected: 1, lastInsertId: 0 };
      }
      return { rowsAffected: 0, lastInsertId: 0 };
    }
    if (normalized.startsWith('update customers set debt = debt + ?')) {
      const amt = params[0] as number;
      const id = params[1] as string;
      const customers = this.getTable('customers');
      const idx = customers.findIndex(c => c.id === id);
      if (idx >= 0) {
        customers[idx].debt = (customers[idx].debt || 0) + amt;
        this.saveTable('customers', customers);
        return { rowsAffected: 1, lastInsertId: 0 };
      }
      return { rowsAffected: 0, lastInsertId: 0 };
    }
    if (normalized.startsWith('update customers set debt = ?')) {
      const debt = params[0] as number;
      const id = params[1] as string;
      const customers = this.getTable('customers');
      const idx = customers.findIndex(c => c.id === id);
      if (idx >= 0) {
        customers[idx].debt = debt;
        this.saveTable('customers', customers);
        return { rowsAffected: 1, lastInsertId: 0 };
      }
      return { rowsAffected: 0, lastInsertId: 0 };
    }
    if (normalized.startsWith('update customers set puntos_saldo = max(0, puntos_saldo + ?)')) {
      const diff = params[0] as number;
      const id = params[1] as string;
      const customers = this.getTable('customers');
      const idx = customers.findIndex(c => c.id === id);
      if (idx >= 0) {
        customers[idx].puntos_saldo = Math.max(0, (customers[idx].puntos_saldo || 0) + diff);
        this.saveTable('customers', customers);
        return { rowsAffected: 1, lastInsertId: 0 };
      }
      return { rowsAffected: 0, lastInsertId: 0 };
    }
    if (normalized.startsWith('delete from customers')) {
      const id = params[0] as string;
      let customers = this.getTable('customers');
      customers = customers.filter(c => c.id !== id);
      this.saveTable('customers', customers);
      return { rowsAffected: 1, lastInsertId: 0 };
    }

    // Clientes Fiscales writes
    if (normalized.startsWith('insert or replace into clientes_fiscales')) {
      const clientes = this.getTable('clientes_fiscales');
      const id = params[0] as string;
      const rfc = params[1] as string;
      const razon_social = params[2] as string;
      const codigo_postal = params[3] as string;
      const regimen_fiscal = params[4] as string;
      const uso_cfdi = params[5] as string;
      const idx = clientes.findIndex(c => c.id === id);
      const newCliente = { id, rfc, razon_social, codigo_postal, regimen_fiscal, uso_cfdi };
      if (idx >= 0) {
        clientes[idx] = newCliente;
      } else {
        clientes.push(newCliente);
      }
      this.saveTable('clientes_fiscales', clientes);
      return { rowsAffected: 1, lastInsertId: 0 };
    }

    // 5. Customer Payments writes
    if (normalized.startsWith('insert into customer_payments')) {
      const payments = this.getTable('customer_payments');
      const id = params[0] as string;
      const customerId = params[1] as string;
      const date = params[2] as string;
      const amount = params[3] as number;
      payments.push({ id, customerId, date, amount });
      this.saveTable('customer_payments', payments);
      return { rowsAffected: 1, lastInsertId: 0 };
    }
    if (normalized.startsWith('delete from customer_payments')) {
      const custId = params[0] as string;
      let payments = this.getTable('customer_payments');
      payments = payments.filter(p => p.customerId !== custId);
      this.saveTable('customer_payments', payments);
      return { rowsAffected: 1, lastInsertId: 0 };
    }

    // 6. Sales writes
    if (normalized.startsWith('insert into sales')) {
      const sales = this.getTable('sales');
      const id = params[0] as string;
      const date = params[1] as string;
      const items = params[2] as string;
      const total = params[3] as number;
      const totalCost = params[4] as number;
      const profit = params[5] as number;
      const paidAmount = params[6] as number;
      const changeAmount = params[7] as number;
      const paymentMethod = params[8] as string;
      const customerId = params[9] as string;
      const isCredit = params[10] as number;
      const isCancelled = params[11] as number;

      sales.push({ id, date, items, total, totalCost, profit, paidAmount, changeAmount, paymentMethod, customerId, isCredit, isCancelled });
      this.saveTable('sales', sales);
      return { rowsAffected: 1, lastInsertId: 0 };
    }
    if (normalized.startsWith('update sales set iscancelled = 1')) {
      const cancelledAt = params[0] as string;
      const id = params[1] as string;
      const sales = this.getTable('sales');
      const idx = sales.findIndex(s => s.id === id);
      if (idx >= 0) {
        sales[idx].isCancelled = 1;
        sales[idx].cancelledAt = cancelledAt;
        this.saveTable('sales', sales);
        return { rowsAffected: 1, lastInsertId: 0 };
      }
      return { rowsAffected: 0, lastInsertId: 0 };
    }

    // 7. Shifts writes
    if (normalized.startsWith('insert into shifts')) {
      const shifts = this.getTable('shifts');
      const id = params[0] as string;
      const startTime = params[1] as string;
      const openingBalance = params[2] as number;
      const expectedBalance = params[3] as number;
      shifts.push({ id, startTime, openingBalance, totalCashSales: 0, totalCreditSales: 0, expectedBalance, isOpen: 1 });
      this.saveTable('shifts', shifts);
      return { rowsAffected: 1, lastInsertId: 0 };
    }
    if (normalized.startsWith('update shifts set isopen = 0')) {
      const endTime = params[0] as string;
      const actualBalance = params[1] as number;
      const diff = params[2] as number;
      const id = params[3] as string;
      const shifts = this.getTable('shifts');
      const idx = shifts.findIndex(s => s.id === id);
      if (idx >= 0) {
        shifts[idx].isOpen = 0;
        shifts[idx].endTime = endTime;
        shifts[idx].actualBalance = actualBalance;
        shifts[idx].diff = diff;
        this.saveTable('shifts', shifts);
        return { rowsAffected: 1, lastInsertId: 0 };
      }
      return { rowsAffected: 0, lastInsertId: 0 };
    }

    // 8. Promotions writes
    if (normalized.startsWith('insert or replace into promotions')) {
      const promotions = this.getTable('promotions');
      const id = params[0] as string;
      const name = params[1] as string;
      const type = params[2] as string;
      const targetId = params[3] as string;
      const discountType = params[4] as string;
      const discountValue = params[5] as number;
      const buyQty = params[6] as number;
      const payQty = params[7] as number;
      const isActive = params[8] as number;

      const newPromo = { id, name, type, targetId, discountType, discountValue, buyQty, payQty, isActive };
      const idx = promotions.findIndex(p => p.id === id);
      if (idx >= 0) {
        promotions[idx] = newPromo;
      } else {
        promotions.push(newPromo);
      }
      this.saveTable('promotions', promotions);
      return { rowsAffected: 1, lastInsertId: 0 };
    }
    if (normalized.startsWith('delete from promotions')) {
      const id = params[0] as string;
      let promotions = this.getTable('promotions');
      promotions = promotions.filter(p => p.id !== id);
      this.saveTable('promotions', promotions);
      return { rowsAffected: 1, lastInsertId: 0 };
    }

    // 9. Parked Orders writes
    if (normalized.startsWith('insert into parked_orders')) {
      const orders = this.getTable('parked_orders');
      const id = params[0] as string;
      const items = params[1] as string;
      const timestamp = params[2] as string;
      orders.push({ id, items, timestamp });
      this.saveTable('parked_orders', orders);
      return { rowsAffected: 1, lastInsertId: 0 };
    }
    if (normalized.startsWith('delete from parked_orders')) {
      const id = params[0] as string;
      let orders = this.getTable('parked_orders');
      orders = orders.filter(o => o.id !== id);
      this.saveTable('parked_orders', orders);
      return { rowsAffected: 1, lastInsertId: 0 };
    }

    // 10. Co-occurrences writes
    if (normalized.startsWith('insert into co_ocurrencia')) {
      const co = this.getTable('co_ocurrencia');
      const pA = params[0] as string;
      const pB = params[1] as string;
      const idx = co.findIndex(c => c.producto_a === pA && c.producto_b === pB);
      if (idx >= 0) {
        co[idx].frecuencia = (co[idx].frecuencia || 0) + 1;
      } else {
        co.push({ producto_a: pA, producto_b: pB, frecuencia: 1 });
      }
      this.saveTable('co_ocurrencia', co);
      return { rowsAffected: 1, lastInsertId: 0 };
    }

    // 11. Asistencias writes
    if (normalized.startsWith('insert into asistencias')) {
      const asistencias = this.getTable('asistencias');
      const id = params[0] as string;
      const usuario_id = params[1] as string;
      const tipo_movimiento = params[2] as string;
      const timestamp = params[3] as string;
      const ruta_foto_evidencia = params[4] as string;
      asistencias.push({ id, usuario_id, tipo_movimiento, timestamp, ruta_foto_evidencia });
      this.saveTable('asistencias', asistencias);
      return { rowsAffected: 1, lastInsertId: 0 };
    }

    // 12. Audit log writes
    if (normalized.startsWith('insert into audit_log')) {
      const audit = this.getTable('audit_log');
      const level = params[0] as string;
      const message = params[1] as string;
      const meta = params[2] as string;
      audit.push({ id: audit.length + 1, timestamp: new Date().toISOString(), level, message, meta });
      this.saveTable('audit_log', audit);
      return { rowsAffected: 1, lastInsertId: audit.length };
    }

    // 13. Usuarios writes
    if (normalized.startsWith('insert or replace into usuarios') || normalized.startsWith('insert into usuarios')) {
      const usuarios = this.getTable('usuarios');
      const id = params[0] as string;
      const nombre = params[1] as string;
      const rol = params[2] as string;
      const password_hash = params[3] as string;
      const pin_checador_hash = params[4] as string;
      const estatus = params[5] as number;
      
      const newUsuario = { id, nombre, rol, password_hash, pin_checador_hash, estatus };
      const idx = usuarios.findIndex(u => u.id === id);
      if (idx >= 0) {
        usuarios[idx] = newUsuario;
      } else {
        usuarios.push(newUsuario);
      }
      this.saveTable('usuarios', usuarios);
      return { rowsAffected: 1, lastInsertId: 0 };
    }
    if (normalized.startsWith('delete from usuarios')) {
      const id = params[0] as string;
      let usuarios = this.getTable('usuarios');
      usuarios = usuarios.filter(u => u.id !== id);
      this.saveTable('usuarios', usuarios);
      return { rowsAffected: 1, lastInsertId: 0 };
    }

    return { rowsAffected: 0, lastInsertId: 0 };
  }
}

/**
 * Singleton SQLite connection manager using Tauri plugin-sql.
 * All queries are sanitized via Zod schemas and must use pagination (LIMIT/OFFSET).
 */
class AppDatabase {
  private static instance: AppDatabase | null = null;
  private db: any;

  private constructor(db: any) {
    this.db = db;
  }

  static async getInstance(): Promise<AppDatabase> {
    if (!AppDatabase.instance) {
      let dbInstance: any;
      try {
        dbInstance = await Database.load('sqlite:app.db');
          
          // Each CREATE TABLE must be a separate execute call
          await dbInstance.execute(
            `CREATE TABLE IF NOT EXISTS settings (
              key TEXT PRIMARY KEY,
              value TEXT NOT NULL
            );`
          );
          await dbInstance.execute(
            `CREATE TABLE IF NOT EXISTS products (
              id TEXT PRIMARY KEY,
              barcode TEXT,
              name TEXT NOT NULL,
              cost REAL NOT NULL DEFAULT 0,
              price REAL NOT NULL,
              wholesalePrice REAL DEFAULT 0,
              wholesaleThreshold INTEGER DEFAULT 0,
              stock REAL NOT NULL DEFAULT 0,
              category TEXT NOT NULL DEFAULT 'Abarrotes',
              unit TEXT NOT NULL DEFAULT 'Pza',
              expiresSoon INTEGER DEFAULT 0,
              created_at TEXT DEFAULT (datetime('now'))
            );`
          );
          await dbInstance.execute(
            `CREATE TABLE IF NOT EXISTS customers (
              id TEXT PRIMARY KEY,
              name TEXT NOT NULL,
              phone TEXT DEFAULT '',
              debt REAL DEFAULT 0,
              puntos_saldo REAL DEFAULT 0.0
            );`
          );
          
          // Auto-migrate schema for existing customers
          try {
            await dbInstance.execute(`ALTER TABLE customers ADD COLUMN puntos_saldo REAL DEFAULT 0.0;`);
          } catch (e) {
            // Column probably already exists
          }

          await dbInstance.execute(
            `CREATE TABLE IF NOT EXISTS customer_payments (
              id TEXT PRIMARY KEY,
              customerId TEXT NOT NULL,
              date TEXT NOT NULL,
              amount REAL NOT NULL,
              FOREIGN KEY (customerId) REFERENCES customers(id)
            );`
          );
          await dbInstance.execute(
            `CREATE TABLE IF NOT EXISTS sales (
              id TEXT PRIMARY KEY,
              date TEXT NOT NULL,
              items TEXT NOT NULL,
              total REAL NOT NULL,
              totalCost REAL NOT NULL,
              profit REAL NOT NULL,
              paidAmount REAL NOT NULL,
              changeAmount REAL NOT NULL,
              paymentMethod TEXT NOT NULL,
              customerId TEXT,
              isCredit INTEGER DEFAULT 0,
              isCancelled INTEGER DEFAULT 0,
              cancelledAt TEXT,
              estado_fiscal TEXT DEFAULT 'PENDIENTE',
              uuid_sat TEXT DEFAULT NULL,
              serie_folio TEXT DEFAULT NULL
            );`
          );
          await dbInstance.execute(
            `CREATE TABLE IF NOT EXISTS shifts (
              id TEXT PRIMARY KEY,
              startTime TEXT NOT NULL,
              endTime TEXT,
              openingBalance REAL NOT NULL DEFAULT 0,
              totalCashSales REAL DEFAULT 0,
              totalCreditSales REAL DEFAULT 0,
              expectedBalance REAL DEFAULT 0,
              actualBalance REAL,
              diff REAL,
              isOpen INTEGER DEFAULT 1
            );`
          );
          await dbInstance.execute(
            `CREATE TABLE IF NOT EXISTS promotions (
              id TEXT PRIMARY KEY,
              name TEXT NOT NULL,
              type TEXT NOT NULL,
              targetId TEXT,
              discountType TEXT,
              discountValue REAL DEFAULT 0,
              buyQty INTEGER DEFAULT 0,
              payQty INTEGER DEFAULT 0,
              isActive INTEGER DEFAULT 1
            );`
          );
          await dbInstance.execute(
            `CREATE TABLE IF NOT EXISTS parked_orders (
              id TEXT PRIMARY KEY,
              items TEXT NOT NULL,
              timestamp TEXT NOT NULL
            );`
          );
          await dbInstance.execute(
            `CREATE TABLE IF NOT EXISTS audit_log (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              timestamp TEXT NOT NULL DEFAULT (datetime('now')),
              level TEXT NOT NULL,
              message TEXT NOT NULL,
              meta TEXT
            );`
          );
          await dbInstance.execute(
            `CREATE TABLE IF NOT EXISTS co_ocurrencia (
              producto_a TEXT,
              producto_b TEXT,
              frecuencia INTEGER DEFAULT 1,
              PRIMARY KEY (producto_a, producto_b)
            );`
          );
          await dbInstance.execute(
            `CREATE TABLE IF NOT EXISTS asistencias (
              id TEXT PRIMARY KEY,
              usuario_id TEXT NOT NULL,
              tipo_movimiento TEXT NOT NULL,
              timestamp TEXT NOT NULL,
              ruta_foto_evidencia TEXT
            );`
          );
          await dbInstance.execute(
            `CREATE TABLE IF NOT EXISTS clientes_fiscales (
                id TEXT PRIMARY KEY,
                rfc TEXT NOT NULL UNIQUE,
                razon_social TEXT NOT NULL,
                codigo_postal TEXT NOT NULL,
                regimen_fiscal TEXT NOT NULL,
                uso_cfdi TEXT NOT NULL
            );`
          );
          await dbInstance.execute(
            `CREATE TABLE IF NOT EXISTS usuarios (
              id TEXT PRIMARY KEY,
              nombre TEXT NOT NULL,
              rol TEXT NOT NULL,
              password_hash TEXT,
              pin_checador_hash TEXT,
              estatus INTEGER DEFAULT 1
            );`
          );

          // Add SAT columns if they don't exist
          try {
            await dbInstance.execute(`ALTER TABLE sales ADD COLUMN estado_fiscal TEXT DEFAULT 'PENDIENTE';`);
            await dbInstance.execute(`ALTER TABLE sales ADD COLUMN uuid_sat TEXT DEFAULT NULL;`);
            await dbInstance.execute(`ALTER TABLE sales ADD COLUMN serie_folio TEXT DEFAULT NULL;`);
          } catch (e) {
            // Columns probably already exist
          }

          // Create database indexes for performance optimization
          try {
            await dbInstance.execute('CREATE INDEX IF NOT EXISTS idx_products_barcode ON products(barcode);');
            await dbInstance.execute('CREATE INDEX IF NOT EXISTS idx_sales_customer ON sales(customerId);');
            await dbInstance.execute('CREATE INDEX IF NOT EXISTS idx_customer_payments_customer ON customer_payments(customerId);');
          } catch (e) {
            console.warn('Failed to create indexes:', e);
          }
      } catch (err: any) {
        console.error('Failed to load SQLite DB. Initiating LocalStorage fallback database:', err);
        if (typeof window !== 'undefined') {
          setTimeout(() => {
            toast.error(`Error cargando SQLite (usando local): ${err.message || String(err)}`, { duration: 15000 });
          }, 1500);
        }
        dbInstance = new MockLocalStorageDatabase();
      }

      AppDatabase.instance = new AppDatabase(dbInstance);
    }
    return AppDatabase.instance;
  }

  /** Generic typed query with Zod validation */
  async query<T>(sql: string, params: unknown[], schema: z.Schema<T>): Promise<T[]> {
    const rows: Record<string, unknown>[] = await this.db.select(sql, params);
    return rows.map((row) => schema.parse(row));
  }

  /** Raw select without Zod (for internal use) */
  async rawSelect<T = Record<string, unknown>>(sql: string, params: unknown[] = []): Promise<T[]> {
    return await this.db.select(sql, params);
  }

  /** Execute a non-select statement (INSERT, UPDATE, DELETE) */
  async execute(sql: string, params: unknown[] = []): Promise<{ rowsAffected: number; lastInsertId: number }> {
    const res = await this.db.execute(sql, params);
    return {
      rowsAffected: res.rowsAffected,
      lastInsertId: res.lastInsertId ?? 0,
    };
  }
}

export default AppDatabase;
