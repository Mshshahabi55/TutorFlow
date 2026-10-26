'use client';

import { useState } from 'react';
import Link from 'next/link';
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

/** Mirrors the real Preply recruitment page's (`preply.com/en/teach`) own evidenced 3-step
 * process copy ("Sign up to create your tutor profile" / "Get approved by our team..." / "Start
 * earning...") — reworded in TutorFlow's own voice, not copied verbatim. No specific approval
 * timeframe is stated: Preply's own official sources conflict on this (3 vs. 5 business days,
 * docs/product/preply-reverse-engineering/07-wizard-inventory.md), and TutorFlow has no reviewer
 * SLA of its own to honestly commit to one. */
const PROCESS_STEPS = [
  {
    title: 'Create your profile',
    body: 'Tell us about yourself, add a photo and intro video, and set the subjects you teach.',
  },
  {
    title: 'Get reviewed',
    body: 'Our team checks every application before it goes live, so students can trust who they book.',
  },
  {
    title: 'Start teaching',
    body: 'Once approved, your profile is live — set your schedule and start taking lessons.',
  },
];

/** Qualitative capability statements only — no invented student/tutor counts, country counts, or
 * earning figures (same discipline as the homepage's own `TRUST_ITEMS`, `page.tsx`). */
const BENEFIT_ITEMS = [
  { title: 'Set your own rate', body: 'You decide what to charge, and can adjust it any time.' },
  { title: 'Teach on your schedule', body: 'Pick the hours that work for you — no fixed shifts.' },
  { title: 'Teach any subject', body: 'Languages, academics, test prep, and more.' },
  { title: 'Reach students anywhere', body: 'Connect with learners outside your local area.' },
];

/** Entry point for the real tutor application (ADR-0026) — replaces the previous `ComingSoon`
 * placeholder. Starting an application is two real steps the UI collapses into one click:
 * granting the caller's own account the Tutor role (idempotent, `becomeTutor`), then creating
 * the `Draft` application — both against real, already-built backend endpoints. */
export function BecomeATutorView() {
  const { isAuthenticated, isLoading: authLoading, roles } = useAuth();
  const isTutor = roles.includes('Tutor');
  const becomeTutor = useBecomeTutor();
  const createApplication = useCreateTutorApplication();
  const [startError, setStartError] = useState<string | null>(null);

  // Only a caller who already holds the Tutor role can ever succeed against
  // `GET /api/tutors/applications/me` — it's gated by the group-level `TutorOnly` policy
  // (`TutorsModule.cs`), with no per-endpoint exception. A Student who hasn't started an
  // application yet is not a Tutor yet either, so this query must stay disabled until
  // `becomeTutor()` grants the role — otherwise every first-time visitor gets a 403 the view
  // can't distinguish from a real failure, and never reaches the Start application action below.
  const {
    data: application,
    isLoading: applicationLoading,
    isError,
    refetch,
  } = useCurrentTutorApplication(isAuthenticated && isTutor);

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
      <>
        <section className={styles.hero}>
          <PageContainer className={styles.heroInner}>
            <h1 className={styles.heroTitle}>Share what you know. Earn teaching it.</h1>
            <p className={styles.heroBody}>
              Set your own rate, teach on your own schedule, and connect with students looking for exactly what
              you offer.
            </p>
            <div className={styles.actions}>
              <ButtonLink href="/signup?context=tutor&returnTo=%2Fbecome-a-tutor" className={styles.heroCta}>
                Create your tutor profile
              </ButtonLink>
            </div>
            {/* A plain text link, not a second equal-weight button — the site header already has
                its own prominent "Log in" link for a returning applicant; this page's own primary
                action is signing up, not logging in. */}
            <p className={styles.loginHint}>
              Already started an application?{' '}
              <Link href="/login?context=tutor&returnTo=%2Fbecome-a-tutor">Log in</Link>
            </p>
          </PageContainer>
        </section>

        <section className={styles.howItWorks}>
          <PageContainer>
            <h2 className={styles.sectionTitle}>How it works</h2>
            <ol className={styles.stepsGrid}>
              {PROCESS_STEPS.map((step, index) => (
                <li key={step.title} className={styles.stepCard}>
                  <span className={styles.stepNumber} aria-hidden="true">
                    {index + 1}
                  </span>
                  <h3 className={styles.stepTitle}>{step.title}</h3>
                  <p className={styles.stepBody}>{step.body}</p>
                </li>
              ))}
            </ol>
          </PageContainer>
        </section>

        <section className={styles.benefits}>
          <PageContainer className={styles.benefitsInner}>
            {BENEFIT_ITEMS.map((item) => (
              <div key={item.title} className={styles.benefitItem}>
                <p className={styles.benefitTitle}>{item.title}</p>
                <p className={styles.benefitBody}>{item.body}</p>
              </div>
            ))}
          </PageContainer>
        </section>
      </>
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
