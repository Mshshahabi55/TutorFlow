import { useQuery } from "@tanstack/react-query";
import {
  fetchActiveMeetingForConversation,
  fetchMeetingBySession,
} from "@/features/meetings/api/meetingService";
import { isNotFoundError } from "@/services/api/errorClassification";
import { useAuth } from "@/shared/hooks/useAuth";

// docs/adr/ADR-023-...: no new infrastructure — the countdown/status shown
// on MeetingCard polls this same REST endpoint on an interval, the same
// "no WebSockets" posture ADR-022 already established for Communication.
const MEETING_POLL_MS = 15_000;

/**
 * A 404 here means "no meeting has been started for this session yet" —
 * an expected, common, non-error outcome (most Sessions never have one),
 * never retried as if it were a transient failure.
 */
export function useMeetingBySession(sessionId: string | undefined) {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: ["meetings", "bySession", sessionId],
    queryFn: () => fetchMeetingBySession(sessionId as string),
    enabled: isAuthenticated && Boolean(sessionId),
    refetchInterval: MEETING_POLL_MS,
    retry: (failureCount, error) => !isNotFoundError(error) && failureCount < 1,
  });
}

export function useActiveMeetingForConversation(conversationId: string | undefined) {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: ["meetings", "byConversation", conversationId],
    queryFn: () => fetchActiveMeetingForConversation(conversationId as string),
    enabled: isAuthenticated && Boolean(conversationId),
    refetchInterval: MEETING_POLL_MS,
  });
}
