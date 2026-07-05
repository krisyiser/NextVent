'use client';

import React from 'react';

type InputProps = {
  label?: string;
  value: string;
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  placeholder?: string;
  type?: string;
  icon?: React.ReactNode;
  hint?: string;
  required?: boolean;
  autoFocus?: boolean;
  disabled?: boolean;
  readOnly?: boolean;
  step?: string;
  min?: string;
  max?: string;
  style?: React.CSSProperties;
  inputStyle?: React.CSSProperties;
  containerStyle?: React.CSSProperties;
  name?: string;
  onSubmit?: (e: React.FormEvent) => void;
};

export const Input = ({
  label,
  value,
  onChange,
  placeholder,
  type = 'text',
  icon,
  hint,
  required,
  autoFocus,
  disabled,
  readOnly,
  step,
  min,
  max,
  style,
  inputStyle,
  containerStyle,
  name,
}: InputProps) => {
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
      <div style={{
        display: 'flex',
        alignItems: 'center',
        backgroundColor: 'var(--bg-tertiary)',
        borderRadius: 'var(--radius-md)',
        border: '1px solid var(--border-color)',
        padding: icon && !label ? '0 12px' : '0',
        transition: 'var(--transition)',
        ...containerStyle,
      }}>
        <input
          name={name}
          type={type}
          value={value}
          onChange={onChange}
          placeholder={placeholder}
          required={required}
          autoFocus={autoFocus}
          disabled={disabled}
          readOnly={readOnly}
          step={step}
          min={min}
          max={max}
          style={{
            width: '100%',
            padding: '12px',
            backgroundColor: 'transparent',
            border: 'none',
            color: 'var(--text-primary)',
            fontSize: '14px',
            outline: 'none',
            fontFamily: 'inherit',
            ...inputStyle,
          }}
        />
      </div>
      {hint && (
        <div style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '4px' }}>
          {hint}
        </div>
      )}
    </div>
  );
};
