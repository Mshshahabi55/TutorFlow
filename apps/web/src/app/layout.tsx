import type { Metadata } from 'next';
import './globals.css';
import { Providers } from './providers';
import { SiteHeader } from '@/components/layout/SiteHeader';
import { SiteFooter } from '@/components/layout/SiteFooter';
import { HeaderVariantProvider } from '@/components/layout/HeaderVariantContext';
import { TooltipProvider } from '@/components/ui/Tooltip';
import { ToastProvider } from '@/components/ui/Toast';

export const metadata: Metadata = {
  title: 'TutorFlow — Find your perfect tutor',
  description: 'TutorFlow is a marketplace connecting students with tutors for live online lessons.',
};

export default function RootLayout({ children }: LayoutProps<'/'>) {
  return (
    <html lang="en">
      <body>
        <a href="#main-content" className="visually-hidden">
          Skip to main content
        </a>
        <Providers>
          <TooltipProvider>
            <ToastProvider>
              <HeaderVariantProvider>
                <SiteHeader />
                <main id="main-content">{children}</main>
              </HeaderVariantProvider>
              <SiteFooter />
            </ToastProvider>
          </TooltipProvider>
        </Providers>
      </body>
    </html>
  );
}
