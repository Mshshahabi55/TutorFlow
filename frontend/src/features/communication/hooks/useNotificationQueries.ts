import { useQuery } from "@tanstack/react-query";
import { fetchMyNotifications } from "@/features/communication/api/communicationService";
import { useAuth } from "@/shared/hooks/useAuth";

// ADR-022: no WebSockets/SignalR — the Notification Bell's "real unread
// counter" polls this same REST endpoint on an interval rather than
// receiving a push.
const NOTIFICATION_POLL_MS = 20_000;

/**
 * RC4.4: gated on real authentication — the Notification Bell renders in
 * AppHeader on every page, signed in or not, so without this guard it
 * fires (and 401s) for every anonymous visitor and every dev-only "Acting
 * as" preview session. See useLogin's own RC4.4 comment for the other half
 * of this fix.
 */
export function useMyNotifications() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: ["communication", "notifications", "mine"],
    queryFn: () => fetchMyNotifications(),
    enabled: isAuthenticated,
    refetchInterval: NOTIFICATION_POLL_MS,
  });
}
