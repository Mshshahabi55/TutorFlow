import { useQuery } from "@tanstack/react-query";
import { fetchAllSessions } from "@/features/oversight/api/oversightService";

export function useAllSessions(page: number, pageSize: number) {
  return useQuery({
    queryKey: ["oversight", "sessions", "all", page, pageSize],
    queryFn: () => fetchAllSessions(page, pageSize),
  });
}
