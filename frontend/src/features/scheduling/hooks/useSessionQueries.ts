import { useQuery } from "@tanstack/react-query";
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
