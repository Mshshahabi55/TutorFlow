import { byScheduledTimeAscending, byScheduledTimeDescending } from "@/features/scheduling/utils/sessionSort";
import { SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

export interface StudentRosterEntry {
  studentId: string;
  totalSessions: number;
  upcomingSessions: number;
  /** The soonest still-Scheduled Session with this Student, if any. */
  nextSession?: SessionDto;
  /** The most recent Completed/Cancelled/No-Show Session with this Student, if any. */
  mostRecentPastSession?: SessionDto;
  /** Every Session with this Student, most recent first — for an on-demand history view. */
  sessions: SessionDto[];
}

/**
 * Groups a Tutor's own schedule (`useTutorSchedule`, already fetched by
 * every caller) into one entry per unique Student — shared by
 * `TutorStudentsPage` (the full roster) and the Tutor Dashboard's
 * "Students Requiring Attention" highlight, so this grouping is written
 * once, not twice. Client-side derivation only: no new endpoint, no
 * per-Student fetch.
 */
export function deriveStudentRoster(sessions: SessionDto[]): StudentRosterEntry[] {
  const byStudent = new Map<string, SessionDto[]>();

  for (const session of sessions) {
    const existing = byStudent.get(session.studentId) ?? [];
    existing.push(session);
    byStudent.set(session.studentId, existing);
  }

  return Array.from(byStudent.entries()).map(([studentId, studentSessions]) => {
    const upcoming = studentSessions
      .filter((session) => session.status === SessionStatus.Scheduled)
      .sort(byScheduledTimeAscending);
    const past = studentSessions
      .filter((session) => session.status !== SessionStatus.Scheduled)
      .sort(byScheduledTimeDescending);

    return {
      studentId,
      totalSessions: studentSessions.length,
      upcomingSessions: upcoming.length,
      nextSession: upcoming[0],
      mostRecentPastSession: past[0],
      sessions: [...studentSessions].sort(byScheduledTimeDescending),
    };
  });
}
