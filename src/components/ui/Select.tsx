'use client';

import React from 'react';

type SelectOption = {
  value: string;
  label: string;
};

type SelectProps = {
  label?: string;
  value: string;
  onChange: (e: React.ChangeEvent<HTMLSelectElement>) => void;
  options: SelectOption[];
  icon?: React.ReactNode;
  hint?: string;
  required?: boolean;
  disabled?: boolean;
  placeholder?: string;
  style?: React.CSSProperties;
};

export const Select = ({
  label,
  value,
  onChange,
  options,
  icon,
  hint,
  required,
  disabled,
  placeholder,
  style,
}: SelectProps) => {
  return (
    <div style={{ marginBottom: '16px', ...style }}>
      {label && (
        <label style={{
          display: 'flex', alignItems: 'center', gap: '6px',
          fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)',
          marginBottom: '6px', textTransform: 'uppercase', letterSpacing: '0.5px'
        }}>
          {icon} {label}
        </label>
      )}
      <select
        value={value}
        onChange={onChange}
        required={required}
        disabled={disabled}
        style={{
          width: '100%',
          padding: '12px',
          backgroundColor: 'var(--bg-tertiary)',
          border: '1px solid var(--border-color)',
          borderRadius: 'var(--radius-md)',
          color: 'var(--text-primary)',
          fontSize: '14px',
          outline: 'none',
          cursor: 'pointer',
          fontFamily: 'inherit',
          appearance: 'auto',
        }}
      >
        {placeholder && <option value="">{placeholder}</option>}
        {options.map(opt => (
          <option key={opt.value} value={opt.value}>{opt.label}</option>
        ))}
      </select>
      {hint && (
        <div style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '4px' }}>
          {hint}
        </div>
      )}
    </div>
  );
};
