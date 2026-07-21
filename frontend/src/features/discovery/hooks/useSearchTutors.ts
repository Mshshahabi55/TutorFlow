import { useQuery } from "@tanstack/react-query";
import { searchTutors, type SearchTutorsFilters } from "@/features/discovery/api/discoveryService";

export function useSearchTutors(filters: SearchTutorsFilters, page: number, pageSize: number) {
  return useQuery({
    queryKey: ["discovery", "tutors", "search", filters, page, pageSize],
    queryFn: () => searchTutors(filters, page, pageSize),
  });
}
