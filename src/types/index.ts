export type Product = {
  id: string;
  name: string;
  cost: number;
  price: number;
  wholesalePrice?: number;
  wholesaleThreshold?: number;
  stock: number;
  category: string;
  unit: string;
  barcode?: string;
  expiresSoon?: boolean;
};

// El Snapshot guarda los valores exactos al momento de la venta
export type SaleItemSnapshot = {
  productId: string;
  name: string;
  cost: number;
  price: number;
  quantity: number;
  unit: string;
  total: number;
  isWholesale: boolean;
};

export type TicketItem = Product & {
  quantity: number;
};

export type CustomerPayment = {
  id: string;
  date: string;
  amount: number;
};

export type Customer = {
  id: string;
  name: string;
  phone?: string;
  debt: number;
  payments: CustomerPayment[];
};

export type Sale = {
  id: string;
  date: string;
  items: SaleItemSnapshot[];
  total: number;
  totalCost: number;
  profit: number;
  paidAmount: number;
  changeAmount: number;
  customerId?: string;
  isCredit: boolean;
  isCancelled?: boolean;
  cancelledAt?: string;
};

export type Shift = {
  id: string;
  startTime: string;
  endTime?: string;
  openingBalance: number;
  totalCashSales: number;
  totalCreditSales: number;
  expectedBalance: number;
  actualBalance?: number;
  diff?: number;
  isOpen: boolean;
};
