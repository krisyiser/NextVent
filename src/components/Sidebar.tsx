'use client';
import { useState } from 'react';
import Link from 'next/link';
import { SquaresFour, ShoppingCart, Package, User, ChartPieSlice, Gear, Archive, Tag, WhatsappLogo, Storefront, Receipt } from 'phosphor-react';
import { getActiveShift } from '@/lib/storage';
import { SettingsModal } from './SettingsModal';
import { BlindCloseShiftModal } from './BlindCloseShiftModal';
import { Shift } from '@/types';
import { toast } from 'sonner';
import { useWhatsappPanel } from './WhatsappPanelProvider';
import { ThemeToggle } from './ThemeToggle';
import { useTheme } from './ThemeProvider';
import { useAuth } from '../auth/AuthProvider';

type SidebarProps = {
  activeModule?: 'pos' | 'inventory' | 'customers' | 'reports' | 'history' | 'promotions' | 'reloj' | 'fiscal';
};

export const Sidebar = ({ activeModule = 'pos' }: SidebarProps) => {
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isBlindCloseOpen, setIsBlindCloseOpen] = useState(false);
  const [currentShift, setCurrentShift] = useState<Shift | null>(null);
  const { toggleOpen } = useWhatsappPanel();
  const { logo, refreshSettings } = useTheme();
  const { auth } = useAuth();
  const role = auth.role || 'CAJERO'; // Default to lowest privilege if not ready

  const handleCloseShift = async () => {
    try {
        const shift = await getActiveShift();
        if (!shift) {
            toast.error("No hay un turno de caja abierto en este momento.");
            return;
        }
        setCurrentShift(shift as Shift);
        setIsBlindCloseOpen(true);
    } catch (e) {
        console.error(e);
        toast.error("Error de base de datos al buscar turno activo.");
    }
  };

  const handleWhatsappClick = () => {
    toggleOpen();
  };

  return (
    <nav className="sidebar">
      {/* Company Logo / Brand Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '4px' }}>
        {logo ? (
          <div style={{ width: '40px', height: '40px', borderRadius: '4px', overflow: 'hidden', display: 'flex', alignItems: 'center', justifyContent: 'center', backgroundColor: 'rgba(255,255,255,0.15)', border: '1px solid rgba(255,255,255,0.2)' }}>
            <img src={logo} alt="Empresa Logo" style={{ maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }} />
          </div>
        ) : (
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center' }} title="NextVent POS">
            <img src="/logo.png" alt="NextVent" style={{ width: '32px', height: '32px', objectFit: 'contain', filter: 'drop-shadow(0 0 8px rgba(16,185,129,0.4))' }} />
          </div>
        )}
      </div>

      <Link href="/" className={`nav-item ${activeModule === 'pos' ? 'active' : ''}`} title="Punto de Venta">
        <ShoppingCart size={24} weight="regular" />
      </Link>
      
      {(role === 'ADMIN' || role === 'GERENTE') && (
        <>
          <Link href="/inventory" className={`nav-item ${activeModule === 'inventory' ? 'active' : ''}`} title="Inventario">
            <Package size={24} weight="regular" />
          </Link>
          <Link href="/history" className={`nav-item ${activeModule === 'history' ? 'active' : ''}`} title="Historial y Reportes">
            <ChartPieSlice size={24} weight="regular" />
          </Link>
        </>
      )}

      {role === 'ADMIN' && (
        <>
          <Link href="/customers" className={`nav-item ${activeModule === 'customers' ? 'active' : ''}`} title="Clientes">
            <User size={24} weight="regular" />
          </Link>
          <Link href="/promotions" className={`nav-item ${activeModule === 'promotions' ? 'active' : ''}`} title="Promociones">
            <Tag size={24} weight="regular" />
          </Link>
          <Link href="/fiscal" className={`nav-item ${activeModule === 'fiscal' ? 'active' : ''}`} title="Facturación y Herramientas Fiscales">
            <Receipt size={24} weight="regular" />
          </Link>
        </>
      )}

      <button 
        className="nav-item" 
        style={{ color: '#25D366', border: 'none', background: 'none', cursor: 'pointer' }}
        onClick={handleWhatsappClick}
        title="WhatsApp Web"
      >
        <WhatsappLogo size={24} weight="fill" />
      </button>
      
      {/* Controls Container with margin adaptation for top/bottom horizontal layout */}
      <div style={{ 
        marginTop: 'var(--sidebar-item-margin-top, auto)', 
        marginLeft: 'var(--sidebar-item-margin-left, 0)', 
        display: 'flex', 
        alignItems: 'center',
        flexDirection: 'inherit',
        gap: '20px'
      }}>
        <ThemeToggle />

        {role === 'ADMIN' && (
          <button className="nav-item" style={{ border: 'none', background: 'none' }} onClick={() => setIsSettingsOpen(true)} title="Configuración">
            <Gear size={24} weight="regular" />
          </button>
        )}
        <button 
          className="nav-item" 
          style={{ color: 'var(--accent-danger)', border: 'none', background: 'none' }}
          onClick={handleCloseShift}
          title="Cerrar Turno (Corte de Caja)"
        >
          <Archive size={24} weight="regular" />
        </button>
      </div>

      <SettingsModal isOpen={isSettingsOpen} onClose={() => { setIsSettingsOpen(false); refreshSettings(); }} />
      <BlindCloseShiftModal isOpen={isBlindCloseOpen} onClose={() => setIsBlindCloseOpen(false)} shift={currentShift} />
    </nav>
  );
};
