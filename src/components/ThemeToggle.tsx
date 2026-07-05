// src/components/ThemeToggle.tsx
'use client';

import React from 'react';
import { Sun, Moon } from 'phosphor-react';
import { useTheme } from './ThemeProvider';

/**
 * ThemeToggle — toggles between light and dark mode.
 * Consumes the global ThemeProvider context to ensure state persistence across pages.
 */
export const ThemeToggle: React.FC = () => {
  const { theme, toggleTheme } = useTheme();

  return (
    <button
      onClick={toggleTheme}
      className="nav-item"
      style={{ border: 'none', background: 'none', cursor: 'pointer' }}
      title={theme === 'dark' ? 'Cambiar a Modo Claro' : 'Cambiar a Modo Oscuro'}
    >
      {theme === 'dark' ? <Sun size={24} weight="regular" /> : <Moon size={24} weight="regular" />}
    </button>
  );
};
