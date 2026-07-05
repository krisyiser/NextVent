'use client';

import React, { useState } from 'react';
import * as Dialog from '@radix-ui/react-dialog';
import { LockKey, Key, X } from 'phosphor-react';
import { getManagers } from '@/lib/storage';
import { invoke } from '@tauri-apps/api/core';
import { toast } from 'sonner';

type ManagerOverrideModalProps = {
  isOpen: boolean;
  actionName: string;
  onSuccess: () => void;
  onCancel: () => void;
};

export const ManagerOverrideModal = ({ isOpen, actionName, onSuccess, onCancel }: ManagerOverrideModalProps) => {
  const [pin, setPin] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (pin.length < 4) {
      toast.error('Ingresa un PIN de 4 dígitos');
      return;
    }

    setLoading(true);
    try {
      const managers = await getManagers();
      let authorized = false;

      for (const manager of managers) {
        if (manager.pin_checador_hash) {
          try {
            const isValid = await invoke<boolean>('verify_secret', { 
              secret: pin, 
              hashed: manager.pin_checador_hash 
            });
            if (isValid) {
              authorized = true;
              break;
            }
          } catch (e) {
            console.warn('Tauri invoke failed. Using mock verification.');
            if (manager.pin_checador_hash === `mock_hash_${pin}`) {
              authorized = true;
              break;
            }
          }
        }
      }

      if (authorized) {
        toast.success('Autorización exitosa');
        setPin('');
        onSuccess();
      } else {
        toast.error('PIN incorrecto o sin privilegios de Gerente');
      }
    } catch (e) {
      console.error(e);
      toast.error('Error al verificar autorización');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog.Root open={isOpen} onOpenChange={(open) => !open && onCancel()}>
      <Dialog.Portal>
        <Dialog.Overlay className="dialog-overlay" style={{ zIndex: 9999 }} />
        <Dialog.Content className="dialog-content" style={{ maxWidth: '400px', textAlign: 'center', zIndex: 10000, backgroundColor: 'var(--bg-secondary)', border: '1px solid var(--border-color)' }}>
          <Dialog.Title style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '8px', color: 'var(--accent-warning)', fontSize: '18px' }}>
            <LockKey size={48} weight="fill" />
            Autorización de Supervisor
          </Dialog.Title>
          <Dialog.Description style={{ marginTop: '10px', color: 'var(--text-secondary)', fontSize: '14px' }}>
            Se requiere autorización para: <strong style={{ color: 'var(--text-primary)' }}>{actionName}</strong>
          </Dialog.Description>

          <form onSubmit={handleSubmit} style={{ marginTop: '24px' }}>
            <div style={{ position: 'relative', width: '200px', margin: '0 auto' }}>
              <Key size={20} color="var(--text-muted)" style={{ position: 'absolute', left: '12px', top: '16px' }} />
              <input
                type="password"
                maxLength={4}
                value={pin}
                onChange={(e) => setPin(e.target.value.replace(/\\D/g, ''))}
                placeholder="PIN"
                disabled={loading}
                autoFocus
                style={{
                  width: '100%',
                  padding: '16px 16px 16px 44px',
                  borderRadius: 'var(--radius-md)',
                  border: '2px solid var(--border-color)',
                  backgroundColor: 'var(--bg-primary)',
                  color: 'var(--text-primary)',
                  fontSize: '24px',
                  fontWeight: 'bold',
                  letterSpacing: '8px',
                  textAlign: 'center',
                  outline: 'none',
                  transition: 'border-color 0.2s'
                }}
              />
            </div>
            
            <div style={{ display: 'flex', gap: '12px', marginTop: '32px' }}>
              <button 
                type="button" 
                onClick={onCancel}
                disabled={loading}
                style={{ flex: 1, padding: '12px', borderRadius: 'var(--radius-md)', border: '1px solid var(--border-color)', backgroundColor: 'transparent', color: 'var(--text-secondary)', fontWeight: 'bold', cursor: 'pointer' }}
              >
                Cancelar
              </button>
              <button 
                type="submit" 
                disabled={loading || pin.length < 4}
                style={{ flex: 1, padding: '12px', borderRadius: 'var(--radius-md)', border: 'none', backgroundColor: 'var(--accent-warning)', color: '#000', fontWeight: 'bold', cursor: loading || pin.length < 4 ? 'not-allowed' : 'pointer' }}
              >
                {loading ? 'Verificando...' : 'Autorizar'}
              </button>
            </div>
          </form>
          
          <button onClick={onCancel} style={{ position: 'absolute', top: '16px', right: '16px', border: 'none', background: 'transparent', color: 'var(--text-muted)', cursor: 'pointer' }}>
            <X size={20} />
          </button>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
};
