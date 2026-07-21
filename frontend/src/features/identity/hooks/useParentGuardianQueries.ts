import { useQuery } from "@tanstack/react-query";
import { fetchParentGuardianById } from "@/features/identity/api/identityService";

export function useParentGuardian(parentGuardianId: string | undefined) {
  return useQuery({
    queryKey: ["identity", "parentGuardians", "detail", parentGuardianId],
    queryFn: () => fetchParentGuardianById(parentGuardianId as string),
    enabled: Boolean(parentGuardianId),
  });
}
