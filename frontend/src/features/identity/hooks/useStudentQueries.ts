import { useQuery } from "@tanstack/react-query";
import { fetchStudentById } from "@/features/identity/api/identityService";

export function useStudent(studentId: string | undefined) {
  return useQuery({
    queryKey: ["identity", "students", "detail", studentId],
    queryFn: () => fetchStudentById(studentId as string),
    enabled: Boolean(studentId),
  });
}
