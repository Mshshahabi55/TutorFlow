import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  bookSession,
  cancelSession,
  completeSession,
  markSessionNoShow,
  rescheduleSession,
  type BookSessionInput,
} from "@/features/scheduling/api/schedulingService";

/** Every Session query key is namespaced under ["scheduling","sessions",...], so this one invalidation covers detail, student schedule, and tutor schedule together. */
function useInvalidateSessions() {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: ["scheduling", "sessions"] });
  };
}

export function useBookSession() {
  const invalidate = useInvalidateSessions();
  return useMutation({
    mutationFn: (input: BookSessionInput) => bookSession(input),
    onSuccess: invalidate,
  });
}

export function useRescheduleSession(sessionId: string) {
  const invalidate = useInvalidateSessions();
  return useMutation({
    mutationFn: (newScheduledTimeUtc: string) => rescheduleSession(sessionId, newScheduledTimeUtc),
    onSuccess: invalidate,
  });
}

export function useCancelSession(sessionId: string) {
  const invalidate = useInvalidateSessions();
  return useMutation({
    mutationFn: () => cancelSession(sessionId),
    onSuccess: invalidate,
  });
}

export function useCompleteSession(sessionId: string) {
  const invalidate = useInvalidateSessions();
  return useMutation({
    mutationFn: () => completeSession(sessionId),
    onSuccess: invalidate,
  });
}

export function useMarkSessionNoShow(sessionId: string) {
  const invalidate = useInvalidateSessions();
  return useMutation({
    mutationFn: () => markSessionNoShow(sessionId),
    onSuccess: invalidate,
  });
}
