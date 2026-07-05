// src/components/BackupExport.tsx
'use client';

import React from 'react';
import { exportJSONFile, exportCSVFile } from '../lib/export';
import { DownloadSimple } from 'phosphor-react';
import { toast } from 'sonner';

export const BackupExport = () => {
  const handleExport = async (type: 'json' | 'csv') => {
    try {
      if (type === 'json') {
        await exportJSONFile();
      } else {
        await exportCSVFile();
      }
      toast.success(`Archivo ${type.toUpperCase()} exportado correctamente`);
    } catch (e) {
      console.error('Export failed', e);
      toast.error('Error al exportar. Intente de nuevo.');
    }
  };

  return (
    <div className="flex space-x-2">
      <button
        onClick={() => handleExport('json')}
        className="flex items-center gap-2 rounded bg-royal-blue px-3 py-1 text-white hover:bg-royal-blue/80"
      >
        <DownloadSimple size={16} weight="regular" />
        Exportar JSON
      </button>
      <button
        onClick={() => handleExport('csv')}
        className="flex items-center gap-2 rounded bg-royal-blue px-3 py-1 text-white hover:bg-royal-blue/80"
      >
        <DownloadSimple size={16} weight="regular" />
        Exportar CSV
      </button>
    </div>
  );
};
