'use client';

import React, { useState, useEffect } from 'react';
import { Sale, SaleItemSnapshot } from '@/types';

type ReceiptTemplateProps = {
  sale: Sale;
};

export const ReceiptTemplate = ({ sale }: ReceiptTemplateProps) => {
  const [storeName, setStoreName] = useState('NEXT VENT POS');
  const [address, setAddress] = useState('');
  const [phone, setPhone] = useState('');
  const [footerMessage, setFooterMessage] = useState('¡Gracias por su compra!');
  const [paperWidth, setPaperWidth] = useState('80mm');
  const [fontSize, setFontSize] = useState('12px');

  useEffect(() => {
    setStoreName(localStorage.getItem('pos_store_name') || 'NEXT VENT POS');
    setAddress(localStorage.getItem('pos_store_address') || '');
    setPhone(localStorage.getItem('pos_store_phone') || '');
    setFooterMessage(localStorage.getItem('pos_ticket_footer') || '¡Gracias por su compra!');
    setPaperWidth(localStorage.getItem('pos_ticket_width') || '80mm');
    setFontSize(localStorage.getItem('pos_ticket_font') || '12px');
  }, []);

  return (
    <div className="receipt-container" id="receipt-print">
      <style jsx>{`
        @media screen {
          #receipt-print { display: none; }
        }
        @media print {
          body * { visibility: hidden; }
          #receipt-print, #receipt-print * { visibility: visible; }
          #receipt-print {
            position: absolute;
            left: 0;
            top: 0;
            width: ${paperWidth};
            padding: 5mm;
            font-family: 'Courier New', Courier, monospace;
            font-size: ${fontSize};
            color: #000;
          }
          .receipt-header { text-align: center; margin-bottom: 10px; }
          .receipt-header h1 { font-size: 16px; margin: 0; }
          .receipt-divider { border-top: 1px dashed #000; margin: 10px 0; }
          .receipt-item { display: flex; justify-content: space-between; margin-bottom: 5px; }
          .receipt-item-details { font-size: 10px; }
          .receipt-total { display: flex; justify-content: space-between; font-weight: bold; font-size: 14px; margin-top: 10px; }
          .receipt-footer { text-align: center; margin-top: 20px; font-size: 10px; }
        }
      `}</style>

      <div className="receipt-header">
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

      <div className="receipt-footer">
        <p style={{ fontWeight: 'bold' }}>{footerMessage}</p>
        <p style={{ marginTop: '10px' }}>Software by Antigravity</p>
      </div>
    </div>
  );
};
