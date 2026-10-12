'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { searchTutors } from './api';
import { addFavorite, getFavoriteStatuses, removeFavorite } from '../tutor-profile/api';
import { SearchFilters } from './types';

export function tutorSearchQueryKey(filters: SearchFilters) {
  return ['search', 'tutors', filters] as const;
}

export function useTutorSearch(filters: SearchFilters) {
  return useQuery({
    queryKey: tutorSearchQueryKey(filters),
    queryFn: ({ signal }) => searchTutors(filters, { signal }),
    placeholderData: (previous) => previous,
  });
}

const favoriteStatusesQueryKeyPrefix = ['search', 'favorite-statuses'] as const;

/** Bulk favorite status for a page of results (ADR-0024) — one request, not one per card.
 * Only meaningful for an authenticated Student; callers pass `enabled` based on auth state. */
export function useFavoriteStatuses(tutorUserIds: string[], enabled: boolean) {
  const sortedIds = [...tutorUserIds].sort();
  return useQuery({
    queryKey: [...favoriteStatusesQueryKeyPrefix, sortedIds] as const,
    queryFn: ({ signal }) => getFavoriteStatuses(sortedIds, { signal }),
    enabled: enabled && tutorUserIds.length > 0,
  });
}

/** Add/remove reuse the exact same Students endpoints Tutor Profile's BookingPanel already
 * calls — not a second favorite system. Invalidating the whole bulk-status query family (prefix
 * match) after a toggle is simpler and just as correct as surgical cache edits here, since a
 * results page is at most ~20 cards. */
export function useFavoriteToggle() {
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: favoriteStatusesQueryKeyPrefix });

  const add = useMutation({
    mutationFn: (tutorUserId: string) => addFavorite(tutorUserId),
    onSuccess: invalidate,
  });

  const remove = useMutation({
    mutationFn: (tutorUserId: string) => removeFavorite(tutorUserId),
    onSuccess: invalidate,
  });

  return { add, remove };
}
