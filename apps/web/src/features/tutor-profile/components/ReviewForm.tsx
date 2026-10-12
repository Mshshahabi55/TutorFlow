'use client';

import { FormEvent, useId, useState } from 'react';
import { Button } from '@/components/ui/Button';
import { Textarea } from '@/components/ui/Textarea';
import { Alert } from '@/components/ui/Alert';
import { StarRatingInput } from '@/components/ui/StarRatingInput';
import { ApiError } from '@/lib/api/client';
import { useSubmitReview } from '@/features/reviews/useReviews';
import { ExistingReview } from '@/features/reviews/types';
import styles from './ReviewForm.module.css';

export interface ReviewFormProps {
  tutorUserId: string;
  existingReview: ExistingReview | null;
}

/** Student <-> Tutor relationship review (Product Owner decision — documented Preply behavior):
 * one editable review per tutor, never a per-lesson form. `existingReview` present means this
 * renders as an edit; absent means a first-time create — same form either way, since the backend
 * itself treats submission as an upsert. */
export function ReviewForm({ tutorUserId, existingReview }: ReviewFormProps) {
  const [rating, setRating] = useState<number | null>(existingReview?.rating ?? null);
  const [comment, setComment] = useState(existingReview?.comment ?? '');
  const [validationError, setValidationError] = useState<string | null>(null);
  const submitReview = useSubmitReview(tutorUserId);
  const commentId = useId();

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (rating === null) {
      setValidationError('Choose a rating before submitting.');
      return;
    }
    setValidationError(null);
    submitReview.mutate({ rating, comment: comment.trim() || null });
  };

  const serverErrorMessage = (() => {
    if (!submitReview.isError) return null;
    const error = submitReview.error;
    if (error instanceof ApiError) {
      return error.problem.detail ?? error.problem.title ?? 'Could not submit your review. Please try again.';
    }
    return 'Could not submit your review. Please try again.';
  })();

  return (
    <form className={styles.form} onSubmit={handleSubmit}>
      <div className={styles.field}>
        <span className={styles.label} id={`${commentId}-rating-label`}>
          Your rating
        </span>
        <StarRatingInput
          name="rating"
          value={rating}
          onChange={(value) => {
            setRating(value);
            setValidationError(null);
          }}
          aria-describedby={validationError ? `${commentId}-rating-error` : undefined}
          aria-invalid={Boolean(validationError)}
        />
        {validationError && (
          <span id={`${commentId}-rating-error`} role="alert" className={styles.error}>
            {validationError}
          </span>
        )}
      </div>

      <div className={styles.field}>
        <label htmlFor={commentId} className={styles.label}>
          Comment <span className={styles.optional}>(optional)</span>
        </label>
        <Textarea
          id={commentId}
          value={comment}
          onChange={(e) => setComment(e.target.value)}
          rows={4}
          maxLength={2000}
        />
      </div>

      {serverErrorMessage && (
        <Alert variant="error" title="Couldn't submit your review">
          {serverErrorMessage}
        </Alert>
      )}

      {submitReview.isSuccess && (
        <Alert variant="success" title={existingReview ? 'Review updated' : 'Review submitted'}>
          Thanks for sharing your feedback.
        </Alert>
      )}

      <Button type="submit" loading={submitReview.isPending}>
        {existingReview ? 'Update review' : 'Submit review'}
      </Button>
    </form>
  );
}
