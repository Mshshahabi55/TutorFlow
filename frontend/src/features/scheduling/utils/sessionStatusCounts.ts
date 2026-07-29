import { SessionStatus } from "@/services/api/dtos";
import type { SessionDto, SessionStatusCountsDto } from "@/services/api/dtos";

export interface StatusCounts {
  scheduled: number;
  completed: number;
  cancelled: number;
  noShow: number;
}

/** Tutor/Student dashboards hold a full session array already (useTutorSchedule/useStudentSchedule); Admin's own KPI comes pre-aggregated from GET /sessions/status-counts instead (see that endpoint's own comment on why it's a DB-side GROUP BY, not every row fetched client-side). */
export function deriveStatusCounts(sessions: SessionDto[]): StatusCounts {
  const counts: StatusCounts = { scheduled: 0, completed: 0, cancelled: 0, noShow: 0 };
  for (const session of sessions) {
    if (session.status === SessionStatus.Scheduled) counts.scheduled += 1;
    else if (session.status === SessionStatus.Completed) counts.completed += 1;
    else if (session.status === SessionStatus.Cancelled) counts.cancelled += 1;
    else if (session.status === SessionStatus.NoShow) counts.noShow += 1;
  }
  return counts;
}

export function countsFromDto(dto: SessionStatusCountsDto): StatusCounts {
  return { scheduled: dto.scheduled, completed: dto.completed, cancelled: dto.cancelled, noShow: dto.noShow };
}
