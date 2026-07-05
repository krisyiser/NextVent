'use client';

import React, { useState, useEffect } from 'react';
import { Users, UserPlus, Trash, Shield, PencilSimple, Key, CheckCircle } from 'phosphor-react';
import { getUsers, saveUser, deleteUser, Usuario } from '@/lib/storage';
import { toast } from 'sonner';
import { invoke } from '@tauri-apps/api/core';

export const SettingsUsers = () => {
  const [users, setUsers] = useState<Usuario[]>([]);
  const [loading, setLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  
  // Form State
  const [id, setId] = useState('');
  const [nombre, setNombre] = useState('');
  const [rol, setRol] = useState<'ADMIN' | 'GERENTE' | 'CAJERO'>('CAJERO');
  const [password, setPassword] = useState('');
  const [pin, setPin] = useState('');

  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    try {
      const u = await getUsers();
      setUsers(u);
    } catch (e) {
      console.error(e);
      toast.error('Error al cargar usuarios');
    }
  };

  const handleEdit = (u: Usuario) => {
    setId(u.id);
    setNombre(u.nombre);
    setRol(u.rol as 'ADMIN' | 'GERENTE' | 'CAJERO');
    setPassword('');
    setPin('');
    setShowForm(true);
  };

  const handleNew = () => {
    setId(`USR-${Date.now()}`);
    setNombre('');
    setRol('CAJERO');
    setPassword('');
    setPin('');
    setShowForm(true);
  };

  const handleDelete = async (userId: string) => {
    if (!confirm('¿Estás seguro de eliminar este usuario?')) return;
    try {
      await deleteUser(userId);
      toast.success('Usuario eliminado');
      loadUsers();
    } catch (e) {
      console.error(e);
      toast.error('Error al eliminar usuario');
    }
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!nombre) { toast.error('El nombre es obligatorio'); return; }
    
    setLoading(true);
    try {
      let passwordHash = undefined;
      let pinHash = undefined;

      if (password) {
        try {
          passwordHash = await invoke<string>('hash_secret', { secret: password });
        } catch (e) {
          console.warn('Tauri invoke failed. Using mock hash.');
          passwordHash = `mock_hash_${password}`;
        }
      }

      if (pin) {
        try {
          pinHash = await invoke<string>('hash_secret', { secret: pin });
        } catch (e) {
          console.warn('Tauri invoke failed. Using mock hash.');
          pinHash = `mock_hash_${pin}`;
        }
      }

      const existingUser = users.find(u => u.id === id);

      const userToSave: Usuario = {
        id,
        nombre,
        rol,
        estatus: 1,
        // Mantener el hash anterior si no se actualizó el campo
        password_hash: passwordHash || existingUser?.password_hash,
        pin_checador_hash: pinHash || existingUser?.pin_checador_hash
      };

      await saveUser(userToSave);
      toast.success('Usuario guardado correctamente');
      setShowForm(false);
      loadUsers();
    } catch (err) {
      console.error(err);
      toast.error('Error al guardar el usuario');
    } finally {
      setLoading(false);
    }
  };

  if (showForm) {
    return (
      <div style={{ backgroundColor: 'var(--bg-primary)', padding: '20px', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
        <h3 style={{ fontSize: '16px', fontWeight: 700, marginBottom: '20px', display: 'flex', alignItems: 'center', gap: '8px' }}>
          <UserPlus size={20} className="text-accent" />
          {users.find(u => u.id === id) ? 'Editar Usuario' : 'Nuevo Usuario'}
        </h3>

        <form onSubmit={handleSave} style={{ display: 'grid', gap: '16px' }}>
          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '6px' }}>Nombre Completo</label>
            <input 
              value={nombre} onChange={e => setNombre(e.target.value)} 
              placeholder="Ej. María Sánchez"
              style={{ width: '100%', padding: '10px', borderRadius: '4px', border: '1px solid var(--border-color)', backgroundColor: 'var(--bg-secondary)', color: 'var(--text-primary)' }}
            />
          </div>

          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '6px' }}>Rol</label>
            <select 
              value={rol} onChange={e => setRol(e.target.value as 'ADMIN' | 'GERENTE' | 'CAJERO')}
              style={{ width: '100%', padding: '10px', borderRadius: '4px', border: '1px solid var(--border-color)', backgroundColor: 'var(--bg-secondary)', color: 'var(--text-primary)' }}
            >
              <option value="CAJERO">Cajero (Solo ventas)</option>
              <option value="GERENTE">Gerente (Inventario y Cortes)</option>
              <option value="ADMIN">Administrador (Acceso Total)</option>
            </select>
          </div>

          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '6px' }}>
              Contraseña de Acceso {users.find(u => u.id === id) && '(Dejar en blanco para no cambiar)'}
            </label>
            <input 
              type="password" value={password} onChange={e => setPassword(e.target.value)} 
              placeholder="***"
              style={{ width: '100%', padding: '10px', borderRadius: '4px', border: '1px solid var(--border-color)', backgroundColor: 'var(--bg-secondary)', color: 'var(--text-primary)' }}
            />
          </div>

          {(rol === 'ADMIN' || rol === 'GERENTE') && (
            <div>
              <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '6px' }}>
                PIN Rápido (4 dígitos) {users.find(u => u.id === id) && '(Dejar en blanco para no cambiar)'}
              </label>
              <input 
                type="password" maxLength={4} value={pin} onChange={e => setPin(e.target.value.replace(/\D/g, ''))} 
                placeholder="1234"
                style={{ width: '100%', padding: '10px', borderRadius: '4px', border: '1px solid var(--border-color)', backgroundColor: 'var(--bg-secondary)', color: 'var(--text-primary)' }}
              />
              <p style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '4px' }}>Este PIN se utiliza para aprobar cancelaciones (Manager Override) de los cajeros en tiempo real.</p>
            </div>
          )}

          <div style={{ display: 'flex', gap: '12px', marginTop: '16px' }}>
            <button 
              type="button" onClick={() => setShowForm(false)}
              style={{ flex: 1, padding: '12px', backgroundColor: 'transparent', border: '1px solid var(--border-color)', borderRadius: '4px', color: 'var(--text-secondary)', cursor: 'pointer', fontWeight: 600 }}
            >
              Cancelar
            </button>
            <button 
              type="submit" disabled={loading}
              style={{ flex: 1, padding: '12px', backgroundColor: 'var(--accent-primary)', border: 'none', borderRadius: '4px', color: '#fff', cursor: loading ? 'not-allowed' : 'pointer', fontWeight: 600, display: 'flex', justifyContent: 'center', alignItems: 'center', gap: '8px' }}
            >
              <CheckCircle size={18} /> {loading ? 'Guardando...' : 'Guardar Usuario'}
            </button>
          </div>
        </form>
      </div>
    );
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px', paddingBottom: '12px', borderBottom: '1px solid var(--border-color)' }}>
        <h3 style={{ fontSize: '16px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--text-primary)' }}>
          <Users size={20} className="text-accent" /> Gestión de Usuarios y Roles
        </h3>
        <button 
          onClick={handleNew}
          style={{ padding: '8px 16px', backgroundColor: 'var(--accent-primary)', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer', fontSize: '12px', fontWeight: 600, display: 'flex', alignItems: 'center', gap: '6px' }}
        >
          <UserPlus size={16} /> Agregar Usuario
        </button>
      </div>

      <div style={{ display: 'grid', gap: '12px' }}>
        {users.map(u => (
          <div key={u.id} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '16px', backgroundColor: 'var(--bg-primary)', border: '1px solid var(--border-color)', borderRadius: '6px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
              <div style={{ width: '40px', height: '40px', borderRadius: '50%', backgroundColor: 'rgba(59, 130, 246, 0.1)', display: 'flex', justifyContent: 'center', alignItems: 'center', color: 'var(--accent-primary)' }}>
                {u.rol === 'ADMIN' ? <Shield size={20} weight="fill" /> : <Users size={20} />}
              </div>
              <div>
                <div style={{ fontWeight: 600, fontSize: '14px', color: 'var(--text-primary)' }}>{u.nombre}</div>
                <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '2px', display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <span style={{ padding: '2px 6px', backgroundColor: 'var(--bg-tertiary)', borderRadius: '4px', fontWeight: 500 }}>{u.rol}</span>
                </div>
              </div>
            </div>
            
            <div style={{ display: 'flex', gap: '8px' }}>
              <button 
                onClick={() => handleEdit(u)}
                style={{ width: '32px', height: '32px', display: 'flex', justifyContent: 'center', alignItems: 'center', border: '1px solid var(--border-color)', backgroundColor: 'transparent', borderRadius: '4px', cursor: 'pointer', color: 'var(--text-secondary)' }}
                title="Editar Usuario"
              >
                <PencilSimple size={16} />
              </button>
              {u.rol !== 'ADMIN' && (
                <button 
                  onClick={() => handleDelete(u.id)}
                  style={{ width: '32px', height: '32px', display: 'flex', justifyContent: 'center', alignItems: 'center', border: '1px solid var(--border-color)', backgroundColor: 'transparent', borderRadius: '4px', cursor: 'pointer', color: 'var(--accent-danger)' }}
                  title="Eliminar Usuario"
                >
                  <Trash size={16} />
                </button>
              )}
            </div>
          </div>
        ))}

        {users.length === 0 && (
          <div style={{ textAlign: 'center', padding: '32px', color: 'var(--text-muted)' }}>
            No hay usuarios registrados.
          </div>
        )}
      </div>
    </div>
  );
};
