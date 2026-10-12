'use client';

import { useState } from 'react';
import Link from 'next/link';
import { LoadingState } from '@/components/ui/LoadingState';
import { ErrorState } from '@/components/ui/ErrorState';
import { PageContainer } from '@/components/layout/PageContainer';
import { useAuth } from '@/features/auth/useAuth';
import { AuthGateModal } from '@/components/auth/AuthGateModal';
import { useTutorProfile } from '../useTutorProfile';
import { TutorPublicProfile } from '../types';
import { TutorProfileHero } from './TutorProfileHero';
import { TutorIntroVideo } from './TutorIntroVideo';
import { TutorAbout } from './TutorAbout';
import { TutorSubjectsAndSpecialties } from './TutorSubjectsAndSpecialties';
import { TutorLanguages } from './TutorLanguages';
import { TutorCredentials } from './TutorCredentials';
import { TutorAvailability } from './TutorAvailability';
import { TutorReviews } from './TutorReviews';
import { SimilarTutors } from './SimilarTutors';
import { BookingPanel } from './BookingPanel';
import styles from './TutorProfileView.module.css';

export function TutorProfileView({ slug, initialProfile }: { slug: string; initialProfile: TutorPublicProfile }) {
  const { data: profile, isLoading, isError, refetch } = useTutorProfile(slug, initialProfile);
  const { isAuthenticated, roles } = useAuth();
  const isStudent = isAuthenticated && roles.includes('Student');
  const isTutorOnlyViewer = isAuthenticated && roles.includes('Tutor') && !isStudent;
  const [authGate, setAuthGate] = useState<{ open: boolean; action: string }>({ open: false, action: '' });

  const requireAuth = (action: string) => setAuthGate({ open: true, action });

  if (isLoading) {
    return (
      <PageContainer>
        <LoadingState />
      </PageContainer>
    );
  }

  if (isError || !profile) {
    return (
      <PageContainer>
        <ErrorState
          title="Couldn't load this profile"
          description="Something went wrong loading this tutor's profile."
          onRetry={() => refetch()}
        />
      </PageContainer>
    );
  }

  return (
    <PageContainer className={styles.layout}>
      <nav className={styles.breadcrumb} aria-label="Breadcrumb">
        <Link href="/find-tutors">Find tutors</Link>
        {' › '}
        <span>{profile.displayName}</span>
      </nav>

      <TutorProfileHero profile={profile} />

      <TutorIntroVideo
        introVideoReference={profile.introVideoReference}
        profilePhotoReference={profile.profilePhotoReference}
        displayName={profile.displayName}
      />

      <div className={styles.grid}>
        <div className={styles.main}>
          <TutorAbout about={profile.about} />
          <TutorSubjectsAndSpecialties subjectIds={profile.subjectIds} specialtyIds={profile.specialtyIds} />
          <TutorLanguages spokenLanguages={profile.spokenLanguages} />
          <TutorCredentials
            yearsOfExperience={profile.yearsOfExperience}
            experienceSummary={profile.experienceSummary}
            educationEntries={profile.educationEntries}
            certificateEntries={profile.certificateEntries}
          />
          <TutorAvailability
            tutorUserId={profile.tutorUserId}
            tutorSlug={profile.slug}
            isAcceptingNewStudents={profile.isAcceptingNewStudents}
            isAuthenticatedStudent={isStudent}
            onRequireAuth={requireAuth}
          />
          <TutorReviews
            tutorUserId={profile.tutorUserId}
            isAuthenticatedStudent={isStudent}
            onRequireAuth={requireAuth}
          />
          <SimilarTutors
            subjectId={profile.subjectIds[0]}
            excludeTutorUserId={profile.tutorUserId}
            isStudent={isStudent}
            isTutorOnlyViewer={isTutorOnlyViewer}
            onRequireAuth={requireAuth}
          />
        </div>

        <div className={styles.sidebar}>
          <BookingPanel profile={profile} onRequireAuth={requireAuth} />
        </div>
      </div>

      <AuthGateModal
        open={authGate.open}
        onOpenChange={(open) => setAuthGate((prev) => ({ ...prev, open }))}
        action={authGate.action}
        returnTo={`/tutors/${slug}`}
      />
    </PageContainer>
  );
}
