'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  createTutorApplication,
  getCurrentTutorApplication,
  reopenTutorApplicationForRevision,
  submitTutorApplication,
  updateTutorApplicationDraft,
} from './api';
import { UpdateDraftCommand } from './types';

export const tutorApplicationQueryKey = ['tutor-onboarding', 'application'] as const;

/** `null` data means "authenticated, Tutor-capable, but hasn't started an application yet" —
 * distinct from `isLoading`, which callers must handle themselves (ADR-0026: "Not Started" is
 * the absence of a row, never a separate backend status). */
export function useCurrentTutorApplication(enabled: boolean) {
  return useQuery({
    queryKey: tutorApplicationQueryKey,
    queryFn: ({ signal }) => getCurrentTutorApplication(signal),
    enabled,
  });
}

export function useCreateTutorApplication() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createTutorApplication,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tutorApplicationQueryKey }),
  });
}

/** Every wizard section save goes through this one mutation — there is no separate
 * local/offline draft store (ADR-0026 §4); a save that fails must surface as a real error, not
 * be silently buffered. */
export function useUpdateTutorApplicationDraft() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (command: UpdateDraftCommand) => updateTutorApplicationDraft(command),
    onSuccess: (data) => queryClient.setQueryData(tutorApplicationQueryKey, data),
  });
}

export function useSubmitTutorApplication() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: submitTutorApplication,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tutorApplicationQueryKey }),
  });
}

export function useReopenTutorApplicationForRevision() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: reopenTutorApplicationForRevision,
    onSuccess: (data) => queryClient.setQueryData(tutorApplicationQueryKey, data),
  });
}
