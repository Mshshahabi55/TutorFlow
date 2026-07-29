import { useQueries, useQuery } from "@tanstack/react-query";
import {
  fetchPendingTutors,
  fetchTutorById,
  fetchTutorDirectory,
} from "@/features/identity/api/identityService";

export function useTutor(tutorId: string | undefined) {
  return useQuery({
    queryKey: ["identity", "tutors", "detail", tutorId],
    queryFn: () => fetchTutorById(tutorId as string),
    enabled: Boolean(tutorId),
  });
}

/**
 * Favorites/Recently Viewed/Compare each hold a bounded, client-side list
 * of Tutor ids (never more than a handful in realistic use) and need each
 * one's current data — same parallel-fetch-sharing-the-single-Tutor-cache
 * pattern `useStudentSchedules` already established. A stale/deleted id
 * (e.g. a Tutor removed after being favorited) surfaces as that one
 * query's own isError, not a failure of the whole list.
 */
export function useTutorsByIds(tutorIds: string[]) {
  return useQueries({
    queries: tutorIds.map((tutorId) => ({
      queryKey: ["identity", "tutors", "detail", tutorId],
      queryFn: () => fetchTutorById(tutorId),
    })),
  });
}

/** GET /tutors — every discoverable Tutor, unpaginated (matches the endpoint's own shape). */
export function useTutorDirectory() {
  return useQuery({
    queryKey: ["identity", "tutors", "directory"],
    queryFn: fetchTutorDirectory,
  });
}

export function usePendingTutors(page: number, pageSize: number) {
  return useQuery({
    queryKey: ["identity", "tutors", "pending", page, pageSize],
    queryFn: () => fetchPendingTutors(page, pageSize),
  });
}
