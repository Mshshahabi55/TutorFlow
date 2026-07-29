import { useQuery } from "@tanstack/react-query";
import {
  fetchConversationMessages,
  fetchMyConversations,
} from "@/features/communication/api/communicationService";
import { useAuth } from "@/shared/hooks/useAuth";

// ADR-022: no WebSockets/SignalR — "live" delivery is client-side polling
// over the same REST endpoints, refetched on an interval while the relevant
// screen is open.
const CONVERSATION_LIST_POLL_MS = 15_000;
const MESSAGE_POLL_MS = 5_000;

/**
 * RC4.4: gated on real authentication, not just an effective (possibly
 * dev-preview) role — this query renders globally (every Dashboard), so
 * without this guard it fires, and 401s, for a visitor who has never
 * signed in (or is only using the dev-only "Acting as" preview, which
 * carries no real bearer token). See useLogin's own RC4.4 comment for the
 * other half of this fix (why a cached 401 from that case would otherwise
 * outlive a subsequent real sign-in).
 */
export function useMyConversations() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: ["communication", "conversations", "mine"],
    queryFn: () => fetchMyConversations(),
    enabled: isAuthenticated,
    refetchInterval: CONVERSATION_LIST_POLL_MS,
  });
}

export function useConversationMessages(conversationId: string | undefined) {
  return useQuery({
    queryKey: ["communication", "conversations", "detail", conversationId, "messages"],
    queryFn: () => fetchConversationMessages(conversationId as string),
    enabled: Boolean(conversationId),
    refetchInterval: MESSAGE_POLL_MS,
  });
}
