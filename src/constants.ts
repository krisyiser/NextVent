import { Product } from "./types";

export const INITIAL_PRODUCTS: Product[] = [
  { id: '1', barcode: '7501234567890', name: 'Coca-Cola 600ml', cost: 12.5, price: 18, stock: 45, category: 'Bebidas', unit: 'Pza' },
  { id: '2', barcode: '7509876543210', name: 'Leche Lala Entera 1L', cost: 22, price: 28, stock: 12, category: 'Lácteos', unit: 'Pza', expiresSoon: true },
  { id: '3', barcode: '7501000000003', name: 'Pan Bimbo Blanco', cost: 34, price: 45, stock: 8, category: 'Abarrotes', unit: 'Pza' },
  { id: '4', barcode: '7502000000004', name: 'Paracetamol 500mg', cost: 18, price: 35, stock: 68, category: 'Farmacia', unit: 'Caja' },
  { id: '5', barcode: '7503000000005', name: 'Sabritas Saladas 40g', cost: 10, price: 16, stock: 50, category: 'Botanas', unit: 'Pza' },
  { id: '6', barcode: '7504000000006', name: 'Huevo San Juan (Kilo)', cost: 32, price: 42, stock: 30, category: 'Abarrotes', unit: 'Kg' },
  { id: '7', barcode: '7505000000007', name: 'Gatorade Naranja 1L', cost: 21, price: 29, stock: 24, category: 'Bebidas', unit: 'Pza' },
  { id: '8', barcode: '7506000000008', name: 'Aspirina Protect', cost: 38, price: 55, stock: 15, category: 'Farmacia', unit: 'Caja', expiresSoon: true },
  { id: '9', barcode: '7507000000009', name: 'Jabón Zote Rosa', cost: 14, price: 22, stock: 40, category: 'Limpieza', unit: 'Pza' },
  { id: '10', barcode: '7508000000010', name: 'Fabuloso Lavanda 1L', cost: 23, price: 32, stock: 18, category: 'Limpieza', unit: 'Pza' },
];

export const CATEGORIES = ['Todos', 'Abarrotes', 'Bebidas', 'Lácteos', 'Botanas', 'Farmacia', 'Limpieza'];
