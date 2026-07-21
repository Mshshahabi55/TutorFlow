import { useQuery } from "@tanstack/react-query";
import { fetchRelationshipsForAccount } from "@/features/identity/api/identityService";

export function useRelationshipsForAccount(accountId: string | undefined) {
  return useQuery({
    queryKey: ["identity", "relationships", "byAccount", accountId],
    queryFn: () => fetchRelationshipsForAccount(accountId as string),
    enabled: Boolean(accountId),
  });
}
