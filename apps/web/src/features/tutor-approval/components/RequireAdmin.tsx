'use client';

import { ReactNode } from 'react';
import { PageContainer } from '@/components/layout/PageContainer';
import { Card } from '@/components/ui/Card';
import { ButtonLink } from '@/components/ui/ButtonLink';
import { LoadingState } from '@/components/ui/LoadingState';
import { useAuth } from '@/features/auth/useAuth';
import styles from './RequireAdmin.module.css';

/**
 * Client-side gate for the admin surface — a UX convenience only, never the real authorization
 * boundary (Phase 4, ADR-0028): every admin endpoint independently enforces
 * `PolicyNames.AdminOnly` server-side regardless of what this component renders. Hiding the
 * admin nav/pages from a non-admin is about not showing a confusing 403 page, not security.
 */
export function RequireAdmin({ children }: { children: ReactNode }) {
  const { isAuthenticated, isLoading, roles } = useAuth();

  if (isLoading) {
    return (
      <PageContainer>
        <LoadingState />
      </PageContainer>
    );
  }

  if (!isAuthenticated) {
    return (
      <PageContainer className={styles.intro}>
        <Card className={styles.card}>
          <h1>Admin sign-in required</h1>
          <p>This area is for TutorFlow staff only.</p>
          <ButtonLink href="/login?returnTo=%2Fadmin%2Ftutor-applications">Log in</ButtonLink>
        </Card>
      </PageContainer>
    );
  }

  if (!roles.includes('Admin')) {
    return (
      <PageContainer className={styles.intro}>
        <Card className={styles.card}>
          <h1>Not authorized</h1>
          <p>Your account doesn&apos;t have access to the admin area.</p>
        </Card>
      </PageContainer>
    );
  }

  return <>{children}</>;
}
