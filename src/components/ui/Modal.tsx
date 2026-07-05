'use client';

import React from 'react';
import { X } from 'phosphor-react';

type ModalProps = {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  icon?: React.ReactNode;
  children: React.ReactNode;
  maxWidth?: string;
  zIndex?: number;
};

export const Modal = ({
  isOpen,
  onClose,
  title,
  icon,
  children,
  maxWidth = '500px',
  zIndex = 1100,
}: ModalProps) => {
  if (!isOpen) return null;

  return (
    <div
      className="nv-modal-overlay"
      style={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex,
        backdropFilter: 'blur(6px)',
        WebkitBackdropFilter: 'blur(6px)',
      }}
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div
        className="nv-modal-card"
        style={{
          backgroundColor: 'var(--bg-secondary)',
          padding: '32px',
          borderRadius: 'var(--radius-lg)',
          width: '100%',
          maxWidth,
          border: '1px solid var(--border-color)',
          boxShadow: 'var(--shadow-lg)',
          maxHeight: '90vh',
          overflowY: 'auto',
        }}
      >
        {/* Header */}
        <div style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '24px',
        }}>
          <h2 style={{
            fontSize: '20px',
            fontWeight: 'bold',
            display: 'flex',
            alignItems: 'center',
            gap: '10px',
            color: 'var(--text-primary)',
          }}>
            {icon}
            {title}
          </h2>
          <button
            type="button"
            onClick={onClose}
            style={{
              width: '36px',
              height: '36px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              borderRadius: 'var(--radius-sm)',
              border: '1px solid var(--border-color)',
              backgroundColor: 'transparent',
              color: 'var(--text-secondary)',
              cursor: 'pointer',
              transition: 'var(--transition)',
              flexShrink: 0,
            }}
          >
            <X size={20} weight="regular" />
          </button>
        </div>

        {/* Body */}
        {children}
      </div>
    </div>
  );
};
