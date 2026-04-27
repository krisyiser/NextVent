'use client';

import React from 'react';
import { ShoppingCart, Package } from 'lucide-react';
import { Product } from '@/types';

type ProductCardProps = {
  product: Product;
  onAdd: (product: Product) => void;
};

// Dummy icon for beverages since Baggage is not in lucide by default
const ShoppingBag = ({ size }: { size: number }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
    <line x1="3" y1="6" x2="21" y2="6"></line>
    <path d="M16 10a4 4 0 0 1-8 0"></path>
  </svg>
);

export const ProductCard = ({ product, onAdd }: ProductCardProps) => {
  return (
    <div className="product-card" onClick={() => onAdd(product)}>
      <div className="product-icon">
        {product.category === 'Bebidas' ? <ShoppingBag size={24} /> :
          product.category === 'Farmacia' ? <Package size={24} /> :
            <ShoppingCart size={24} />}
      </div>
      <div className="product-name">{product.name}</div>
      <div className="product-price">${product.price.toFixed(2)}</div>
      <div className={`product-stock ${product.stock <= 5 ? 'low' : ''}`}>
        Stock: {product.stock} {product.unit}
        {product.expiresSoon && <span className="text-warning" style={{ marginLeft: 8, fontSize: 10 }}>⚠️ Próx. a caducar</span>}
      </div>
    </div>
  );
};
