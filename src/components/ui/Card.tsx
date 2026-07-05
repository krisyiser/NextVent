'use client';

import React from 'react';

type CardProps = {
  children: React.ReactNode;
  padding?: string;
  interactive?: boolean;
  selected?: boolean;
  onClick?: () => void;
  style?: React.CSSProperties;
  className?: string;
};

export const Card = ({
  children,
  padding = '20px',
  interactive = false,
  selected = false,
  onClick,
  style,
  className,
}: CardProps) => {
  return (
    <div
      className={className}
      onClick={onClick}
      style={{
        backgroundColor: 'var(--bg-secondary)',
        borderRadius: 'var(--radius-lg)',
        border: selected
          ? '2px solid var(--accent-primary)'
          : '1px solid var(--border-color)',
        padding,
        cursor: interactive || onClick ? 'pointer' : 'default',
        transition: 'var(--transition)',
        ...style,
      }}
    >
      {children}
    </div>
  );
};
