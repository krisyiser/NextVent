'use client';

import React from 'react';

type Column = {
  key: string;
  label: string;
  align?: 'left' | 'center' | 'right';
  width?: string;
};

type DataTableProps<T> = {
  columns: Column[];
  data: T[];
  renderRow: (item: T, index: number) => React.ReactNode;
  emptyMessage?: string;
  style?: React.CSSProperties;
};

export function DataTable<T>({
  columns,
  data,
  renderRow,
  emptyMessage = 'Sin datos disponibles.',
  style,
}: DataTableProps<T>) {
  return (
    <table style={{
      width: '100%',
      borderCollapse: 'collapse',
      backgroundColor: 'var(--bg-secondary)',
      borderRadius: 'var(--radius-lg)',
      overflow: 'hidden',
      ...style,
    }}>
      <thead>
        <tr style={{
          textAlign: 'left',
          borderBottom: '1px solid var(--border-color)',
          backgroundColor: 'var(--bg-tertiary)',
        }}>
          {columns.map(col => (
            <th
              key={col.key}
              style={{
                padding: '14px 16px',
                fontSize: '12px',
                fontWeight: 700,
                color: 'var(--text-secondary)',
                textTransform: 'uppercase',
                letterSpacing: '0.3px',
                textAlign: col.align || 'left',
                width: col.width,
              }}
            >
              {col.label}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {data.length === 0 ? (
          <tr>
            <td
              colSpan={columns.length}
              style={{
                padding: '40px',
                textAlign: 'center',
                color: 'var(--text-muted)',
                fontSize: '14px',
              }}
            >
              {emptyMessage}
            </td>
          </tr>
        ) : (
          data.map((item, idx) => renderRow(item, idx))
        )}
      </tbody>
    </table>
  );
}
