// src/tests/storage.test.ts
import { getProducts, addProduct, clearAllData } from '../lib/storage';

describe('Storage utilities', () => {
  beforeAll(async () => {
    // Ensure a clean state before tests
    await clearAllData();
  });

  test('add and retrieve product', async () => {
    const product = { id: 'test-1', name: 'Test Product', price: 10, category: 'Test', stock: 5 } as any;
    await addProduct(product);
    const products = await getProducts();
    expect(products.find(p => p.id === 'test-1')).toBeDefined();
  });
});
