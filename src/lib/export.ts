// src/lib/export.ts
// Export app data as JSON or CSV using Tauri file system API
import { getProductsPaginated, getCustomers, getSales, getPromotions, getShifts } from './storage';
import { save } from '@tauri-apps/plugin-dialog';
import { writeTextFile } from '@tauri-apps/plugin-fs';

// Helper to convert array of objects to CSV
const arrayToCSV = (data: Record<string, unknown>[]): string => {
  if (data.length === 0) return '';
  const headers = Object.keys(data[0]);
  const rows = data.map((row) =>
    headers.map((h) => JSON.stringify(row[h] ?? '')).join(',')
  );
  return [headers.join(','), ...rows].join('\n');
};

/** Collect all products in paginated batches */
const collectAllProducts = async (): Promise<unknown[]> => {
  const pageSize = 5000;
  let page = 1;
  const all: unknown[] = [];
  while (true) {
    const batch = await getProductsPaginated(page, pageSize);
    if (batch.length === 0) break;
    all.push(...batch);
    page++;
    if (batch.length < pageSize) break;
  }
  return all;
};

/** Export the full dataset as a JSON file via Tauri save dialog */
export const exportJSONFile = async (): Promise<void> => {
  const products = await collectAllProducts();
  const customers = await getCustomers();
  const sales = await getSales();
  const promotions = await getPromotions();
  const shifts = await getShifts();

  const payload = { products, customers, sales, promotions, shifts };
  const jsonStr = JSON.stringify(payload, null, 2);

  const filePath = await save({
    title: 'Exportar datos como JSON',
    defaultPath: 'nextvent_export.json',
    filters: [{ name: 'JSON', extensions: ['json'] }],
  });

  if (filePath) {
    await writeTextFile(filePath, jsonStr);
  }
};

/** Export the full dataset as a CSV file via Tauri save dialog */
export const exportCSVFile = async (): Promise<void> => {
  const products = await collectAllProducts();
  const customers = await getCustomers();
  const sales = await getSales();
  const promotions = await getPromotions();
  const shifts = await getShifts();

  const csvParts = [
    '--- PRODUCTOS ---',
    arrayToCSV(products as Record<string, unknown>[]),
    '\n--- CLIENTES ---',
    arrayToCSV(customers as unknown as Record<string, unknown>[]),
    '\n--- VENTAS ---',
    arrayToCSV(sales as unknown as Record<string, unknown>[]),
    '\n--- PROMOCIONES ---',
    arrayToCSV(promotions as unknown as Record<string, unknown>[]),
    '\n--- TURNOS ---',
    arrayToCSV(shifts as unknown as Record<string, unknown>[]),
  ];

  const csvContent = csvParts.join('\n');

  const filePath = await save({
    title: 'Exportar datos como CSV',
    defaultPath: 'nextvent_export.csv',
    filters: [{ name: 'CSV', extensions: ['csv'] }],
  });

  if (filePath) {
    await writeTextFile(filePath, csvContent);
  }
};
