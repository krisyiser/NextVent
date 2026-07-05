// src/components/ReceiptTemplate.tsx
'use client';

import React, { useState, useEffect } from 'react';
import QRCode from 'qrcode';
import { Sale } from '@/types';
import { getSetting } from '@/lib/storage';
import { useTheme } from './ThemeProvider';

type ReceiptTemplateProps = {
  sale: Sale;
};

export const ReceiptTemplate = ({ sale }: ReceiptTemplateProps) => {
  const { logo, storeName } = useTheme();
  const [address, setAddress] = useState('');
  const [phone, setPhone] = useState('');
  const [footerMessage, setFooterMessage] = useState('¡Gracias por su compra!');
  const [paperWidth, setPaperWidth] = useState('80mm');
  const [fontSize, setFontSize] = useState('12px');
  const [qrDataUrl, setQrDataUrl] = useState<string>('');

  useEffect(() => {
    (async () => {
        setAddress(await getSetting('pos_store_address', ''));
        setPhone(await getSetting('pos_store_phone', ''));
        setFooterMessage(await getSetting('pos_ticket_footer', '¡Gracias por su compra!'));
        setPaperWidth(await getSetting('pos_ticket_width', '80mm'));
        setFontSize(await getSetting('pos_ticket_font', '12px'));
    })();
    // Generate QR code for sale id (or full sale JSON)
    const qrContent = JSON.stringify({ id: sale.id, total: sale.total, date: sale.date });
    QRCode.toDataURL(qrContent)
      .then(url => setQrDataUrl(url))
      .catch(err => console.error('QR generation error', err));
  }, [sale]);

  return (
    <div className="receipt-container" id="receipt-print">
      <style jsx>{`
        @media screen {#receipt-print { display: none; }}
        @media print {body * { visibility: hidden; }#receipt-print, #receipt-print * { visibility: visible; }#receipt-print {position: absolute; left: 0; top: 0; width: ${paperWidth}; padding: 5mm; font-family: 'Courier New', Courier, monospace; font-size: ${fontSize}; color: #000;}}
      `}</style>

      <div className="receipt-header">
        {logo && (
          <div style={{ textAlign: 'center', marginBottom: '8px' }}>
            <img src={logo} alt="Company Logo" style={{ maxHeight: '50px', maxWidth: '120px', objectFit: 'contain', filter: 'grayscale(100%)' }} />
          </div>
        )}
        <h1>{storeName}</h1>
        {address && <p style={{ margin: '2px 0' }}>{address}</p>}
        {phone && <p style={{ margin: '2px 0' }}>Tel: {phone}</p>}
        <p style={{ margin: '2px 0' }}>Venta #{sale.id.split('-')[1]}</p>
        <p style={{ margin: '2px 0' }}>{new Date(sale.date).toLocaleString()}</p>
      </div>

      <div className="receipt-divider" />

      <div className="receipt-items">
        {sale.items.map((item, idx) => (
          <div key={idx} style={{ marginBottom: '8px' }}>
            <div className="receipt-item">
              <span>{item.name}</span>
              <span>${item.total.toFixed(2)}</span>
            </div>
            <div className="receipt-item-details">
              {item.quantity} {item.unit} x ${item.price.toFixed(2)}
            </div>
          </div>
        ))}
      </div>

      <div className="receipt-divider" />

      <div className="receipt-total">
        <span>TOTAL</span>
        <span>${sale.total.toFixed(2)}</span>
      </div>

      <div className="receipt-item" style={{ marginTop: '5px' }}>
        <span>Metodo:</span>
        <span>{sale.paymentMethod}</span>
      </div>

      {sale.paymentMethod === 'Cash' && (
        <>
          <div className="receipt-item">
            <span>Pagó con:</span>
            <span>${sale.paidAmount.toFixed(2)}</span>
          </div>
          <div className="receipt-item">
            <span>Cambio:</span>
            <span>${sale.changeAmount.toFixed(2)}</span>
          </div>
        </>
      )}

      {/* QR Code section */}
      {qrDataUrl && (
        <div style={{ textAlign: 'center', marginTop: '10px' }}>
          <img src={qrDataUrl} alt="QR code for sale" style={{ width: '80px', height: '80px' }} />
        </div>
      )}

      <div className="receipt-footer">
        <p style={{ fontWeight: 'bold' }}>{footerMessage}</p>
        <p style={{ marginTop: '10px' }}>Software by Antigravity</p>
      </div>
    </div>
  );
};
