'use client';

import React from 'react';

type BadgeVariant = 'info' | 'success' | 'danger' | 'warning' | 'neutral';

type BadgeProps = {
  children: React.ReactNode;
  variant?: BadgeVariant;
  style?: React.CSSProperties;
};

const VARIANT_STYLES: Record<BadgeVariant, { bg: string; color: string; border: string }> = {
  info:    { bg: 'rgba(59, 130, 246, 0.1)', color: 'var(--accent-primary)', border: 'rgba(59, 130, 246, 0.2)' },
  success: { bg: 'rgba(16, 185, 129, 0.1)', color: 'var(--accent-success)', border: 'rgba(16, 185, 129, 0.2)' },
  danger:  { bg: 'rgba(239, 68, 68, 0.15)', color: 'var(--accent-danger)', border: 'rgba(239, 68, 68, 0.2)' },
  warning: { bg: 'rgba(245, 158, 11, 0.1)', color: 'var(--accent-warning)', border: 'rgba(245, 158, 11, 0.2)' },
  neutral: { bg: 'var(--bg-tertiary)', color: 'var(--text-muted)', border: 'var(--border-color)' },
};

export const Badge = ({ children, variant = 'info', style }: BadgeProps) => {
  const v = VARIANT_STYLES[variant];
  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: '4px',
        padding: '2px 8px',
        borderRadius: '4px',
        fontSize: '10px',
        fontWeight: 700,
        textTransform: 'uppercase',
        letterSpacing: '0.3px',
        backgroundColor: v.bg,
        color: v.color,
        border: `1px solid ${v.border}`,
        whiteSpace: 'nowrap',
        ...style,
      }}
    >
      {children}
    </span>
  );
};
