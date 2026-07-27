import type { UseQueryResult } from "@tanstack/react-query";
import type { SessionDto } from "@/services/api/dtos";

export interface FamilySchedule {
  isPending: boolean;
  isError: boolean;
  /** The first child schedule query's own real error, if any — for ErrorState, never a fabricated message. */
  error: unknown;
  /** Every Session across every queried child, flattened — each SessionDto already carries its own studentId. */
  sessions: SessionDto[];
}

/**
 * Flattens `useStudentSchedules`' per-child query results into one family-
 * wide list — the Parent Dashboard's Today's Lessons/Next Lesson/Upcoming/
 * Recent Activity all read from this rather than re-deriving the same
 * pending/error/flatten logic four times.
 */
export function aggregateFamilySchedule(results: UseQueryResult<SessionDto[]>[]): FamilySchedule {
  return {
    isPending: results.some((result) => result.isPending),
    isError: results.some((result) => result.isError),
    error: results.find((result) => result.isError)?.error,
    sessions: results.flatMap((result) => result.data ?? []),
  };
}
