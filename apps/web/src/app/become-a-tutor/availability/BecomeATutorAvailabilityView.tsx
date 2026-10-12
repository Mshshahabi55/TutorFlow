'use client';

import { PageContainer } from '@/components/layout/PageContainer';
import { Card } from '@/components/ui/Card';
import { ButtonLink } from '@/components/ui/ButtonLink';
import { LoadingState } from '@/components/ui/LoadingState';
import { useAuth } from '@/features/auth/useAuth';
import { TutorAvailabilityManager } from '@/features/availability/components/TutorAvailabilityManager';
import styles from './BecomeATutorAvailabilityView.module.css';

/** Independent audit, 2026-10-05: "a tutor who completed onboarding months ago has no way to
 * change their weekly schedule through the product." This is that reusable, focused surface —
 * not a Tutor Dashboard (none exists yet, and this doesn't invent one). */
export function BecomeATutorAvailabilityView() {
  const { isAuthenticated, isLoading, roles } = useAuth();
  const isTutor = isAuthenticated && roles.includes('Tutor');

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
          <h1>Manage your availability</h1>
          <p>Log in to manage the hours you&apos;re open for lessons.</p>
          <ButtonLink href="/login?returnTo=%2Fbecome-a-tutor%2Favailability">Log in</ButtonLink>
        </Card>
      </PageContainer>
    );
  }

  if (!isTutor) {
    return (
      <PageContainer className={styles.intro}>
        <Card className={styles.card}>
          <h1>Manage your availability</h1>
          <p>You&apos;ll need a tutor application before you can set your availability.</p>
          <ButtonLink href="/become-a-tutor">Start your tutor application</ButtonLink>
        </Card>
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <TutorAvailabilityManager />
    </PageContainer>
  );
}
