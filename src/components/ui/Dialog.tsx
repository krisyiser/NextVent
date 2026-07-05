import * as DialogPrimitive from '@radix-ui/react-dialog';
import { X } from 'phosphor-react';
import React from 'react';

interface DialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  children: React.ReactNode;
}

export const Dialog: React.FC<DialogProps> = ({ open, onOpenChange, title, children }) => (
  <DialogPrimitive.Root open={open} onOpenChange={onOpenChange}>
    <DialogPrimitive.Portal>
      <DialogPrimitive.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm" />
      <DialogPrimitive.Content className="fixed top-1/2 left-1/2 max-h-[85vh] w-[90vw] max-w-lg -translate-x-1/2 -translate-y-1/2 rounded-lg bg-white dark:bg-slate-800 p-6 shadow-lg outline-none">
        <div className="flex items-center justify-between mb-4">
          <DialogPrimitive.Title className="text-lg font-semibold text-slate-900 dark:text-slate-100">{title}</DialogPrimitive.Title>
          <DialogPrimitive.Close className="text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200">
            <X size={20} weight="regular" />
          </DialogPrimitive.Close>
        </div>
        {children}
      </DialogPrimitive.Content>
    </DialogPrimitive.Portal>
  </DialogPrimitive.Root>
);
