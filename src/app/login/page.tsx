'use client';

import React, { useState, useRef, useEffect } from 'react';
import { useAuth } from '../../auth/AuthProvider';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import { SignIn, User, Lock, Camera, SignOut } from 'phosphor-react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { recordAssistance } from '@/lib/storage';

const loginSchema = z.object({
  username: z.string().min(1, 'El usuario es obligatorio'),
  password: z.string().min(4, 'La contraseña debe tener al menos 4 caracteres'),
});

type LoginFields = z.infer<typeof loginSchema>;

export default function LoginPage() {
  const { login } = useAuth();
  const router = useRouter();
  const [loading, setLoading] = useState(false);

  const { register, handleSubmit, formState: { errors } } = useForm<LoginFields>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      username: '',
      password: '',
    }
  });

  const onSubmit = async (data: LoginFields) => {
    setLoading(true);
    try {
      const ok = await login(data.username, data.password);
      if (ok) {
        toast.success(`Bienvenido, ${data.username}`);
        router.push('/');
      } else {
        toast.error('Credenciales inválidas');
      }
    } catch (e) {
      toast.error('Ocurrió un error al iniciar sesión');
    } finally {
      setLoading(false);
    }
  };

  // --- RELOJ CHECADOR STATE & LOGIC ---
  const [pin, setPin] = useState('');
  const [stream, setStream] = useState<MediaStream | null>(null);
  const videoRef = useRef<HTMLVideoElement>(null);

  useEffect(() => {
    // Iniciar cámara
    navigator.mediaDevices.getUserMedia({ video: true })
      .then(s => {
          setStream(s);
          if (videoRef.current) {
              videoRef.current.srcObject = s;
          }
      })
      .catch(e => console.error("No webcam available for clock-in", e));

    return () => {
        if (stream) stream.getTracks().forEach(t => t.stop());
    };
  }, []);

  const handleClock = async (type: 'ENTRADA' | 'SALIDA') => {
      if (pin.length < 4) {
          toast.error("Ingresa un PIN válido (4 dígitos).");
          return;
      }
      
      const eventId = `reloj_${pin}_${type}_${Date.now()}`;
      
      // Attempt manual capture since we already have stream
      let photoPath = "NO_PHOTO";
      if (videoRef.current && stream) {
          const canvas = document.createElement('canvas');
          canvas.width = videoRef.current.videoWidth;
          canvas.height = videoRef.current.videoHeight;
          const ctx = canvas.getContext('2d');
          if (ctx) {
              ctx.drawImage(videoRef.current, 0, 0, canvas.width, canvas.height);
              const b64 = canvas.toDataURL('image/png');
              try {
                const { invoke } = await import('@tauri-apps/api/core');
                photoPath = await invoke('save_audit_image', { eventId, base64Image: b64 });
              } catch (e) {
                console.warn("Could not save audit image via Tauri", e);
              }
          }
      }

      try {
        await recordAssistance(pin, type, photoPath);
        toast.success(`Asistencia (${type}) registrada correctamente.`);
      } catch (e) {
        toast.error('Error al registrar la asistencia');
        console.error(e);
      }
      setPin('');
  };

  return (
    <div 
      style={{ 
        display: 'flex', 
        minHeight: '100vh', 
        width: '100vw', 
        alignItems: 'center', 
        justifyContent: 'center', 
        backgroundColor: 'var(--bg-primary)',
        color: 'var(--text-primary)',
        fontFamily: 'var(--font-sans, system-ui, sans-serif)',
        position: 'absolute',
        top: 0,
        left: 0,
        zIndex: 1000,
        padding: '20px'
      }}
    >
      <div 
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          gap: '40px',
          justifyContent: 'center',
          alignItems: 'stretch',
          width: '100%',
          maxWidth: '1000px'
        }}
      >
        {/* --- LEFT COLUMN: LOGIN --- */}
        <form
          onSubmit={handleSubmit(onSubmit)}
          style={{
            flex: '1 1 380px',
            maxWidth: '450px',
            backgroundColor: 'var(--bg-secondary)',
            padding: '36px',
            borderRadius: 'var(--radius-lg)',
            border: '1px solid var(--border-color)',
            boxShadow: 'var(--shadow-lg)',
            display: 'flex',
            flexDirection: 'column',
            gap: '16px'
          }}
        >
          <div style={{ textAlign: 'center', marginBottom: '8px', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <img src="/logo.png" alt="NextVent" style={{ width: '48px', height: '48px', objectFit: 'contain', marginBottom: '12px', filter: 'drop-shadow(0 0 8px rgba(16,185,129,0.4))' }} />
            <h2 style={{ fontSize: '28px', fontWeight: 700, color: 'var(--accent-primary)', letterSpacing: '-0.5px', marginBottom: '4px' }}>
              NextVent
            </h2>
            <p style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
              Iniciar sesión en el sistema
            </p>
          </div>
          
          <div>
            <label 
              style={{ 
                display: 'block', 
                fontSize: '12px', 
                fontWeight: 600, 
                color: 'var(--text-secondary)', 
                marginBottom: '6px',
                textTransform: 'uppercase',
                letterSpacing: '0.5px'
              }} 
              htmlFor="username"
            >
              Usuario
            </label>
            <div 
              style={{ 
                display: 'flex', 
                alignItems: 'center', 
                border: '1px solid var(--border-color)', 
                borderRadius: 'var(--radius-md)', 
                backgroundColor: 'var(--bg-tertiary)', 
                padding: '0 12px',
                height: '42px',
                transition: 'var(--transition)'
              }}
            >
              <User size={18} weight="regular" style={{ color: 'var(--text-muted)', marginRight: '8px' }} />
              <input
                id="username"
                type="text"
                {...register('username')}
                style={{
                  flex: 1,
                  border: 'none',
                  outline: 'none',
                  backgroundColor: 'transparent',
                  color: 'var(--text-primary)',
                  fontSize: '14px',
                  height: '100%'
                }}
                autoFocus
                placeholder="admin"
              />
            </div>
            {errors.username && (
              <p style={{ marginTop: '4px', fontSize: '11px', color: 'var(--accent-danger)' }}>{errors.username.message}</p>
            )}
          </div>

          <div>
            <label 
              style={{ 
                display: 'block', 
                fontSize: '12px', 
                fontWeight: 600, 
                color: 'var(--text-secondary)', 
                marginBottom: '6px',
                textTransform: 'uppercase',
                letterSpacing: '0.5px'
              }} 
              htmlFor="password"
            >
              Contraseña
            </label>
            <div 
              style={{ 
                display: 'flex', 
                alignItems: 'center', 
                border: '1px solid var(--border-color)', 
                borderRadius: 'var(--radius-md)', 
                backgroundColor: 'var(--bg-tertiary)', 
                padding: '0 12px',
                height: '42px',
                transition: 'var(--transition)'
              }}
            >
              <Lock size={18} weight="regular" style={{ color: 'var(--text-muted)', marginRight: '8px' }} />
              <input
                id="password"
                type="password"
                {...register('password')}
                style={{
                  flex: 1,
                  border: 'none',
                  outline: 'none',
                  backgroundColor: 'transparent',
                  color: 'var(--text-primary)',
                  fontSize: '14px',
                  height: '100%'
                }}
                placeholder="••••••"
              />
            </div>
            {errors.password && (
              <p style={{ marginTop: '4px', fontSize: '11px', color: 'var(--accent-danger)' }}>{errors.password.message}</p>
            )}
          </div>

          <button
            type="submit"
            disabled={loading}
            style={{
              width: '100%',
              height: '44px',
              backgroundColor: 'var(--accent-primary)',
              color: '#FFFFFF',
              border: 'none',
              borderRadius: 'var(--radius-md)',
              fontWeight: 600,
              fontSize: '14px',
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              gap: '8px',
              transition: 'var(--transition)',
              opacity: loading ? 0.6 : 1,
              marginTop: 'auto'
            }}
          >
            <SignIn size={18} weight="regular" />
            {loading ? 'Ingresando...' : 'Entrar al Sistema'}
          </button>
        </form>

        {/* --- RIGHT COLUMN: RELOJ CHECADOR --- */}
        <div style={{
          flex: '1 1 380px',
          maxWidth: '450px',
          backgroundColor: 'var(--bg-secondary)', 
          padding: '36px', 
          borderRadius: 'var(--radius-lg)', 
          textAlign: 'center', 
          border: '1px solid var(--border-color)',
          boxShadow: 'var(--shadow-lg)'
        }}>
          
          <div style={{ position: 'relative', width: '120px', height: '120px', margin: '0 auto 20px', borderRadius: '50%', overflow: 'hidden', backgroundColor: 'var(--bg-tertiary)', border: '4px solid var(--border-color)' }}>
              <video ref={videoRef} autoPlay playsInline muted style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
              {!stream && <div style={{ position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%, -50%)', color: 'var(--text-muted)' }}><Camera size={32} /></div>}
          </div>

          <h2 style={{ fontSize: '24px', fontWeight: 'bold', marginBottom: '4px', color: 'var(--text-primary)' }}>Reloj Checador</h2>
          <p style={{ color: 'var(--text-secondary)', marginBottom: '20px', fontSize: '13px' }}>Ingresa tu PIN de 4 dígitos</p>

          <input 
              type="password" 
              value={pin}
              readOnly
              style={{ width: '100%', padding: '12px', fontSize: '28px', textAlign: 'center', backgroundColor: 'var(--bg-tertiary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)', color: 'var(--text-primary)', marginBottom: '20px', letterSpacing: '8px', outline: 'none' }}
          />

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '10px', marginBottom: '20px' }}>
              {[1, 2, 3, 4, 5, 6, 7, 8, 9, 'C', 0, 'X'].map(num => (
                  <button 
                      key={num}
                      type="button"
                      onClick={() => {
                          if (num === 'C') setPin('');
                          else if (num === 'X') setPin(pin.slice(0, -1));
                          else if (pin.length < 4) setPin(pin + num);
                      }}
                      style={{ padding: '12px', fontSize: '20px', fontWeight: 600, backgroundColor: 'var(--bg-primary)', border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)', color: 'var(--text-primary)', cursor: 'pointer', transition: 'var(--transition)' }}
                      onMouseEnter={e => e.currentTarget.style.backgroundColor = 'var(--bg-tertiary)'}
                      onMouseLeave={e => e.currentTarget.style.backgroundColor = 'var(--bg-primary)'}
                  >
                      {num}
                  </button>
              ))}
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <button 
                type="button"
                onClick={() => handleClock('ENTRADA')} 
                style={{ padding: '14px', backgroundColor: 'var(--accent-success)', border: 'none', borderRadius: 'var(--radius-md)', color: 'white', fontWeight: 600, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '6px', cursor: 'pointer', transition: 'opacity 0.2s' }}
                onMouseEnter={e => e.currentTarget.style.opacity = '0.9'}
                onMouseLeave={e => e.currentTarget.style.opacity = '1'}
              >
                  <SignIn size={20} />
                  ENTRADA
              </button>
              <button 
                type="button"
                onClick={() => handleClock('SALIDA')} 
                style={{ padding: '14px', backgroundColor: 'var(--accent-danger)', border: 'none', borderRadius: 'var(--radius-md)', color: 'white', fontWeight: 600, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '6px', cursor: 'pointer', transition: 'opacity 0.2s' }}
                onMouseEnter={e => e.currentTarget.style.opacity = '0.9'}
                onMouseLeave={e => e.currentTarget.style.opacity = '1'}
              >
                  <SignOut size={20} />
                  SALIDA
              </button>
          </div>
        </div>

      </div>
    </div>
  );
}
