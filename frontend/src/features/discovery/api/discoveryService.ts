import { apiClient, unwrapValue } from "@/services/api/apiClient";
import type { ApiResult, PagedResult } from "@/services/api/apiTypes";
import type { TutorDto } from "@/services/api/dtos";

// Direct, 1:1 mapping to GET /tutors/search (TutorFlow.Web.Endpoints.DiscoveryEndpoints)
// — verified against that source, not inferred.

export interface SearchTutorsFilters {
  subject: string;
  language: string;
  location: string;
  /** UTC ISO 8601 string, or empty for no filter. */
  availableFrom: string;
}

export async function searchTutors(
  filters: SearchTutorsFilters,
  page: number,
  pageSize: number,
): Promise<PagedResult<TutorDto>> {
  const response = await apiClient.get<ApiResult<PagedResult<TutorDto>>>("/tutors/search", {
    params: {
      subject: filters.subject || undefined,
      language: filters.language || undefined,
      location: filters.location || undefined,
      availableFrom: filters.availableFrom || undefined,
      page,
      pageSize,
    },
  });
  return unwrapValue(response.data);
}
