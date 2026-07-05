// src/tests/components.test.tsx
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { ProductCard } from '../components/ProductCard'; // assuming component exists

describe('UI component tests', () => {
  test('renders product name', () => {
    const product = { id: 'p1', name: 'Demo', price: 5, category: 'Test', stock: 10 } as any;
    render(<ProductCard product={product} onAdd={jest.fn()} />);
    expect(screen.getByText('Demo')).toBeInTheDocument();
  });
});
