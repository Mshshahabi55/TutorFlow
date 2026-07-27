import { useQueries, useQuery } from "@tanstack/react-query";
import {
  fetchSessionById,
  fetchStudentSchedule,
  fetchTutorSchedule,
} from "@/features/scheduling/api/schedulingService";

export function useSession(sessionId: string | undefined) {
  return useQuery({
    queryKey: ["scheduling", "sessions", "detail", sessionId],
    queryFn: () => fetchSessionById(sessionId as string),
    enabled: Boolean(sessionId),
  });
}

export function useStudentSchedule(studentId: string | undefined) {
  return useQuery({
    queryKey: ["scheduling", "sessions", "byStudent", studentId],
    queryFn: () => fetchStudentSchedule(studentId as string),
    enabled: Boolean(studentId),
  });
}

export function useTutorSchedule(tutorId: string | undefined) {
  return useQuery({
    queryKey: ["scheduling", "sessions", "byTutor", tutorId],
    queryFn: () => fetchTutorSchedule(tutorId as string),
    enabled: Boolean(tutorId),
  });
}

/**
 * The Parent Dashboard/My Children's family-wide view (Today's Lessons,
 * Next Lesson, Upcoming, Recent Activity) needs every confirmed child's
 * schedule, not just one — `studentIds` is bounded by how many children a
 * family actually has (realistically a handful), not an unbounded list, so
 * this is a deliberate parallel fetch rather than the N+1 pattern a
 * paginated list would create. Each query uses the exact same key
 * `useStudentSchedule` does, so it shares that hook's cache rather than
 * duplicating a request already made elsewhere (e.g. a child's own "My
 * Lessons" visit).
 */
export function useStudentSchedules(studentIds: string[]) {
  return useQueries({
    queries: studentIds.map((studentId) => ({
      queryKey: ["scheduling", "sessions", "byStudent", studentId],
      queryFn: () => fetchStudentSchedule(studentId),
    })),
  });
}
