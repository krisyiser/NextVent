import { useState } from 'react';
import Link from 'next/link';
import { LayoutDashboard, ShoppingCart, Package, User, PieChart, MessageCircle, Settings, LogOut } from 'lucide-react';
import { closeShift, getActiveShift } from '@/lib/storage';
import { SettingsModal } from './SettingsModal';
import { MessageModal } from './MessageModal';

type SidebarProps = {
  activeModule?: 'pos' | 'inventory' | 'customers' | 'reports' | 'history';
};

export const Sidebar = ({ activeModule = 'pos' }: SidebarProps) => {
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isMessageOpen, setIsMessageOpen] = useState(false);

  const handleCloseShift = async () => {
    const shift = await getActiveShift();
    if (!shift) return;

    const actual = prompt(`Corte de Caja - Turno #${shift.id}\nEfectivo Esperado: $${shift.expectedBalance.toFixed(2)}\nIngresa el EFECTIVO REAL en caja:`);
    if (actual !== null && !isNaN(parseFloat(actual))) {
      await closeShift(parseFloat(actual));
      window.location.reload(); 
    }
  };

  return (
    <nav className="sidebar">

      <Link href="/" className={`nav-item ${activeModule === 'pos' ? 'active' : ''}`}>
        <ShoppingCart size={24} />
      </Link>
      <Link href="/inventory" className={`nav-item ${activeModule === 'inventory' ? 'active' : ''}`}>
        <Package size={24} />
      </Link>
      <Link href="/history" className={`nav-item ${activeModule === 'history' ? 'active' : ''}`}>
        <PieChart size={24} />
      </Link>
      <Link href="/customers" className={`nav-item ${activeModule === 'customers' ? 'active' : ''}`}>
        <User size={24} />
      </Link>
      
      <button className="nav-item" style={{ border: 'none', background: 'none', marginTop: 'auto' }} onClick={() => setIsMessageOpen(true)} title="Enviar Mensaje a Cliente">
        <MessageCircle size={24} className="text-success" />
      </button>
      <button className="nav-item" style={{ border: 'none', background: 'none' }} onClick={() => setIsSettingsOpen(true)} title="Configuración">
        <Settings size={24} />
      </button>
      <button 
        className="nav-item" 
        style={{ color: 'var(--accent-danger)', border: 'none', background: 'none' }}
        onClick={handleCloseShift}
        title="Cerrar Turno (Corte de Caja)"
      >
        <LogOut size={24} />
      </button>

      <div style={{ marginTop: '16px', fontSize: '10px', textAlign: 'center', color: 'var(--text-muted)' }}>
        <span style={{ fontWeight: 'bold' }}>NextVent</span><br/>
        by Zima Technologies
      </div>

      {/* Renderizamos Mods aquí para que estén en todas las páginas donde esté Sidebar */}
      <SettingsModal isOpen={isSettingsOpen} onClose={() => setIsSettingsOpen(false)} />
      <MessageModal isOpen={isMessageOpen} onClose={() => setIsMessageOpen(false)} />
    </nav>
  );
};
