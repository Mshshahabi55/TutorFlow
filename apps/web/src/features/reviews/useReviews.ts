'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  getReviewEligibility,
  getSubcategoryRatingEligibility,
  getTutorReviewSummary,
  submitReview,
  submitSubcategoryRating,
} from './api';
import { SubmitReviewCommand, SubmitSubcategoryRatingCommand } from './types';

export function useTutorReviewSummary(tutorUserId: string) {
  return useQuery({
    queryKey: ['reviews', 'summary', tutorUserId] as const,
    queryFn: ({ signal }) => getTutorReviewSummary(tutorUserId, signal),
  });
}

export function useReviewEligibility(tutorUserId: string, enabled: boolean) {
  return useQuery({
    queryKey: ['reviews', 'eligibility', tutorUserId] as const,
    queryFn: ({ signal }) => getReviewEligibility(tutorUserId, signal),
    enabled,
  });
}

export function useSubmitReview(tutorUserId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (command: SubmitReviewCommand) => submitReview(tutorUserId, command),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reviews', 'summary', tutorUserId] });
      queryClient.invalidateQueries({ queryKey: ['reviews', 'eligibility', tutorUserId] });
    },
  });
}

/** A deliberately separate hook from `useReviewEligibility` — DOCUMENTED, the subcategory gate
 * (>=1 completed lesson, once per calendar month) is not the same rule as the written review's
 * 3-lesson threshold. */
export function useSubcategoryRatingEligibility(tutorUserId: string, enabled: boolean) {
  return useQuery({
    queryKey: ['reviews', 'subcategory-eligibility', tutorUserId] as const,
    queryFn: ({ signal }) => getSubcategoryRatingEligibility(tutorUserId, signal),
    enabled,
  });
}

export function useSubmitSubcategoryRating(tutorUserId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (command: SubmitSubcategoryRatingCommand) => submitSubcategoryRating(tutorUserId, command),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reviews', 'summary', tutorUserId] });
      queryClient.invalidateQueries({ queryKey: ['reviews', 'subcategory-eligibility', tutorUserId] });
    },
  });
}
