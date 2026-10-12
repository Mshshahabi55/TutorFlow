'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  approveTutorApplication,
  beginReviewTutorApplication,
  getPendingTutorApplications,
  getTutorApplicationForAdmin,
  publishTutorApplication,
  rejectTutorApplication,
  suspendTutorApplication,
} from './api';
import { TutorApplicationStatus } from './types';

const adminTutorApplicationsPrefix = ['admin', 'tutor-applications'] as const;

export function pendingApplicationsQueryKey(status: TutorApplicationStatus | undefined, page: number) {
  return [...adminTutorApplicationsPrefix, 'list', status ?? 'pending', page] as const;
}

export function usePendingTutorApplications(status: TutorApplicationStatus | undefined, page: number, enabled: boolean) {
  return useQuery({
    queryKey: pendingApplicationsQueryKey(status, page),
    queryFn: ({ signal }) => getPendingTutorApplications({ status, page, pageSize: 20 }, signal),
    enabled,
    placeholderData: (previous) => previous,
  });
}

export function adminApplicationDetailQueryKey(id: string) {
  return [...adminTutorApplicationsPrefix, 'detail', id] as const;
}

export function useTutorApplicationForAdmin(id: string, enabled: boolean) {
  return useQuery({
    queryKey: adminApplicationDetailQueryKey(id),
    queryFn: ({ signal }) => getTutorApplicationForAdmin(id, signal),
    enabled,
  });
}

/** Every admin mutation invalidates both its own detail entry and the whole list family (prefix
 * match) — a page of at most 20 rows makes a full list refetch cheap, and correctness (a
 * status change must disappear from the pending queue) matters more than a surgical cache edit
 * here. */
function useInvalidateAfterMutation(id: string) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: adminApplicationDetailQueryKey(id) });
    queryClient.invalidateQueries({ queryKey: adminTutorApplicationsPrefix });
  };
}

export function useBeginReviewTutorApplication(id: string) {
  const invalidate = useInvalidateAfterMutation(id);
  return useMutation({ mutationFn: () => beginReviewTutorApplication(id), onSuccess: invalidate });
}

export function useApproveTutorApplication(id: string) {
  const invalidate = useInvalidateAfterMutation(id);
  return useMutation({ mutationFn: () => approveTutorApplication(id), onSuccess: invalidate });
}

export function useRejectTutorApplication(id: string) {
  const invalidate = useInvalidateAfterMutation(id);
  return useMutation({ mutationFn: (reason: string) => rejectTutorApplication(id, reason), onSuccess: invalidate });
}

export function usePublishTutorApplication(id: string) {
  const invalidate = useInvalidateAfterMutation(id);
  return useMutation({ mutationFn: () => publishTutorApplication(id), onSuccess: invalidate });
}

export function useSuspendTutorApplication(id: string) {
  const invalidate = useInvalidateAfterMutation(id);
  return useMutation({ mutationFn: (reason: string) => suspendTutorApplication(id, reason), onSuccess: invalidate });
}
