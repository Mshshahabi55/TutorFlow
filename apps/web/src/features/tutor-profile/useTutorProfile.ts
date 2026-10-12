'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  addFavorite,
  getAvailabilityPreview,
  getFavoriteStatus,
  GetTutorSlotsParams,
  getSimilarTutors,
  getTrialEligibility,
  getTutorProfile,
  getTutorSlots,
  removeFavorite,
} from './api';
import { TutorPublicProfile } from './types';

export function tutorProfileQueryKey(slug: string) {
  return ['tutor-profile', slug] as const;
}

export function useTutorProfile(slug: string, initialData?: TutorPublicProfile) {
  return useQuery({
    queryKey: tutorProfileQueryKey(slug),
    queryFn: ({ signal }) => getTutorProfile(slug, { signal }),
    initialData,
  });
}

export function useAvailabilityPreview(tutorUserId: string) {
  return useQuery({
    queryKey: ['tutor-profile', tutorUserId, 'availability-preview'] as const,
    queryFn: ({ signal }) => getAvailabilityPreview(tutorUserId, { signal }),
  });
}

/** Only meaningful once a real session exists (see useAuth) — callers must pass `enabled`
 * themselves based on auth state; this hook does not guess. Duration/range are part of the
 * query key (ADR-0025) — changing either triggers a real backend refetch, never a client-side
 * filter of a previously-fetched list. */
export function useTutorSlots(tutorUserId: string, params: GetTutorSlotsParams, enabled: boolean) {
  return useQuery({
    queryKey: ['tutor-profile', tutorUserId, 'slots', params] as const,
    queryFn: ({ signal }) => getTutorSlots(tutorUserId, params, { signal }),
    enabled,
  });
}

export function useTrialEligibility(tutorUserId: string, enabled: boolean) {
  return useQuery({
    queryKey: ['tutor-profile', tutorUserId, 'trial-eligibility'] as const,
    queryFn: ({ signal }) => getTrialEligibility(tutorUserId, { signal }),
    enabled,
  });
}

export function useFavoriteStatus(tutorUserId: string, enabled: boolean) {
  return useQuery({
    queryKey: ['tutor-profile', tutorUserId, 'favorite-status'] as const,
    queryFn: ({ signal }) => getFavoriteStatus(tutorUserId, { signal }),
    enabled,
  });
}

export function useFavoriteMutations(tutorUserId: string) {
  const queryClient = useQueryClient();
  const queryKey = ['tutor-profile', tutorUserId, 'favorite-status'] as const;

  const add = useMutation({
    mutationFn: () => addFavorite(tutorUserId),
    onSuccess: (data) => queryClient.setQueryData(queryKey, data),
  });

  const remove = useMutation({
    mutationFn: () => removeFavorite(tutorUserId),
    onSuccess: () => queryClient.setQueryData(queryKey, { isFavorited: false }),
  });

  return { add, remove };
}

export function useSimilarTutors(subjectId: string | undefined, excludeTutorUserId: string) {
  return useQuery({
    queryKey: ['tutor-profile', excludeTutorUserId, 'similar', subjectId ?? 'none'] as const,
    queryFn: ({ signal }) => getSimilarTutors(subjectId, excludeTutorUserId, { signal }),
  });
}
