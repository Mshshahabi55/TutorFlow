'use client';

import { useSearchParams } from 'next/navigation';
import { PageContainer } from '@/components/layout/PageContainer';
import { Card } from '@/components/ui/Card';
import { SignupForm } from '@/features/auth/components/SignupForm';
import { sanitizeReturnTo } from '@/features/auth/returnTo';
import styles from '@/features/auth/components/AuthForm.module.css';

export function SignupPageContent() {
  const searchParams = useSearchParams();
  const returnTo = sanitizeReturnTo(searchParams.get('returnTo'));
  // Explicit context param, not inferred from returnTo — mirrors how the real Preply journey
  // tracks entry point (/en/login?source_page=teach-signup) rather than coupling UI copy to a
  // specific redirect destination. Set by BecomeATutorView's "Sign up to apply" link only.
  const isTutorContext = searchParams.get('context') === 'tutor';

  return (
    <PageContainer className={styles.page}>
      <Card className={styles.card}>
        <h1 className={styles.title}>{isTutorContext ? 'Sign up as a tutor' : 'Sign up'}</h1>
        <SignupForm returnTo={returnTo} context={isTutorContext ? 'tutor' : 'student'} />
      </Card>
    </PageContainer>
  );
}
