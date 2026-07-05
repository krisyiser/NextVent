'use client';

import React, { useEffect, useState } from 'react';
import { Html5QrcodeScanner } from 'html5-qrcode';
import { getProducts } from '@/lib/storage';
import { Product } from '@/types';
import { Tag } from 'phosphor-react';

export default function KioskoVerificador() {
  const [scannedResult, setScannedResult] = useState<Product | null>(null);
  const [errorMsg, setErrorMsg] = useState('');

  useEffect(() => {
    const scanner = new Html5QrcodeScanner(
      "reader",
      { fps: 10, qrbox: { width: 250, height: 100 } },
      false
    );

    scanner.render(async (decodedText) => {
      try {
          const products = await getProducts();
          const found = products.find(p => p.barcode === decodedText);
          if (found) {
              setScannedResult(found);
              setErrorMsg('');
          } else {
              setScannedResult(null);
              setErrorMsg(`Producto no encontrado: ${decodedText}`);
          }
      } catch (e) {
          console.error(e);
      }
    }, (error) => {
      // Ignorar errores de escaneo continuo
    });

    return () => {
      scanner.clear().catch(e => console.error("Failed to clear scanner", e));
    };
  }, []);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', backgroundColor: 'var(--bg-primary)', color: '#fff', alignItems: 'center', justifyContent: 'center', padding: '24px' }}>
        <div style={{ textAlign: 'center', marginBottom: '32px' }}>
            <Tag size={64} color="var(--accent-primary)" style={{ margin: '0 auto 16px' }} />
            <h1 style={{ fontSize: '36px', fontWeight: 'bold' }}>Kiosko Verificador</h1>
            <p style={{ color: 'var(--text-secondary)', fontSize: '20px' }}>Acerca el código de barras a la cámara para ver el precio.</p>
        </div>

        <div style={{ display: 'flex', gap: '32px', width: '100%', maxWidth: '1000px' }}>
            <div style={{ flex: 1, backgroundColor: 'var(--bg-secondary)', padding: '24px', borderRadius: 'var(--radius-lg)', boxShadow: 'var(--shadow-lg)' }}>
                <div id="reader" style={{ width: '100%' }}></div>
            </div>

            <div style={{ flex: 1, backgroundColor: 'var(--bg-secondary)', padding: '40px', borderRadius: 'var(--radius-lg)', boxShadow: 'var(--shadow-lg)', display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', textAlign: 'center' }}>
                {scannedResult ? (
                    <>
                        <div style={{ fontSize: '24px', color: 'var(--text-muted)', marginBottom: '8px' }}>{scannedResult.barcode}</div>
                        <h2 style={{ fontSize: '48px', fontWeight: 'bold', marginBottom: '24px' }}>{scannedResult.name}</h2>
                        <div style={{ fontSize: '64px', fontWeight: 'bold', color: 'var(--accent-success)' }}>${scannedResult.price.toFixed(2)}</div>
                        
                        {scannedResult.wholesalePrice && (
                            <div style={{ marginTop: '32px', padding: '16px', backgroundColor: 'rgba(59, 130, 246, 0.1)', borderRadius: 'var(--radius-md)', color: 'var(--accent-primary)' }}>
                                Precio Mayoreo: <b>${scannedResult.wholesalePrice.toFixed(2)}</b> (a partir de {scannedResult.wholesaleThreshold} {scannedResult.unit})
                            </div>
                        )}
                    </>
                ) : (
                    <div>
                        <Tag size={120} color="var(--bg-tertiary)" />
                        <div style={{ fontSize: '24px', color: 'var(--text-muted)', marginTop: '24px' }}>Esperando código...</div>
                        {errorMsg && <div style={{ color: 'var(--accent-danger)', marginTop: '16px', fontWeight: 'bold' }}>{errorMsg}</div>}
                    </div>
                )}
            </div>
        </div>
    </div>
  );
}
