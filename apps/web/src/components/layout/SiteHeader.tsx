'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useState } from 'react';
import styles from './SiteHeader.module.css';
import { PageContainer } from './PageContainer';
import { ButtonLink } from '@/components/ui/ButtonLink';
import { Button } from '@/components/ui/Button';
import { Drawer } from '@/components/ui/Drawer';
import { useAuth } from '@/features/auth/useAuth';
import { useLogout } from '@/features/auth/useAuthMutations';

const NAV_LINKS = [
  { href: '/find-tutors', label: 'Find Tutors' },
  { href: '/subjects', label: 'Subjects' },
  { href: '/how-it-works', label: 'How It Works' },
];

export function SiteHeader() {
  const pathname = usePathname();
  const router = useRouter();
  const [mobileOpen, setMobileOpen] = useState(false);
  const { isAuthenticated, isLoading, email } = useAuth();
  const logout = useLogout();

  const handleLogout = (afterLogout?: () => void) => {
    logout.mutate(undefined, {
      onSuccess: () => {
        afterLogout?.();
        router.push('/');
      },
    });
  };

  return (
    <header className={styles.header}>
      <PageContainer className={styles.bar}>
        <Link href="/" className={styles.logo} aria-label="TutorFlow home">
          TutorFlow
        </Link>

        <nav aria-label="Primary" className={styles.desktopNav}>
          {NAV_LINKS.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              className={[styles.navLink, pathname === link.href ? styles.navLinkActive : '']
                .filter(Boolean)
                .join(' ')}
              aria-current={pathname === link.href ? 'page' : undefined}
            >
              {link.label}
            </Link>
          ))}
        </nav>

        <div className={styles.desktopActions}>
          <ButtonLink href="/become-a-tutor" variant="secondary" size="sm">
            Become a Tutor
          </ButtonLink>
          {!isLoading && isAuthenticated ? (
            <>
              <span className={styles.accountEmail}>{email}</span>
              <Button variant="ghost" size="sm" onClick={() => handleLogout()} loading={logout.isPending}>
                Log out
              </Button>
            </>
          ) : !isLoading ? (
            <>
              <ButtonLink href="/login" variant="ghost" size="sm">
                Log in
              </ButtonLink>
              <ButtonLink href="/signup" variant="primary" size="sm">
                Sign up
              </ButtonLink>
            </>
          ) : null}
        </div>

        <button
          type="button"
          className={styles.mobileMenuButton}
          onClick={() => setMobileOpen(true)}
          aria-label="Open menu"
        >
          ☰
        </button>
      </PageContainer>

      <Drawer open={mobileOpen} onOpenChange={setMobileOpen} title="Menu">
        <nav aria-label="Primary" className={styles.mobileNavList}>
          {NAV_LINKS.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              className={styles.mobileNavLink}
              onClick={() => setMobileOpen(false)}
            >
              {link.label}
            </Link>
          ))}
        </nav>
        <div className={styles.mobileActions}>
          <ButtonLink href="/become-a-tutor" variant="secondary" onClick={() => setMobileOpen(false)}>
            Become a Tutor
          </ButtonLink>
          {!isLoading && isAuthenticated ? (
            <>
              <span className={styles.accountEmail}>{email}</span>
              <Button
                variant="ghost"
                onClick={() => handleLogout(() => setMobileOpen(false))}
                loading={logout.isPending}
              >
                Log out
              </Button>
            </>
          ) : !isLoading ? (
            <>
              <ButtonLink href="/login" variant="ghost" onClick={() => setMobileOpen(false)}>
                Log in
              </ButtonLink>
              <ButtonLink href="/signup" variant="primary" onClick={() => setMobileOpen(false)}>
                Sign up
              </ButtonLink>
            </>
          ) : null}
        </div>
      </Drawer>
    </header>
  );
}
