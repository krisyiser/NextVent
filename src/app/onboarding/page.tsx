'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import { invoke } from '@tauri-apps/api/core';
import { saveUser, setSetting } from '@/lib/storage';
import { ShieldCheck, User, Lock, Key } from 'phosphor-react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const onboardingSchema = z.object({
  nombre: z.string().min(3, 'El nombre debe tener al menos 3 caracteres'),
  password: z.string().min(4, 'La contraseña maestra debe tener al menos 4 caracteres'),
  pin: z.string().length(4, 'El PIN rápido debe ser de exactamente 4 dígitos').regex(/^\d+$/, 'El PIN solo puede contener números')
});

type OnboardingFields = z.infer<typeof onboardingSchema>;

export default function OnboardingPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<OnboardingFields>({
    resolver: zodResolver(onboardingSchema),
    defaultValues: {
      nombre: 'Administrador',
      password: '',
      pin: '',
    }
  });

  const onSubmit = async (data: OnboardingFields) => {
    setLoading(true);
    try {
      // 1. Hash secrets natively via Tauri Rust Backend
      let passwordHash = data.password;
      let pinHash = data.pin;
      
      try {
        passwordHash = await invoke<string>('hash_secret', { secret: data.password });
        pinHash = await invoke<string>('hash_secret', { secret: data.pin });
      } catch (e) {
        console.warn('Tauri invoke failed (likely browser mode). Using fallback hashing.', e);
        // Fallback for browser dev mode
        passwordHash = `mock_hash_${data.password}`;
        pinHash = `mock_hash_${data.pin}`;
      }

      // 2. Save root user to SQLite (or MockDB)
      await saveUser({
        id: `USR-${Date.now()}`,
        nombre: data.nombre,
        rol: 'ADMIN',
        password_hash: passwordHash,
        pin_checador_hash: pinHash,
        estatus: 1
      });

      // 3. Mark system as configured
      await setSetting('configurado', '1');

      toast.success('¡Sistema configurado correctamente! Bienvenido a NextVent.');
      router.push('/login');
    } catch (e) {
      console.error(e);
      toast.error('Ocurrió un error al configurar el sistema.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ display: 'flex', minHeight: '100vh', backgroundColor: 'var(--bg-primary)', alignItems: 'center', justifyContent: 'center' }}>
      <div style={{ backgroundColor: 'var(--bg-secondary)', padding: '40px', borderRadius: 'var(--radius-lg)', boxShadow: 'var(--shadow-lg)', maxWidth: '450px', width: '100%' }}>
        <div style={{ textAlign: 'center', marginBottom: '32px' }}>
          <ShieldCheck size={48} color="var(--accent-primary)" weight="fill" style={{ margin: '0 auto 16px' }} />
          <h1 style={{ fontSize: '24px', fontWeight: 'bold' }}>Bienvenido a NextVent</h1>
          <p style={{ color: 'var(--text-secondary)', fontSize: '14px', marginTop: '8px' }}>
            Tu punto de venta está listo. Por seguridad, debes crear la cuenta maestra de administrador antes de continuar.
          </p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <div>
            <label style={{ display: 'block', fontSize: '13px', fontWeight: 'bold', marginBottom: '8px', color: 'var(--text-secondary)' }}>NOMBRE DEL ADMINISTRADOR</label>
            <div style={{ position: 'relative' }}>
              <User size={18} color="var(--text-muted)" style={{ position: 'absolute', left: '12px', top: '10px' }} />
              <input
                {...register('nombre')}
                type="text"
                placeholder="Ej. Juan Pérez"
                style={{ width: '100%', padding: '10px 10px 10px 40px', borderRadius: 'var(--radius-md)', border: '1px solid var(--border-color)', backgroundColor: 'var(--bg-primary)' }}
              />
            </div>
            {errors.nombre && <p style={{ color: 'var(--accent-danger)', fontSize: '12px', marginTop: '4px' }}>{errors.nombre.message}</p>}
          </div>

          <div>
            <label style={{ display: 'block', fontSize: '13px', fontWeight: 'bold', marginBottom: '8px', color: 'var(--text-secondary)' }}>CONTRASEÑA MAESTRA</label>
            <div style={{ position: 'relative' }}>
              <Lock size={18} color="var(--text-muted)" style={{ position: 'absolute', left: '12px', top: '10px' }} />
              <input
                {...register('password')}
                type="password"
                placeholder="Contraseña segura"
                style={{ width: '100%', padding: '10px 10px 10px 40px', borderRadius: 'var(--radius-md)', border: '1px solid var(--border-color)', backgroundColor: 'var(--bg-primary)' }}
              />
            </div>
            {errors.password && <p style={{ color: 'var(--accent-danger)', fontSize: '12px', marginTop: '4px' }}>{errors.password.message}</p>}
          </div>

          <div>
            <label style={{ display: 'block', fontSize: '13px', fontWeight: 'bold', marginBottom: '8px', color: 'var(--text-secondary)' }}>PIN DE AUTORIZACIÓN (4 DÍGITOS)</label>
            <div style={{ position: 'relative' }}>
              <Key size={18} color="var(--text-muted)" style={{ position: 'absolute', left: '12px', top: '10px' }} />
              <input
                {...register('pin')}
                type="password"
                maxLength={4}
                placeholder="1234"
                style={{ width: '100%', padding: '10px 10px 10px 40px', borderRadius: 'var(--radius-md)', border: '1px solid var(--border-color)', backgroundColor: 'var(--bg-primary)', letterSpacing: '4px', fontWeight: 'bold' }}
              />
            </div>
            <p style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '4px' }}>Este PIN servirá para autorizar cancelaciones sin cerrar sesión.</p>
            {errors.pin && <p style={{ color: 'var(--accent-danger)', fontSize: '12px', marginTop: '4px' }}>{errors.pin.message}</p>}
          </div>

          <button 
            type="submit" 
            disabled={loading}
            style={{ 
              width: '100%', padding: '14px', borderRadius: 'var(--radius-md)', border: 'none', 
              backgroundColor: 'var(--accent-primary)', color: 'white', fontWeight: 'bold', fontSize: '15px', 
              cursor: loading ? 'not-allowed' : 'pointer', marginTop: '10px', transition: 'var(--transition)' 
            }}
          >
            {loading ? 'Cifrando Datos...' : 'Crear Cuenta Maestra'}
          </button>
        </form>
      </div>
    </div>
  );
}
