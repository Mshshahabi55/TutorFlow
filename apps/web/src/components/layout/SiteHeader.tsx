'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useState } from 'react';
import styles from './SiteHeader.module.css';
import { PageContainer } from './PageContainer';
import { BrandMark } from './BrandMark';
import { useHeaderVariant } from './HeaderVariantContext';
import { ButtonLink } from '@/components/ui/ButtonLink';
import { Button } from '@/components/ui/Button';
import { Avatar } from '@/components/ui/Avatar';
import { Drawer } from '@/components/ui/Drawer';
import { useAuth } from '@/features/auth/useAuth';
import { useLogout } from '@/features/auth/useAuthMutations';

const NAV_LINKS = [
  { href: '/find-tutors', label: 'Find Tutors' },
  { href: '/subjects', label: 'Subjects' },
  { href: '/how-it-works', label: 'How It Works' },
];

/**
 * Account-area destinations, shown only once authenticated and only when the signed-in account
 * actually holds a role the destination's own backend policy accepts — never a flat "show to
 * every authenticated user" list. `My Lessons` is `StudentOnly` server-side
 * (`LessonsModule.MapLessonsModule`, "List the caller's own lessons as a student") so a
 * Tutor-only account (no Student role) never sees a link into a 403. `Messages` is
 * `StudentOrTutorOnly` (`MessagingModule`), so either role qualifies. A dual-role account
 * (ADR-0002) sees both, exactly matching what each endpoint will actually let it do.
 *
 * `Saved Tutors` and `Settings` are deliberately not listed here — neither has a real destination
 * anywhere in this app yet (no `/settings` route, no standalone saved-tutors listing page, only
 * the per-profile favorite toggle already on the tutor profile page). Adding either would be a
 * placeholder link to nothing.
 */
const ACCOUNT_LINKS: { href: string; label: string; isVisible: (roles: string[]) => boolean }[] = [
  { href: '/messages', label: 'Messages', isVisible: (roles) => roles.includes('Student') || roles.includes('Tutor') },
  { href: '/my-lessons', label: 'My Lessons', isVisible: (roles) => roles.includes('Student') },
];

export function SiteHeader() {
  const pathname = usePathname();
  const router = useRouter();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [announcementDismissed, setAnnouncementDismissed] = useState(false);
  const { isAuthenticated, isLoading, email, roles } = useAuth();
  const logout = useLogout();
  const variant = useHeaderVariant();
  const isHome = variant === 'home';
  const visibleAccountLinks = ACCOUNT_LINKS.filter((link) => link.isVisible(roles));

  const handleLogout = (afterLogout?: () => void) => {
    logout.mutate(undefined, {
      onSuccess: () => {
        afterLogout?.();
        router.push('/');
      },
    });
  };

  if (variant === 'minimal') {
    return (
      <header className={`${styles.header} ${styles.headerMinimal}`}>
        <PageContainer className={styles.minimalBar}>
          <Link href="/" className={styles.logo} aria-label="TutorFlow home">
            <BrandMark size={22} />
            TutorFlow
          </Link>
          <div className={styles.minimalActions}>
            <Link href="/help" className={styles.navLink}>
              Help
            </Link>
            {email && <Avatar name={email} size="sm" />}
          </div>
        </PageContainer>
      </header>
    );
  }

  return (
    <>
      {/* Dark announcement strip above a pink nav — matches the real Preply homepage's two
          horizontal bands (header blends into the hero below). Homepage-only (`isHome`); every
          other route keeps the plain white header untouched. Dismiss is session-local component
          state, not persisted — there's no real backend/cookie infra for this today and adding
          one would be scope beyond a visual match. */}
      {isHome && !announcementDismissed && (
        <div className={styles.announcement}>
          <PageContainer className={styles.announcementBar}>
            <p>Real tutors. Real progress. Start today.</p>
            <button
              type="button"
              onClick={() => setAnnouncementDismissed(true)}
              aria-label="Dismiss announcement"
              className={styles.announcementClose}
            >
              ×
            </button>
          </PageContainer>
        </div>
      )}
      <header className={`${styles.header} ${isHome ? styles.headerHome : ''}`}>
        <PageContainer className={styles.bar}>
          <Link href="/" className={styles.logo} aria-label="TutorFlow home">
            <BrandMark size={24} />
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
              {visibleAccountLinks.length > 0 && (
                <nav aria-label="Account" className={styles.accountNav}>
                  {visibleAccountLinks.map((link) => (
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
              )}
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
              {visibleAccountLinks.length > 0 && (
                <nav aria-label="Account" className={styles.mobileNavList}>
                  {visibleAccountLinks.map((link) => (
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
              )}
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
    </>
  );
}
