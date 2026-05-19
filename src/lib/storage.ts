import Database from "@tauri-apps/plugin-sql";
import { Product, Sale, Customer, Shift, SaleItemSnapshot, CustomerPayment } from "@/types";
import { INITIAL_PRODUCTS } from "@/constants";

// Check if we are in Tauri or Browser
const isTauri = typeof window !== 'undefined' && (window as any).__TAURI_IPC__ !== undefined;

let dbInstance: any = null;

// MEMORY/LOCALSTORAGE FALLBACK FOR BROWSER
const getBrowserStore = (key: string) => {
    if (typeof window === 'undefined') return [];
    const data = localStorage.getItem(key);
    return data ? JSON.parse(data) : null;
};

const setBrowserStore = (key: string, data: any) => {
    if (typeof window !== 'undefined') {
        localStorage.setItem(key, JSON.stringify(data));
    }
};

export const initDB = async () => {
    if (!isTauri) return null;
    if (dbInstance) return dbInstance;
    
    try {
        dbInstance = await Database.load("sqlite:pos.db");
        
        // Create Tables
        await dbInstance.execute(`
            CREATE TABLE IF NOT EXISTS products (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                cost REAL NOT NULL,
                price REAL NOT NULL,
                wholesale_price REAL,
                wholesale_threshold REAL,
                stock REAL NOT NULL,
                category TEXT,
                unit TEXT,
                barcode TEXT
            )
        `);

        await dbInstance.execute(`
            CREATE TABLE IF NOT EXISTS customers (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                phone TEXT,
                debt REAL DEFAULT 0
            )
        `);

        await dbInstance.execute(`
            CREATE TABLE IF NOT EXISTS payments (
                id TEXT PRIMARY KEY,
                customer_id TEXT NOT NULL,
                date TEXT NOT NULL,
                amount REAL NOT NULL,
                FOREIGN KEY(customer_id) REFERENCES customers(id)
            )
        `);

        await dbInstance.execute(`
            CREATE TABLE IF NOT EXISTS sales (
                id TEXT PRIMARY KEY,
                date TEXT NOT NULL,
                items_json TEXT NOT NULL,
                total REAL NOT NULL,
                total_cost REAL NOT NULL,
                profit REAL NOT NULL,
                paid_amount REAL NOT NULL,
                change_amount REAL NOT NULL,
                payment_method TEXT DEFAULT 'Cash',
                customer_id TEXT,
                is_credit INTEGER DEFAULT 0,
                is_cancelled INTEGER DEFAULT 0,
                cancelled_at TEXT
            )
        `);

        await dbInstance.execute(`
            CREATE TABLE IF NOT EXISTS promotions (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                type TEXT NOT NULL,
                target_id TEXT,
                discount_value REAL NOT NULL,
                discount_type TEXT NOT NULL,
                buy_qty INTEGER DEFAULT 1,
                pay_qty INTEGER DEFAULT 1,
                is_active INTEGER DEFAULT 1
            )
        `);

        await dbInstance.execute(`
            CREATE TABLE IF NOT EXISTS shifts (
                id TEXT PRIMARY KEY,
                start_time TEXT NOT NULL,
                end_time TEXT,
                opening_balance REAL NOT NULL,
                total_cash_sales REAL DEFAULT 0,
                total_credit_sales REAL DEFAULT 0,
                expected_balance REAL NOT NULL,
                actual_balance REAL,
                diff REAL,
                is_open INTEGER DEFAULT 1
            )
        `);

        // Migrations
        try {
            await dbInstance.execute("ALTER TABLE sales ADD COLUMN payment_method TEXT DEFAULT 'Cash'");
        } catch (e) {
            // Column probably already exists
        }

        // Bootstrap products if empty
        const count: any = await dbInstance.select("SELECT COUNT(*) as count FROM products");
        if (count[0].count === 0) {
            for (const p of INITIAL_PRODUCTS) {
                await dbInstance.execute(
                    "INSERT INTO products (id, name, cost, price, wholesale_price, wholesale_threshold, stock, category, unit, barcode) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                    [p.id, p.name, p.cost, p.price, p.wholesalePrice || null, p.wholesaleThreshold || null, p.stock, p.category, p.unit, p.barcode || null]
                );
            }
        }
        return dbInstance;
    } catch (e) {
        console.error("Failed to load Tauri SQL, falling back to LocalStorage", e);
        return null;
    }
};

// HELPER FOR ROUNDING
const roundMoney = (amount: number): number => {
    return Math.round((amount + Number.EPSILON) * 100) / 100;
};

// PRODUCTS
export const getProducts = async (): Promise<Product[]> => {
    const db = await initDB();
    if (db) {
        const rows: any[] = await db.select("SELECT * FROM products");
        return rows.map(r => ({
            ...r,
            wholesalePrice: r.wholesale_price,
            wholesaleThreshold: r.wholesale_threshold
        }));
    } else {
        const products = getBrowserStore('products');
        if (!products) {
            setBrowserStore('products', INITIAL_PRODUCTS);
            return INITIAL_PRODUCTS;
        }
        // Sanitize retrieved products to auto-heal any duplicate or empty IDs
        let needsSave = false;
        const seenIds = new Set<string>();
        const sanitized = products.map((p: Product, idx: number) => {
            let id = p.id;
            if (!id || id === '' || seenIds.has(id)) {
                id = `PROD-${Date.now()}-${idx}-${Math.floor(1000 + Math.random() * 9000)}`;
                needsSave = true;
            }
            seenIds.add(id);
            return { ...p, id };
        });
        if (needsSave) {
            setBrowserStore('products', sanitized);
        }
        return sanitized;
    }
};

export const saveProducts = async (products: Product[]) => {
    const db = await initDB();
    if (db) {
        for (const p of products) {
            await db.execute(
                "UPDATE products SET name=?, cost=?, price=?, wholesale_price=?, wholesale_threshold=?, stock=?, category=?, unit=?, barcode=? WHERE id=?",
                [p.name, p.cost, p.price, p.wholesalePrice || null, p.wholesaleThreshold || null, p.stock, p.category, p.unit, p.barcode || null, p.id]
            );
        }
    } else {
        setBrowserStore('products', products);
    }
};

export const updateProduct = async (product: Product) => {
    const db = await initDB();
    if (db) {
        await db.execute(
            "UPDATE products SET name=?, cost=?, price=?, wholesale_price=?, wholesale_threshold=?, stock=?, category=?, unit=?, barcode=? WHERE id=?",
            [product.name, product.cost, product.price, product.wholesalePrice || null, product.wholesaleThreshold || null, product.stock, product.category, product.unit, product.barcode || null, product.id]
        );
    } else {
        const products = await getProducts();
        const updated = products.map(p => p.id === product.id ? product : p);
        setBrowserStore('products', updated);
    }
};

export const addProduct = async (product: Product) => {
    const finalProduct = {
        ...product,
        id: product.id && product.id !== '' ? product.id : `PROD-${Date.now()}-${Math.floor(1000 + Math.random() * 9000)}`
    };
    const db = await initDB();
    if (db) {
        await db.execute(
            "INSERT INTO products (id, name, cost, price, wholesale_price, wholesale_threshold, stock, category, unit, barcode) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
            [finalProduct.id, finalProduct.name, finalProduct.cost, finalProduct.price, finalProduct.wholesalePrice || null, finalProduct.wholesaleThreshold || null, finalProduct.stock, finalProduct.category, finalProduct.unit, finalProduct.barcode || null]
        );
    } else {
        const products = await getProducts();
        setBrowserStore('products', [finalProduct, ...products]);
    }
};

export const deleteProduct = async (id: string) => {
    const db = await initDB();
    if (db) {
        await db.execute("DELETE FROM products WHERE id = ?", [id]);
    } else {
        const products = await getProducts();
        setBrowserStore('products', products.filter(p => p.id !== id));
    }
};

// CUSTOMERS
export const getCustomers = async (): Promise<Customer[]> => {
    const db = await initDB();
    if (db) {
        const rows: any[] = await db.select("SELECT * FROM customers");
        const customers = [];
        for (const r of rows) {
            const payments: any[] = await db.select("SELECT * FROM payments WHERE customer_id = ?", [r.id]);
            customers.push({
                ...r,
                payments: payments.map(p => ({ ...p, amount: roundMoney(p.amount) }))
            });
        }
        return customers;
    } else {
        return getBrowserStore('customers') || [];
    }
};

export const addCustomer = async (customer: Customer) => {
    const db = await initDB();
    if (db) {
        await db.execute("INSERT INTO customers (id, name, phone, debt) VALUES (?, ?, ?, ?)", [customer.id, customer.name, customer.phone || null, customer.debt]);
    } else {
        const customers = await getCustomers();
        setBrowserStore('customers', [customer, ...customers]);
    }
};

export const deleteCustomer = async (id: string) => {
    const db = await initDB();
    if (db) {
        await db.execute("DELETE FROM payments WHERE customer_id = ?", [id]);
        await db.execute("DELETE FROM customers WHERE id = ?", [id]);
    } else {
        const customers = await getCustomers();
        setBrowserStore('customers', customers.filter(c => c.id !== id));
    }
};

export const registerPayment = async (id: string, amount: number) => {
    const db = await initDB();
    if (db) {
        const paymentId = `PAY-${Date.now()}`;
        const date = new Date().toISOString();
        await db.execute("INSERT INTO payments (id, customer_id, date, amount) VALUES (?, ?, ?, ?)", [paymentId, id, date, amount]);
        await db.execute("UPDATE customers SET debt = debt - ? WHERE id = ?", [amount, id]);
    } else {
        const customers = await getCustomers();
        const updated = customers.map(c => {
            if (c.id === id) {
                const payment = { id: `PAY-${Date.now()}`, customer_id: id, date: new Date().toISOString(), amount };
                return { ...c, debt: roundMoney(c.debt - amount), payments: [...(c.payments || []), payment] };
            }
            return c;
        });
        setBrowserStore('customers', updated);
    }
};

export const updateCustomerDebt = async (id: string, delta: number) => {
    const db = await initDB();
    if (db) {
        await db.execute("UPDATE customers SET debt = debt + ? WHERE id = ?", [delta, id]);
    } else {
        const customers = await getCustomers();
        const updated = customers.map(c => c.id === id ? { ...c, debt: roundMoney(c.debt + delta) } : c);
        setBrowserStore('customers', updated);
    }
};

// SALES
export const getSalesHistory = async (): Promise<Sale[]> => {
    const db = await initDB();
    if (db) {
        const rows: any[] = await db.select("SELECT * FROM sales ORDER BY date DESC");
        return rows.map(r => ({
            ...r,
            items: JSON.parse(r.items_json),
            paymentMethod: r.payment_method || 'Cash',
            isCredit: r.is_credit === 1,
            isCancelled: r.is_cancelled === 1,
            customerId: r.customer_id
        }));
    } else {
        return getBrowserStore('sales') || [];
    }
};

export const saveSale = async (sale: Sale) => {
    const db = await initDB();
    if (db) {
        await db.execute(
            "INSERT INTO sales (id, date, items_json, total, total_cost, profit, paid_amount, change_amount, payment_method, customer_id, is_credit, is_cancelled) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
            [sale.id, sale.date, JSON.stringify(sale.items), sale.total, sale.totalCost, sale.profit, sale.paidAmount, sale.changeAmount, sale.paymentMethod, sale.customerId || null, sale.isCredit ? 1 : 0, 0]
        );

        // Stock deduction
        for (const item of sale.items) {
            await db.execute("UPDATE products SET stock = stock - ? WHERE id = ?", [item.quantity, item.productId]);
        }

        if (sale.isCredit && sale.customerId) {
            await updateCustomerDebt(sale.customerId, sale.total);
        }
    } else {
        const sales = await getSalesHistory();
        setBrowserStore('sales', [sale, ...sales]);
        
        // Stock deduction fallback
        const products = await getProducts();
        const updatedProducts = products.map(p => {
            const item = sale.items.find(i => i.productId === p.id);
            return item ? { ...p, stock: p.stock - item.quantity } : p;
        });
        setBrowserStore('products', updatedProducts);

        if (sale.isCredit && sale.customerId) {
            await updateCustomerDebt(sale.customerId, sale.total);
        }
    }

    const shift = await getActiveShift();
    if (shift) {
        if (sale.isCredit) {
            shift.totalCreditSales = roundMoney(shift.totalCreditSales + sale.total);
        } else {
            shift.totalCashSales = roundMoney(shift.totalCashSales + sale.total);
        }
        shift.expectedBalance = roundMoney(shift.openingBalance + shift.totalCashSales);
        await saveActiveShift(shift);
    }
};

export const cancelSale = async (saleId: string) => {
    const db = await initDB();
    if (db) {
        const rows: any[] = await db.select("SELECT * FROM sales WHERE id = ?", [saleId]);
        if (rows.length === 0 || rows[0].is_cancelled === 1) return;

        const sale = rows[0];
        const items = JSON.parse(sale.items_json);

        await db.execute("UPDATE sales SET is_cancelled = 1, cancelled_at = ? WHERE id = ?", [new Date().toISOString(), saleId]);

        // Restore stock
        for (const item of items) {
            await db.execute("UPDATE products SET stock = stock + ? WHERE id = ?", [item.quantity, item.productId]);
        }

        if (sale.is_credit === 1 && sale.customer_id) {
            await updateCustomerDebt(sale.customer_id, -sale.total);
        }
    } else {
        const sales = await getSalesHistory();
        const sale = sales.find(s => s.id === saleId);
        if (!sale || sale.isCancelled) return;

        const updatedSales = sales.map(s => s.id === saleId ? { ...s, isCancelled: true, cancelledAt: new Date().toISOString() } : s);
        setBrowserStore('sales', updatedSales);

        // Restore stock
        const products = await getProducts();
        const updatedProducts = products.map(p => {
            const item = sale.items.find(i => i.productId === p.id);
            return item ? { ...p, stock: p.stock + item.quantity } : p;
        });
        setBrowserStore('products', updatedProducts);

        if (sale.isCredit && sale.customerId) {
            await updateCustomerDebt(sale.customerId, -sale.total);
        }
    }

    const shift = await getActiveShift();
    if (shift) {
        const sales = await getSalesHistory();
        const sale = sales.find(s => s.id === saleId);
        if (sale) {
            if (sale.isCredit) {
                shift.totalCreditSales = roundMoney(shift.totalCreditSales - sale.total);
            } else {
                shift.totalCashSales = roundMoney(shift.totalCashSales - sale.total);
            }
            shift.expectedBalance = roundMoney(shift.openingBalance + shift.totalCashSales);
            await saveActiveShift(shift);
        }
    }
};

// SHIFT
export const getActiveShift = async (): Promise<Shift | null> => {
    const db = await initDB();
    if (db) {
        const rows: any[] = await db.select("SELECT * FROM shifts WHERE is_open = 1 LIMIT 1");
        if (rows.length === 0) return null;
        const r = rows[0];
        return {
            ...r,
            openingBalance: r.opening_balance,
            totalCashSales: r.total_cash_sales,
            totalCreditSales: r.total_credit_sales,
            expectedBalance: r.expected_balance,
            actualBalance: r.actual_balance,
            isOpen: r.is_open === 1,
            startTime: r.start_time,
            endTime: r.end_time
        };
    } else {
        return getBrowserStore('activeShift');
    }
};

export const getShiftHistory = async (): Promise<Shift[]> => {
    const db = await initDB();
    if (db) {
        const rows: any[] = await db.select("SELECT * FROM shifts WHERE is_open = 0 ORDER BY start_time DESC");
        return rows.map(r => ({
            ...r,
            openingBalance: r.opening_balance,
            totalCashSales: r.total_cash_sales,
            totalCreditSales: r.total_credit_sales,
            expectedBalance: r.expected_balance,
            actualBalance: r.actual_balance,
            isOpen: r.is_open === 1,
            startTime: r.start_time,
            endTime: r.end_time
        }));
    } else {
        return getBrowserStore('shiftHistory') || [];
    }
};

export const saveActiveShift = async (shift: Shift) => {
    const db = await initDB();
    if (db) {
        await db.execute(
            "UPDATE shifts SET opening_balance=?, total_cash_sales=?, total_credit_sales=?, expected_balance=?, actual_balance=?, diff=?, is_open=? WHERE id=?",
            [shift.openingBalance, shift.totalCashSales, shift.totalCreditSales, shift.expectedBalance, shift.actualBalance || null, shift.diff || null, shift.isOpen ? 1 : 0, shift.id]
        );
    } else {
        setBrowserStore('activeShift', shift);
    }
};

export const openShift = async (openingBalance: number) => {
    const db = await initDB();
    const newShift: Shift = {
        id: `SHIFT-${Date.now()}`,
        startTime: new Date().toISOString(),
        openingBalance: roundMoney(openingBalance),
        totalCashSales: 0,
        totalCreditSales: 0,
        expectedBalance: roundMoney(openingBalance),
        isOpen: true
    };
    if (db) {
        await db.execute(
            "INSERT INTO shifts (id, start_time, opening_balance, total_cash_sales, total_credit_sales, expected_balance, is_open) VALUES (?, ?, ?, ?, ?, ?, ?)",
            [newShift.id, newShift.startTime, newShift.openingBalance, 0, 0, newShift.openingBalance, 1]
        );
    } else {
        setBrowserStore('activeShift', newShift);
    }
    return newShift;
};

export const closeShift = async (actualBalance: number) => {
    const shift = await getActiveShift();
    if (!shift) return;

    shift.isOpen = false;
    shift.endTime = new Date().toISOString();
    shift.actualBalance = roundMoney(actualBalance);
    shift.diff = roundMoney(actualBalance - shift.expectedBalance);
    
    const db = await initDB();
    if (db) {
        await saveActiveShift(shift);
    } else {
        const history = getBrowserStore('shiftHistory') || [];
        setBrowserStore('shiftHistory', [shift, ...history]);
        setBrowserStore('activeShift', null);
    }
};

// ANALYTICS
export const getTopSellingProducts = async (limit = 5) => {
    const history = await getSalesHistory();
    const nonCancelled = history.filter(s => !s.isCancelled);
    const salesMap: Record<string, {name: string, qty: number, total: number}> = {};
    
    nonCancelled.forEach(sale => {
      sale.items.forEach(item => {
        if (!salesMap[item.productId]) {
          salesMap[item.productId] = { name: item.name, qty: 0, total: 0 };
        }
        salesMap[item.productId].qty += item.quantity;
        salesMap[item.productId].total = roundMoney(salesMap[item.productId].total + item.total);
      });
    });
  
    return Object.values(salesMap)
      .sort((a,b) => b.qty - a.qty)
      .slice(0, limit);
};

export const getPromotions = async () => {
    const db = await initDB();
    if (db) {
        const rows: any[] = await db.select("SELECT * FROM promotions");
        return rows.map(r => ({
            id: r.id,
            name: r.name,
            type: r.type,
            targetId: r.target_id,
            discountValue: r.discount_value,
            discountType: r.discount_type,
            buyQty: r.buy_qty || 1,
            payQty: r.pay_qty || 1,
            isActive: r.is_active === 1
        }));
    } else {
        return getBrowserStore('promotions') || [];
    }
};

export const savePromotion = async (promo: any) => {
    const db = await initDB();
    if (db) {
        await db.execute(
            "INSERT OR REPLACE INTO promotions (id, name, type, target_id, discount_value, discount_type, buy_qty, pay_qty, is_active) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
            [promo.id, promo.name, promo.type, promo.targetId, promo.discountValue, promo.discountType, promo.buyQty || 1, promo.payQty || 1, promo.isActive ? 1 : 0]
        );
    } else {
        const promos = await getPromotions();
        const index = promos.findIndex((p: any) => p.id === promo.id);
        if (index >= 0) promos[index] = promo;
        else promos.push(promo);
        setBrowserStore('promotions', promos);
    }
};

export const deletePromotion = async (id: string) => {
    const db = await initDB();
    if (db) {
        await db.execute("DELETE FROM promotions WHERE id = ?", [id]);
    } else {
        const promos = await getPromotions();
        setBrowserStore('promotions', promos.filter((p: any) => p.id !== id));
    }
};
