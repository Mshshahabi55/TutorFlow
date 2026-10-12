import { apiGet, apiPost, apiPut } from '@/lib/api/client';
import {
  ExistingReview,
  ReviewEligibility,
  SubcategoryRatingEligibility,
  SubmitReviewCommand,
  SubmitSubcategoryRatingCommand,
  TutorReviewSummary,
} from './types';

export function getTutorReviewSummary(tutorUserId: string, signal?: AbortSignal): Promise<TutorReviewSummary> {
  return apiGet<TutorReviewSummary>(`/api/reviews/tutors/${tutorUserId}/summary`, undefined, { signal });
}

export function getReviewEligibility(tutorUserId: string, signal?: AbortSignal): Promise<ReviewEligibility> {
  return apiGet<ReviewEligibility>(`/api/reviews/tutors/${tutorUserId}/me/eligibility`, undefined, { signal });
}

export function submitReview(tutorUserId: string, command: SubmitReviewCommand): Promise<ExistingReview> {
  return apiPut<ExistingReview, SubmitReviewCommand>(`/api/reviews/tutors/${tutorUserId}/me`, command);
}

export function getSubcategoryRatingEligibility(tutorUserId: string, signal?: AbortSignal): Promise<SubcategoryRatingEligibility> {
  return apiGet<SubcategoryRatingEligibility>(`/api/reviews/tutors/${tutorUserId}/me/subcategory-eligibility`, undefined, { signal });
}

export function submitSubcategoryRating(tutorUserId: string, command: SubmitSubcategoryRatingCommand) {
  return apiPost<unknown, SubmitSubcategoryRatingCommand>(`/api/reviews/tutors/${tutorUserId}/me/subcategory-ratings`, command);
}
