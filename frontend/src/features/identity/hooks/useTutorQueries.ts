import { useQuery } from "@tanstack/react-query";
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
