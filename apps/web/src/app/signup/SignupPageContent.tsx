'use client';

import { useSearchParams } from 'next/navigation';
import { PageContainer } from '@/components/layout/PageContainer';
import { Card } from '@/components/ui/Card';
import { SignupForm } from '@/features/auth/components/SignupForm';
import styles from '@/features/auth/components/AuthForm.module.css';

export function SignupPageContent() {
  const searchParams = useSearchParams();
  const returnTo = searchParams.get('returnTo') || '/';

  return (
    <PageContainer className={styles.page}>
      <Card className={styles.card}>
        <h1 className={styles.title}>Sign up</h1>
        <SignupForm returnTo={returnTo} />
      </Card>
    </PageContainer>
  );
}
