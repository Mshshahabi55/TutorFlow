import type { StatusTone } from "@/shared/components/feedback/StatusPill";
import { SessionStatus } from "@/services/api/dtos";

// One source of truth for how a Session's status is labeled/toned, reused
// by SessionDetailPage, StudentSessionListPage, TutorSessionListPage
// (Scheduling & Booking) and GlobalSessionListPage (Marketplace Oversight,
// which owns no Session data of its own and reads this Scheduling & Booking
// convention rather than redefining it — ARCHITECTURE.md §4).
export const SESSION_STATUS_LABEL: Record<SessionStatus, string> = {
  [SessionStatus.Scheduled]: "Scheduled",
  [SessionStatus.Completed]: "Completed",
  [SessionStatus.Cancelled]: "Cancelled",
  [SessionStatus.NoShow]: "No-Show",
};

export const SESSION_STATUS_TONE: Record<SessionStatus, StatusTone> = {
  [SessionStatus.Scheduled]: "info",
  [SessionStatus.Completed]: "success",
  [SessionStatus.Cancelled]: "critical",
  [SessionStatus.NoShow]: "warning",
};
