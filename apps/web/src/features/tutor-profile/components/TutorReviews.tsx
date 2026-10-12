'use client';

import { useState } from 'react';
import { EmptyState } from '@/components/ui/EmptyState';
import { ErrorState } from '@/components/ui/ErrorState';
import { LoadingState } from '@/components/ui/LoadingState';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Rating } from '@/components/ui/Rating';
import { useReviewEligibility, useSubcategoryRatingEligibility, useTutorReviewSummary } from '@/features/reviews/useReviews';
import { SubRatingDimension, TutorReviewSubRatings } from '@/features/reviews/types';
import { ProfileSection } from './ProfileSection';
import { ReviewForm } from './ReviewForm';
import { SubcategoryRatingForm } from './SubcategoryRatingForm';
import styles from './TutorReviews.module.css';

const SUBCATEGORY_LABELS: { key: keyof TutorReviewSubRatings; label: string }[] = [
  { key: 'clarity', label: 'Clarity' },
  { key: 'progress', label: 'Progress' },
  { key: 'preparation', label: 'Preparation' },
  { key: 'reassurance', label: 'Reassurance' },
];

function SubRatingRow({ label, dimension }: { label: string; dimension: SubRatingDimension }) {
  return (
    <div className={styles.subRatingRow}>
      <span className={styles.subRatingLabel}>{label}</span>
      <Rating value={dimension.average ?? 0} reviewCount={dimension.count} />
    </div>
  );
}

// DOCUMENTED (help.preply.com/articles/11196386, verbatim): "Subcategory reviews appear on your
// profile once you've received at least one rating in all four categories... If you only have
// ratings in some categories, the section will not appear yet." The backend intentionally keeps
// returning nullable per-dimension data even when incomplete (useful internally/for a future
// tutor-facing dashboard) — this gate is purely a public-display rule, enforced here, not by
// withholding the data from the API.
function allSubRatingDimensionsPopulated(subRatings: TutorReviewSubRatings): boolean {
  return SUBCATEGORY_LABELS.every(({ key }) => subRatings[key].count > 0);
}

export interface TutorReviewsProps {
  tutorUserId: string;
  isAuthenticatedStudent: boolean;
  onRequireAuth: (action: string) => void;
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

/** Reviews are a real Student<->Tutor relationship (Product Owner decision, documented Preply
 * behavior: eligible after the 3rd completed lesson with this specific tutor; one editable
 * review per relationship, never a per-lesson one) — the action area below reflects exactly
 * that relationship state, not an individual lesson. */
export function TutorReviews({ tutorUserId, isAuthenticatedStudent, onRequireAuth }: TutorReviewsProps) {
  const [formOpen, setFormOpen] = useState(false);
  const summary = useTutorReviewSummary(tutorUserId);
  const eligibility = useReviewEligibility(tutorUserId, isAuthenticatedStudent);
  const subcategoryEligibility = useSubcategoryRatingEligibility(tutorUserId, isAuthenticatedStudent);

  const reviewAction = (() => {
    if (!isAuthenticatedStudent) {
      return (
        <Button variant="secondary" onClick={() => onRequireAuth('leave a review')}>
          Log in to leave a review
        </Button>
      );
    }

    if (eligibility.isLoading) {
      return <LoadingState label="Checking review eligibility…" />;
    }

    if (eligibility.isError || !eligibility.data) {
      return null;
    }

    const { isEligible, completedLessonCount, requiredCompletedLessonCount, existingReview } = eligibility.data;

    if (!isEligible) {
      return (
        <p className={styles.notEligible}>
          You can leave a review after completing {requiredCompletedLessonCount} lessons with this tutor
          ({completedLessonCount} of {requiredCompletedLessonCount} so far).
        </p>
      );
    }

    if (formOpen) {
      return <ReviewForm tutorUserId={tutorUserId} existingReview={existingReview} />;
    }

    if (existingReview) {
      return (
        <div className={styles.existingReview}>
          <p className={styles.yourReviewLabel}>Your review</p>
          <Rating value={existingReview.rating} reviewCount={1} />
          {existingReview.comment && <p className={styles.comment}>{existingReview.comment}</p>}
          <Button variant="secondary" onClick={() => setFormOpen(true)}>
            Edit your review
          </Button>
        </div>
      );
    }

    return (
      <Button onClick={() => setFormOpen(true)}>Write a review</Button>
    );
  })();

  // Deliberately a separate action from `reviewAction` above — DOCUMENTED, a distinct eligibility
  // rule (>=1 completed lesson, not 3) and a distinct, non-editable, once-per-month submission.
  const subcategoryAction = (() => {
    if (!isAuthenticatedStudent) {
      return null;
    }

    if (subcategoryEligibility.isLoading) {
      return <LoadingState label="Checking eligibility…" />;
    }

    if (subcategoryEligibility.isError || !subcategoryEligibility.data) {
      return null;
    }

    const { isEligible, hasCompletedAtLeastOneLesson, alreadySubmittedThisMonth } = subcategoryEligibility.data;

    if (!hasCompletedAtLeastOneLesson) {
      return null;
    }

    if (alreadySubmittedThisMonth) {
      return <p className={styles.notEligible}>You&apos;ve already rated specific aspects for this tutor this month.</p>;
    }

    if (!isEligible) {
      return null;
    }

    return <SubcategoryRatingForm tutorUserId={tutorUserId} />;
  })();

  return (
    <ProfileSection title="Reviews">
      {summary.isLoading ? (
        <LoadingState label="Loading reviews…" />
      ) : summary.isError ? (
        <ErrorState
          title="We couldn't load reviews"
          description="Please try again."
          onRetry={() => summary.refetch()}
        />
      ) : (
        <>
          {summary.data?.subRatings && allSubRatingDimensionsPopulated(summary.data.subRatings) && (
            <div className={styles.subRatings}>
              <p className={styles.subRatingsHeading}>Specific aspects</p>
              {SUBCATEGORY_LABELS.map(({ key, label }) => (
                <SubRatingRow key={key} label={label} dimension={summary.data!.subRatings![key]} />
              ))}
            </div>
          )}

          {summary.data && summary.data.reviewCount > 0 ? (
            <>
              <Rating value={summary.data.overallRating} reviewCount={summary.data.reviewCount} />
              <ul className={styles.list}>
                {summary.data.reviews.map((review, index) => (
                  <li key={index} className={styles.entry}>
                    <Card>
                      <div className={styles.entryHeader}>
                        <Rating value={review.rating} reviewCount={1} />
                        <span className={styles.date}>{formatDate(review.createdAtUtc)}</span>
                      </div>
                      {review.comment && <p className={styles.comment}>{review.comment}</p>}
                    </Card>
                  </li>
                ))}
              </ul>
            </>
          ) : (
            <EmptyState
              title="No reviews yet"
              description="This tutor hasn't received any reviews yet. Reviews appear here after students complete lessons."
            />
          )}

          <div className={styles.actionArea}>{reviewAction}</div>
          {subcategoryAction && <div className={styles.actionArea}>{subcategoryAction}</div>}
        </>
      )}
    </ProfileSection>
  );
}
