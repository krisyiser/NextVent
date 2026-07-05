import type { Metadata } from 'next';
import './globals.css';
import { AuthProvider } from '../auth/AuthProvider';
import { ThemeProvider } from '../components/ThemeProvider';
import { AuthGuard } from '../components/AuthGuard';
import { Toaster } from 'sonner';
import { WhatsappPanelProvider } from '../components/WhatsappPanelProvider';

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
        <meta
          name="viewport"
          content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=0"
        />
      </head>
      <body>
        <Toaster richColors position="top-right" />
        <AuthProvider>
          <ThemeProvider>
            <AuthGuard>
              <WhatsappPanelProvider>
                {children}
              </WhatsappPanelProvider>
            </AuthGuard>
          </ThemeProvider>
        </AuthProvider>
      </body>
    </html>
  );
}

