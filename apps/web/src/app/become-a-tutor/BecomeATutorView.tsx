'use client';

import { useState } from 'react';
import { PageContainer } from '@/components/layout/PageContainer';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { ButtonLink } from '@/components/ui/ButtonLink';
import { LoadingState } from '@/components/ui/LoadingState';
import { ErrorState } from '@/components/ui/ErrorState';
import { useAuth } from '@/features/auth/useAuth';
import { useBecomeTutor } from '@/features/auth/useAuthMutations';
import { ApiError } from '@/lib/api/client';
import { OnboardingWizard } from '@/features/tutor-onboarding/components/OnboardingWizard';
import { useCreateTutorApplication, useCurrentTutorApplication } from '@/features/tutor-onboarding/useTutorOnboarding';
import styles from './BecomeATutorView.module.css';

/** Entry point for the real tutor application (ADR-0026) — replaces the previous `ComingSoon`
 * placeholder. Starting an application is two real steps the UI collapses into one click:
 * granting the caller's own account the Tutor role (idempotent, `becomeTutor`), then creating
 * the `Draft` application — both against real, already-built backend endpoints. */
export function BecomeATutorView() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const becomeTutor = useBecomeTutor();
  const createApplication = useCreateTutorApplication();
  const [startError, setStartError] = useState<string | null>(null);

  const { data: application, isLoading: applicationLoading, isError, refetch } = useCurrentTutorApplication(isAuthenticated);

  const handleStart = async () => {
    setStartError(null);
    try {
      await becomeTutor.mutateAsync();
      await createApplication.mutateAsync();
      await refetch();
    } catch (error) {
      setStartError(error instanceof ApiError ? error.problem.detail ?? error.message : 'Could not start your application.');
    }
  };

  if (authLoading) {
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
          <h1>Become a tutor</h1>
          <p>
            Teach students from around the world on your own schedule. Create an account to start your tutor
            application — profile photo, subjects, intro video, availability, and your rate.
          </p>
          <div className={styles.actions}>
            <ButtonLink href="/signup?returnTo=%2Fbecome-a-tutor">Sign up to apply</ButtonLink>
            <ButtonLink href="/login?returnTo=%2Fbecome-a-tutor" variant="secondary">
              Log in
            </ButtonLink>
          </div>
        </Card>
      </PageContainer>
    );
  }

  if (applicationLoading) {
    return (
      <PageContainer>
        <LoadingState />
      </PageContainer>
    );
  }

  if (isError) {
    return (
      <PageContainer>
        <ErrorState
          title="Couldn't load your application"
          description="Something went wrong checking your tutor application status."
          onRetry={() => refetch()}
        />
      </PageContainer>
    );
  }

  if (!application) {
    return (
      <PageContainer className={styles.intro}>
        <Card className={styles.card}>
          <h1>Become a tutor</h1>
          <p>
            Your application has a few sections: the basics, a profile photo, what you teach, education and
            certifications, an intro video, your availability, and your rate. You can save your progress and come
            back anytime before submitting.
          </p>
          {startError && (
            <p className={styles.error} role="alert">
              {startError}
            </p>
          )}
          <Button onClick={handleStart} loading={becomeTutor.isPending || createApplication.isPending}>
            Start application
          </Button>
        </Card>
      </PageContainer>
    );
  }

  return (
    <PageContainer className={styles.wizardPage}>
      <OnboardingWizard application={application} />
    </PageContainer>
  );
}
