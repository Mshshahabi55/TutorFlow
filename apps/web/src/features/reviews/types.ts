/** Mirrors `GetTutorReviewSummary.Response` (Reviews module). Public, anonymous — no reviewer
 * identity is included (see the backend handler's own comment for why). */
export interface TutorReviewSummaryEntry {
  rating: number;
  comment: string | null;
  createdAtUtc: string;
}

/** Mirrors `GetTutorReviewSummary.SubRatingDimension` — an aggregate average across however many
 * students submitted this specific dimension this month-by-month over time (DOCUMENTED:
 * help.preply.com/articles/11196386). `average` is `null` when `count` is 0 — never fabricated
 * as 0. Each of the four dimensions can have a different `count` since each is independently
 * optional per submission. */
export interface SubRatingDimension {
  average: number | null;
  count: number;
}

export interface TutorReviewSubRatings {
  clarity: SubRatingDimension;
  progress: SubRatingDimension;
  preparation: SubRatingDimension;
  reassurance: SubRatingDimension;
}

/** Mirrors `GetSubcategoryRatingEligibility.Response`. DOCUMENTED: a separate, looser gate than
 * the written review (>=1 completed lesson, not 3) with its own "once per calendar month"
 * restriction — never conflated with `ReviewEligibility` above. */
export interface SubcategoryRatingEligibility {
  isEligible: boolean;
  hasCompletedAtLeastOneLesson: boolean;
  alreadySubmittedThisMonth: boolean;
}

/** Mirrors `SubmitSubcategoryRating.Command` — every field independently optional (DOCUMENTED:
 * "rate all, some, or none"). */
export interface SubmitSubcategoryRatingCommand {
  clarity: number | null;
  progress: number | null;
  preparation: number | null;
  reassurance: number | null;
}

export interface TutorReviewSummary {
  overallRating: number;
  reviewCount: number;
  subRatings: TutorReviewSubRatings | null;
  reviews: TutorReviewSummaryEntry[];
}

/** Mirrors `GetReviewEligibility.Response`. Eligibility is a Student<->Tutor relationship gate
 * (3rd completed lesson with that specific tutor, documented Preply behavior) — never a
 * per-Lesson one. */
export interface ExistingReview {
  rating: number;
  comment: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface ReviewEligibility {
  isEligible: boolean;
  completedLessonCount: number;
  requiredCompletedLessonCount: number;
  existingReview: ExistingReview | null;
}

/** Mirrors `SubmitReview.Command`. */
export interface SubmitReviewCommand {
  rating: number;
  comment: string | null;
}
