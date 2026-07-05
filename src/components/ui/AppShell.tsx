'use client';

import React from 'react';
import { Sidebar } from '../Sidebar';

type AppShellProps = {
  activeModule?: 'pos' | 'inventory' | 'customers' | 'reports' | 'history' | 'promotions' | 'reloj' | 'fiscal';
  children: React.ReactNode;
  withoutSidebar?: boolean;
  className?: string;
  mainStyle?: React.CSSProperties;
};

/**
 * AppShell — The centralized layout wrapper for all pages.
 * 
 * This component ALWAYS renders the correct app-container + sidebar + main-content
 * structure, ensuring that sidebar position (left/right/top/bottom) is consistently
 * applied across all routes.
 * 
 * Usage:
 *   <AppShell activeModule="pos">
 *     <header className="top-bar">...</header>
 *     <div className="pos-layout">...</div>
 *   </AppShell>
 */
export const AppShell = ({
  activeModule = 'pos',
  children,
  withoutSidebar = false,
  className = '',
  mainStyle,
}: AppShellProps) => {
  return (
    <div className={`app-container ${className}`}>
      {!withoutSidebar && <Sidebar activeModule={activeModule} />}
      <main className="main-content" style={mainStyle}>
        {children}
      </main>
    </div>
  );
};
