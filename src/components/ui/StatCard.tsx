'use client';

import React from 'react';

type StatCardProps = {
  icon: React.ReactNode;
  iconBg?: string;
  iconColor?: string;
  label: string;
  value: string;
  valueColor?: string;
  children?: React.ReactNode;
};

export const StatCard = ({
  icon,
  iconBg = 'rgba(59, 130, 246, 0.1)',
  iconColor = 'var(--accent-primary)',
  label,
  value,
  valueColor,
  children,
}: StatCardProps) => {
  return (
    <div style={{
      backgroundColor: 'var(--bg-secondary)',
      padding: '20px',
      borderRadius: 'var(--radius-lg)',
      border: '1px solid var(--border-color)',
      display: 'flex',
      alignItems: 'center',
      gap: '16px',
    }}>
      <div style={{
        padding: '12px',
        backgroundColor: iconBg,
        color: iconColor,
        borderRadius: 'var(--radius-md)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        flexShrink: 0,
      }}>
        {icon}
      </div>
      <div style={{ flex: 1 }}>
        <div style={{ color: 'var(--text-secondary)', fontSize: '12px', textTransform: 'uppercase', fontWeight: 600, letterSpacing: '0.3px' }}>
          {label}
        </div>
        <div style={{ fontSize: '20px', fontWeight: 'bold', color: valueColor || 'var(--text-primary)' }}>
          {value}
        </div>
        {children}
      </div>
    </div>
  );
};
