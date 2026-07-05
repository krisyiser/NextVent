'use client';

import React, { useEffect, useRef } from 'react';
import { useAuth } from '../auth/AuthProvider';
import { useRouter, usePathname } from 'next/navigation';
import { toast } from 'sonner';

export const AuthGuard: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { auth, loading } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const splashClosed = useRef(false);

  const isPublicRoute = pathname === '/login' || pathname === '/reloj' || pathname === '/onboarding' || pathname === '/splash';

  // Splash screen close logic has been moved to Rust backend to ensure it always executes reliably.

  useEffect(() => {
    const checkOnboarding = async () => {
      try {
        const { getUsersCount } = await import('../lib/storage');
        const count = await getUsersCount();
        if (count === 0 && pathname !== '/onboarding') {
          router.push('/onboarding');
        }
      } catch (e) {
        console.error('Failed to check onboarding status', e);
      }
    };
    checkOnboarding();
  }, [pathname, router]);

  useEffect(() => {
    if (!loading && !auth.user && !isPublicRoute) {
      router.push('/login');
    } else if (!loading && auth.user && !isPublicRoute) {
      // Role-Based Route Guarding
      const role = auth.role || 'CAJERO';
      const isCajero = role === 'CAJERO';
      const isGerente = role === 'GERENTE';

      if (isCajero && !['/', '/client-view'].includes(pathname || '')) {
        toast.error('Acceso denegado. Permisos insuficientes.');
        router.push('/');
      } else if (isGerente && !['/', '/client-view', '/inventory', '/history'].includes(pathname || '')) {
        toast.error('Acceso denegado. Se requiere nivel Administrador.');
        router.push('/');
      }
    }
  }, [auth.user, auth.role, loading, isPublicRoute, pathname, router]);

  if (loading) {
    return (
      <div 
        style={{ 
          display: 'flex', 
          width: '100vw', 
          height: '100vh', 
          alignItems: 'center', 
          justifyContent: 'center', 
          backgroundColor: 'var(--bg-primary)',
          color: 'var(--text-primary)',
          fontFamily: 'system-ui, sans-serif'
        }}
      >
        <div style={{ textAlign: 'center' }}>
          <div 
            style={{ 
              width: '40px', 
              height: '40px', 
              border: '3px solid var(--border-color)', 
              borderTopColor: 'var(--accent-primary)', 
              borderRadius: '50%', 
              animation: 'spin 1s linear infinite', 
              margin: '0 auto 16px' 
            }} 
          />
          <div style={{ fontSize: '14px', fontWeight: 500, color: 'var(--text-secondary)' }}>
            Iniciando NextVent...
          </div>
          <style>{`
            @keyframes spin {
              to { transform: rotate(360deg); }
            }
          `}</style>
        </div>
      </div>
    );
  }

  // If not authenticated and on a private route, hide children while redirecting
  if (!auth.user && !isPublicRoute) {
    return null;
  }

  return <>{children}</>;
};
