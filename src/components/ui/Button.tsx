'use client';

import React from 'react';

// ─── Contrast Utility ──────────────────────────────────────
function getContrastText(hex: string): string {
  const c = hex.replace('#', '');
  const r = parseInt(c.substring(0, 2), 16);
  const g = parseInt(c.substring(2, 4), 16);
  const b = parseInt(c.substring(4, 6), 16);
  // Relative luminance (WCAG)
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
  return luminance > 0.5 ? '#111827' : '#FFFFFF';
}

export type ButtonVariant = 'primary' | 'success' | 'danger' | 'warning' | 'ghost' | 'outline';
export type ButtonSize = 'sm' | 'md' | 'lg';

type ButtonProps = {
  variant?: ButtonVariant;
  size?: ButtonSize;
  disabled?: boolean;
  fullWidth?: boolean;
  icon?: React.ReactNode;
  children: React.ReactNode;
  onClick?: (e: React.MouseEvent<HTMLButtonElement>) => void;
  type?: 'button' | 'submit' | 'reset';
  title?: string;
  style?: React.CSSProperties;
  className?: string;
};

const VARIANT_MAP: Record<ButtonVariant, { bg: string; border: string; colorVar?: string; hoverBg?: string }> = {
  primary:  { bg: 'var(--accent-primary)', border: 'var(--accent-hover)', hoverBg: 'var(--accent-hover)' },
  success:  { bg: 'var(--accent-success)', border: 'var(--accent-success)' },
  danger:   { bg: 'var(--accent-danger)', border: 'var(--accent-danger)' },
  warning:  { bg: 'var(--accent-warning)', border: 'var(--accent-warning)' },
  ghost:    { bg: 'transparent', border: 'transparent', colorVar: 'var(--text-secondary)' },
  outline:  { bg: 'transparent', border: 'var(--border-color)', colorVar: 'var(--text-primary)' },
};

const SIZE_MAP: Record<ButtonSize, { padding: string; fontSize: string; height: string }> = {
  sm: { padding: '6px 12px', fontSize: '12px', height: '32px' },
  md: { padding: '10px 20px', fontSize: '14px', height: '40px' },
  lg: { padding: '16px 28px', fontSize: '16px', height: '48px' },
};

export const Button = ({
  variant = 'primary',
  size = 'md',
  disabled = false,
  fullWidth = false,
  icon,
  children,
  onClick,
  type = 'button',
  title,
  style: externalStyle,
  className,
}: ButtonProps) => {
  const v = VARIANT_MAP[variant];
  const s = SIZE_MAP[size];

  // For solid variants, compute contrast text color dynamically
  const isSolid = variant !== 'ghost' && variant !== 'outline';
  const textColor = isSolid ? 'var(--text-on-accent, #FFFFFF)' : (v.colorVar || 'var(--text-primary)');

  return (
    <button
      type={type}
      disabled={disabled}
      title={title}
      className={className}
      onClick={onClick}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: '8px',
        padding: s.padding,
        minHeight: s.height,
        fontSize: s.fontSize,
        fontWeight: 600,
        borderRadius: 'var(--radius-md)',
        border: `1px solid ${v.border}`,
        backgroundColor: v.bg,
        color: textColor,
        cursor: disabled ? 'not-allowed' : 'pointer',
        opacity: disabled ? 0.5 : 1,
        transition: 'var(--transition)',
        width: fullWidth ? '100%' : 'auto',
        whiteSpace: 'nowrap',
        lineHeight: 1.2,
        fontFamily: 'inherit',
        ...externalStyle,
      }}
    >
      {icon}
      {children}
    </button>
  );
};
