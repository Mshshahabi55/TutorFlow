'use client';

import { FormEvent, useId, useState } from 'react';
import { StarRatingInput } from '@/components/ui/StarRatingInput';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { ApiError } from '@/lib/api/client';
import { useSubmitSubcategoryRating } from '@/features/reviews/useReviews';
import styles from './SubcategoryRatingForm.module.css';

export interface SubcategoryRatingFormProps {
  tutorUserId: string;
}

// DOCUMENTED (help.preply.com/articles/11196386) descriptions for each dimension, already
// captured in 12-classroom-messaging-reviews-notifications.md — not invented copy.
const DIMENSIONS = [
  { key: 'clarity', label: 'Clarity', description: 'Explanations are simple, structured, and easy to follow.' },
  { key: 'progress', label: 'Progress', description: 'Support building skills and tracking growth.' },
  { key: 'preparation', label: 'Preparation', description: 'Organized, focused lessons with a plan.' },
  { key: 'reassurance', label: 'Reassurance', description: 'Encouragement, patience, and emotional support.' },
] as const;

type DimensionKey = (typeof DIMENSIONS)[number]['key'];

/** A genuinely separate submission from the written review (DOCUMENTED: a distinct, optional,
 * once-per-calendar-month flow) — rendered as its own form, never merged into ReviewForm. Every
 * dimension is independently optional ("rate all, some, or none"), so there is no validation
 * error state here at all — any combination, including all four left unrated, is a valid submit. */
export function SubcategoryRatingForm({ tutorUserId }: SubcategoryRatingFormProps) {
  const [values, setValues] = useState<Record<DimensionKey, number | null>>({
    clarity: null,
    progress: null,
    preparation: null,
    reassurance: null,
  });
  const submit = useSubmitSubcategoryRating(tutorUserId);
  const formId = useId();

  const serverErrorMessage = (() => {
    if (!submit.isError) return null;
    const error = submit.error;
    if (error instanceof ApiError) {
      return error.problem.detail ?? error.problem.title ?? 'Could not submit your ratings. Please try again.';
    }
    return 'Could not submit your ratings. Please try again.';
  })();

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    submit.mutate(values);
  };

  if (submit.isSuccess) {
    return (
      <Alert variant="success" title="Thanks for rating these aspects">
        You can submit again next month.
      </Alert>
    );
  }

  return (
    <form className={styles.form} onSubmit={handleSubmit} aria-labelledby={`${formId}-heading`}>
      <p id={`${formId}-heading`} className={styles.heading}>
        Rate specific aspects <span className={styles.optional}>(optional — rate any, all, or none)</span>
      </p>

      <div className={styles.dimensions}>
        {DIMENSIONS.map((dimension) => (
          <div key={dimension.key} className={styles.dimension}>
            <div className={styles.dimensionLabel}>
              <span className={styles.dimensionName}>{dimension.label}</span>
              <span className={styles.dimensionDescription}>{dimension.description}</span>
            </div>
            <StarRatingInput
              name={`${formId}-${dimension.key}`}
              value={values[dimension.key]}
              onChange={(value) => setValues((prev) => ({ ...prev, [dimension.key]: value }))}
            />
          </div>
        ))}
      </div>

      {serverErrorMessage && (
        <Alert variant="error" title="Couldn't submit your ratings">
          {serverErrorMessage}
        </Alert>
      )}

      <Button type="submit" variant="secondary" loading={submit.isPending}>
        Submit ratings
      </Button>
    </form>
  );
}
