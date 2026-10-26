'use client';

import { Avatar } from '@/components/ui/Avatar';
import { FlagIcon } from '@/components/ui/FlagIcon';
import { Rating } from '@/components/ui/Rating';
import { Skeleton } from '@/components/ui/Skeleton';
import { useTutorReviewSummary } from '@/features/reviews/useReviews';
import { TutorTrustBadges } from './TutorTrustBadges';
import { TutorPublicProfile } from '../types';
import { resolveMediaUrl } from '../mediaUrl';
import styles from './TutorProfileHero.module.css';

export function TutorProfileHero({ profile }: { profile: TutorPublicProfile }) {
  const reviewSummary = useTutorReviewSummary(profile.tutorUserId);

  return (
    <div className={styles.root}>
      <Avatar src={resolveMediaUrl(profile.profilePhotoReference)} name={profile.displayName} size="lg" />
      <div className={styles.identity}>
        <h1 className={styles.name}>{profile.displayName}</h1>
        {profile.headline && <p className={styles.headline}>{profile.headline}</p>}
        {profile.countryOfBirthCode && (
          <p className={styles.origin}>
            <FlagIcon code={profile.countryOfBirthCode} />{' '}
            <span className="visually-hidden">Country of birth: </span>
            {profile.countryOfBirthCode}
          </p>
        )}
        {/* Shares the same authoritative summary TutorReviews renders further down this page
            (useTutorReviewSummary) — never a locally-invented value. While it's in flight, a
            skeleton stands in; on failure, the line is simply omitted rather than asserting a
            confirmed "No reviews yet" the data never actually confirmed. */}
        {reviewSummary.isLoading ? (
          <Skeleton width="8rem" />
        ) : (
          reviewSummary.data && <Rating value={reviewSummary.data.overallRating} reviewCount={reviewSummary.data.reviewCount} />
        )}
        <div className={styles.badgeRow}>
          <TutorTrustBadges isSuperTutor={profile.isSuperTutor} isProfessionalTutor={profile.isProfessionalTutor} />
        </div>
      </div>
    </div>
  );
}
