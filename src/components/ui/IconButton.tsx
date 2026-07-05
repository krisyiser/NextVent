'use client';

import React from 'react';

type IconButtonVariant = 'default' | 'danger' | 'accent' | 'warning' | 'success';

type IconButtonProps = {
  children: React.ReactNode;
  onClick?: (e: React.MouseEvent<HTMLButtonElement>) => void;
  variant?: IconButtonVariant;
  title?: string;
  disabled?: boolean;
  size?: number;
  style?: React.CSSProperties;
  className?: string;
};

const VARIANT_COLORS: Record<IconButtonVariant, { color: string; hoverBg: string }> = {
  default: { color: 'var(--text-secondary)', hoverBg: 'var(--bg-tertiary)' },
  danger:  { color: 'var(--accent-danger)', hoverBg: 'rgba(239, 68, 68, 0.1)' },
  accent:  { color: 'var(--accent-primary)', hoverBg: 'rgba(59, 130, 246, 0.1)' },
  warning: { color: 'var(--accent-warning)', hoverBg: 'rgba(245, 158, 11, 0.1)' },
  success: { color: 'var(--accent-success)', hoverBg: 'rgba(16, 185, 129, 0.1)' },
};

export const IconButton = ({
  children,
  onClick,
  variant = 'default',
  title,
  disabled = false,
  size = 40,
  style: externalStyle,
  className,
}: IconButtonProps) => {
  const v = VARIANT_COLORS[variant];

  return (
    <button
      type="button"
      onClick={onClick}
      title={title}
      disabled={disabled}
      className={className}
      style={{
        width: `${size}px`,
        height: `${size}px`,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        borderRadius: 'var(--radius-sm)',
        border: '1px solid var(--border-color)',
        backgroundColor: 'transparent',
        color: v.color,
        cursor: disabled ? 'not-allowed' : 'pointer',
        opacity: disabled ? 0.5 : 1,
        transition: 'var(--transition)',
        flexShrink: 0,
        ...externalStyle,
      }}
    >
      {children}
    </button>
  );
};
