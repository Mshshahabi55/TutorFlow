'use client';

import { useSearchParams } from 'next/navigation';
import { PageContainer } from '@/components/layout/PageContainer';
import { Card } from '@/components/ui/Card';
import { LoginForm } from '@/features/auth/components/LoginForm';
import { sanitizeReturnTo } from '@/features/auth/returnTo';
import styles from '@/features/auth/components/AuthForm.module.css';

export function LoginPageContent() {
  const searchParams = useSearchParams();
  const returnTo = sanitizeReturnTo(searchParams.get('returnTo'));
  // Login's own heading/copy never changes by context (product decision: Login stays the plain
  // unified page, exactly like Preply's /login). This is read only to relay onward to the Sign up
  // link below, so a user who bounces Become-a-tutor -> Login -> Sign up still sees the
  // tutor-flavored Signup heading instead of losing that context mid-detour.
  const isTutorContext = searchParams.get('context') === 'tutor';

  return (
    <PageContainer className={styles.page}>
      <Card className={styles.card}>
        <h1 className={styles.title}>Log in</h1>
        <LoginForm returnTo={returnTo} context={isTutorContext ? 'tutor' : undefined} />
      </Card>
    </PageContainer>
  );
}
