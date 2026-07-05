'use client';

import React, { createContext, useContext, useState, useEffect, useRef } from 'react';
import { invoke } from '@tauri-apps/api/core';
import { useTheme } from './ThemeProvider';

interface WhatsappContextType {
  isOpen: boolean;
  width: number;
  toggleOpen: () => void;
  setOpen: (open: boolean) => void;
  setWidth: (width: number) => void;
}

const WhatsappContext = createContext<WhatsappContextType | undefined>(undefined);

export const useWhatsappPanel = () => {
  const context = useContext(WhatsappContext);
  if (!context) throw new Error('useWhatsappPanel must be used within a WhatsappPanelProvider');
  return context;
};

export const WhatsappPanelProvider = ({ children }: { children: React.ReactNode }) => {
  const { sidebarPosition } = useTheme();
  const [isOpen, setIsOpen] = useState(false);
  const [width, setWidth] = useState(() => {
    if (typeof window !== 'undefined') {
      const remainingWidth = window.innerWidth - 80;
      const halfWidth = Math.round(remainingWidth / 2);
      return Math.max(350, Math.min(1200, halfWidth));
    }
    return 600;
  });
  const [isDragging, setIsDragging] = useState(false);
  const isDraggingRef = useRef(false);
  const hasResizedRef = useRef(false);

  // Sync layout state with Tauri backend (pinned is always false now)
  const updateTauriLayout = async (openState: boolean, widthVal: number) => {
    try {
      await invoke('set_whatsapp_layout', {
        open: openState,
        width: widthVal,
        pinned: false,
        sidebarPosition: sidebarPosition,
      });
    } catch (e) {
      console.error('Failed to sync tauri layout:', e);
    }
  };

  // Set default width based on current window size on mount
  useEffect(() => {
    updateTauriLayout(isOpen, width);
  }, []);

  const toggleOpen = () => {
    const next = !isOpen;
    setIsOpen(next);

    let currentWidth = width;
    if (!hasResizedRef.current && next && typeof window !== 'undefined') {
      const remainingWidth = window.innerWidth - 80;
      const halfWidth = Math.round(remainingWidth / 2);
      currentWidth = Math.max(350, Math.min(1200, halfWidth));
      setWidth(currentWidth);
    }

    updateTauriLayout(next, currentWidth);
  };

  const handleSetOpen = (open: boolean) => {
    setIsOpen(open);
    
    let currentWidth = width;
    if (!hasResizedRef.current && open && typeof window !== 'undefined') {
      const remainingWidth = window.innerWidth - 80;
      const halfWidth = Math.round(remainingWidth / 2);
      currentWidth = Math.max(350, Math.min(1200, halfWidth));
      setWidth(currentWidth);
    }

    updateTauriLayout(open, currentWidth);
  };

  const handleSetWidth = (newWidth: number) => {
    hasResizedRef.current = true; // Mark as manually resized
    const clamped = Math.max(350, Math.min(1200, newWidth));
    setWidth(clamped);
    updateTauriLayout(isOpen, clamped);
  };

  // Drag-to-resize handlers
  const handleMouseDown = (e: React.MouseEvent) => {
    e.preventDefault();
    isDraggingRef.current = true;
    setIsDragging(true);
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  };

  useEffect(() => {
    const handleMouseMove = (e: MouseEvent) => {
      if (!isDraggingRef.current) return;
      const xOffset = sidebarPosition === 'left' ? 80 : 0;
      const calculatedWidth = e.clientX - xOffset;
      handleSetWidth(calculatedWidth);
    };

    const handleMouseUp = () => {
      if (!isDraggingRef.current) return;
      isDraggingRef.current = false;
      setIsDragging(false);
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    };

    window.addEventListener('mousemove', handleMouseMove);
    window.addEventListener('mouseup', handleMouseUp);
    return () => {
      window.removeEventListener('mousemove', handleMouseMove);
      window.removeEventListener('mouseup', handleMouseUp);
    };
  }, [width, isOpen]);

  // Auto-close on click outside in unpinned (overlay) mode
  useEffect(() => {
    if (!isOpen) return;

    const handleOutsideClick = (e: MouseEvent) => {
      const xOffset = sidebarPosition === 'left' ? 80 : 0;
      if (e.clientX > xOffset + width + 40) {
        handleSetOpen(false);
      }
    };

    window.addEventListener('mousedown', handleOutsideClick);
    return () => window.removeEventListener('mousedown', handleOutsideClick);
  }, [isOpen, width]);

  const handleLeftPosition = (sidebarPosition === 'left' ? 80 : 0) + width;
  const handleTopPosition = sidebarPosition === 'top' ? '64px' : '0';
  const handleHeight = sidebarPosition === 'top' || sidebarPosition === 'bottom' ? 'calc(100vh - 64px)' : '100vh';

  return (
    <WhatsappContext.Provider value={{ isOpen, width, toggleOpen, setOpen: handleSetOpen, setWidth: handleSetWidth }}>
      {children}
      
      {isOpen && (
        <>
          {/* Resize handle bar (vertical line) on the right edge of WhatsApp */}
          <div
            style={{
              position: 'fixed',
              top: handleTopPosition,
              left: `${handleLeftPosition}px`,
              width: '6px',
              height: handleHeight,
              cursor: 'col-resize',
              zIndex: 99999,
              background: isDragging ? '#2563eb' : 'transparent',
              borderRight: '1px solid rgba(0,0,0,0.1)',
              transition: 'background 0.2s',
            }}
            onMouseDown={handleMouseDown}
            title="Arrastra para cambiar el tamaño de WhatsApp"
          />
        </>
      )}
    </WhatsappContext.Provider>
  );
};
