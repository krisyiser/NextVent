import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'NextVent | Zima Technologies',
  description: 'NextVent (NV) - Software desarrollado por Zima Technologies',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="es">
      <head>
        <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=0"/>
      </head>
      <body>
        <div className="app-container">
          {children}
        </div>
      </body>
    </html>
  );
}
