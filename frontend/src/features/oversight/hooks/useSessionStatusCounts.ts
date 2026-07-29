import { useQuery } from "@tanstack/react-query";
import { fetchSessionStatusCounts } from "@/features/oversight/api/oversightService";

export function useSessionStatusCounts() {
  return useQuery({
    queryKey: ["oversight", "sessions", "statusCounts"],
    queryFn: fetchSessionStatusCounts,
  });
}
