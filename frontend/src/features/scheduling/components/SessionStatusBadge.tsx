import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { SESSION_STATUS_LABEL, SESSION_STATUS_TONE } from "@/features/scheduling/utils/sessionStatus";
import type { SessionStatus } from "@/services/api/dtos";

/**
 * One consistent visual treatment for a Session's status, reused by
 * `SessionCard`, `NextSessionCard`, and `SessionDetailPage` — reads the
 * same `SESSION_STATUS_LABEL`/`SESSION_STATUS_TONE` maps every Session
 * view already shared, for the same four statuses `SessionStatus` actually
 * has (Scheduled/Completed/Cancelled/No-Show). There is no Pending or
 * Rejected Session status in this API — those apply to other entities
 * (e.g. a Tutor's approval state), not a Session, so this component does
 * not invent them.
 */
export function SessionStatusBadge({ status }: { status: SessionStatus }) {
  return <StatusPill label={SESSION_STATUS_LABEL[status]} tone={SESSION_STATUS_TONE[status]} />;
}
