'use client';

import { useSearchParams } from 'next/navigation';
import { PageContainer } from '@/components/layout/PageContainer';
import { Card } from '@/components/ui/Card';
import { LoginForm } from '@/features/auth/components/LoginForm';
import styles from '@/features/auth/components/AuthForm.module.css';

export function LoginPageContent() {
  const searchParams = useSearchParams();
  const returnTo = searchParams.get('returnTo') || '/';

  return (
    <PageContainer className={styles.page}>
      <Card className={styles.card}>
        <h1 className={styles.title}>Log in</h1>
        <LoginForm returnTo={returnTo} />
      </Card>
    </PageContainer>
  );
}
