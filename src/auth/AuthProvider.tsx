// src/auth/AuthProvider.tsx
'use client';

import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { z } from 'zod';
import { getSetting, setSetting, logAudit } from '../lib/storage';

interface AuthState {
  user: string | null;
  role: 'ADMIN' | 'GERENTE' | 'CAJERO' | null;
}

interface AuthContextProps {
  auth: AuthState;
  loading: boolean;
  login: (username: string, password: string) => Promise<boolean>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextProps | undefined>(undefined);

import { getUserByName } from '../lib/storage';
import { invoke } from '@tauri-apps/api/core';

const credentialsSchema = z.object({
  username: z.string().min(1),
  password: z.string().min(1),
});

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [auth, setAuth] = useState<AuthState>({ user: null, role: null });
  const [loading, setLoading] = useState(true);

  // On load, clear any previous session to force redirect to login screen on startup
  useEffect(() => {
    (async () => {
      try {
        await setSetting('auth_user', '');
        await setSetting('auth_role', '');
      } catch (_) {
        // DB not ready yet
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const login = async (username: string, password: string): Promise<boolean> => {
    const parse = credentialsSchema.safeParse({ username, password });
    if (!parse.success) return false;

    try {
      const user = await getUserByName(username);
      if (!user) return false;

      // Verify password via Tauri IPC
      if (!user.password_hash) return false;
      let isValid = false;
      
      try {
        isValid = await invoke<boolean>('verify_secret', { secret: password, hashed: user.password_hash });
      } catch (e) {
        console.warn('Tauri invoke failed. Using mock verification.', e);
        isValid = user.password_hash === `mock_hash_${password}`;
      }
      
      if (!isValid) return false;

      if (user.estatus === 0) return false;

      setAuth({ user: user.nombre, role: user.rol });
      await setSetting('auth_user', user.nombre);
      await setSetting('auth_role', user.rol);
      await logAudit('info', `User logged in: ${user.nombre}`, { role: user.rol });
      return true;
    } catch (e) {
      console.error("Login failed:", e);
      return false;
    }
  };

  const logout = () => {
    const previousUser = auth.user;
    setAuth({ user: null, role: null });
    (async () => {
      try {
        await setSetting('auth_user', '');
        await setSetting('auth_role', '');
        await logAudit('info', `User logged out: ${previousUser}`);
      } catch (_) {
        // Silently fail
      }
    })();
  };

  return (
    <AuthContext.Provider value={{ auth, loading, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextProps => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
};

