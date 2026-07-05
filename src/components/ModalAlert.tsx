// src/components/ModalAlert.tsx
import React, { ReactNode } from 'react';
import * as Dialog from '@radix-ui/react-dialog';

interface ModalAlertProps {
  isOpen: boolean;
  title?: string;
  children: ReactNode;
  onClose: () => void;
  onConfirm?: () => void; // optional confirm handler for confirm dialogs
  confirmLabel?: string;
  cancelLabel?: string;
}

export const ModalAlert = ({
  isOpen,
  title = 'Atención',
  children,
  onClose,
  onConfirm,
  confirmLabel = 'Aceptar',
  cancelLabel = 'Cancelar',
}: ModalAlertProps) => {
  return (
    <Dialog.Root open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black bg-opacity-40 backdrop-blur-sm" />
        <Dialog.Content className="fixed top-1/2 left-1/2 max-w-md w-full -translate-x-1/2 -translate-y-1/2 rounded-lg bg-white p-6 shadow-xl dark:bg-gray-800 dark:text-gray-100 border border-black">
          <Dialog.Title className="mb-4 text-lg font-semibold text-gray-900 dark:text-gray-100">
            {title}
          </Dialog.Title>
          <Dialog.Description className="mb-4 text-sm text-gray-700 dark:text-gray-300">
            {children}
          </Dialog.Description>
          <div className="flex justify-end space-x-2">
            <Dialog.Close asChild>
              <button className="rounded bg-gray-200 px-4 py-2 text-sm text-gray-800 hover:bg-gray-300 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600">
                {cancelLabel}
              </button>
            </Dialog.Close>
            {onConfirm && (
              <button
                onClick={onConfirm}
                className="rounded bg-royal-blue px-4 py-2 text-sm text-white hover:bg-royal-blue/80"
              >
                {confirmLabel}
              </button>
            )}
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
};

// Helper hook to simplify usage
export const useModalAlert = () => {
  const [open, setOpen] = React.useState(false);
  const [content, setContent] = React.useState<ReactNode>(null);
  const [title, setTitle] = React.useState('');
  const [onConfirm, setOnConfirm] = React.useState<(() => void) | undefined>(undefined);

  const show = (
    msg: ReactNode,
    opts?: { title?: string; onConfirm?: () => void; confirmLabel?: string; cancelLabel?: string }
  ) => {
    setContent(msg);
    setTitle(opts?.title ?? 'Atención');
    setOnConfirm(() => opts?.onConfirm);
    setOpen(true);
  };

  const hide = () => setOpen(false);

  const Modal = (
    <ModalAlert
      isOpen={open}
      title={title}
      onClose={hide}
      onConfirm={onConfirm}
      confirmLabel={onConfirm ? 'Confirmar' : undefined}
    >
      {content}
    </ModalAlert>
  );

  return { show, hide, Modal };
};
