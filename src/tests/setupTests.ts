// src/tests/setupTests.ts
import '@testing-library/jest-dom';

// Simple in-memory mock storage for testing database calls
let mockDbStore: Record<string, any[]> = {
  products: [],
  customers: [],
  sales: [],
  shifts: [],
  promotions: [],
  settings: [],
};

const executeMock = jest.fn().mockImplementation(async (sql: string, params: any[] = []) => {
  const normalizedSql = sql.toLowerCase();
  
  if (normalizedSql.startsWith('insert or replace into products') || normalizedSql.startsWith('insert into products')) {
    // [id, barcode, name, cost, price, wholesalePrice, wholesaleThreshold, stock, category, unit, expiresSoon]
    const [id, barcode, name, cost, price, wholesalePrice, wholesaleThreshold, stock, category, unit, expiresSoon] = params;
    mockDbStore.products = mockDbStore.products.filter(p => p.id !== id);
    mockDbStore.products.push({ id, barcode, name, cost, price, wholesalePrice, wholesaleThreshold, stock, category, unit, expiresSoon });
  } else if (normalizedSql.startsWith('delete from products')) {
    const [id] = params;
    mockDbStore.products = mockDbStore.products.filter(p => p.id !== id);
  } else if (normalizedSql.startsWith('delete from') && normalizedSql.includes('products')) {
    mockDbStore.products = [];
  }
  return { rowsAffected: 1, lastInsertId: 1 };
});

const selectMock = jest.fn().mockImplementation(async (sql: string, params: any[] = []) => {
  const normalizedSql = sql.toLowerCase();
  
  if (normalizedSql.includes('select') && normalizedSql.includes('from products')) {
    return mockDbStore.products;
  }
  return [];
});

// Mock Tauri plugin-sql database driver
jest.mock('@tauri-apps/plugin-sql', () => {
  const mockDb = {
    execute: executeMock,
    select: selectMock,
  };
  return {
    __esModule: true,
    default: {
      load: jest.fn().mockResolvedValue(mockDb),
    },
  };
});

// Mock Tauri IPC invoke core
jest.mock('@tauri-apps/api/core', () => ({
  invoke: jest.fn().mockResolvedValue({}),
}));
