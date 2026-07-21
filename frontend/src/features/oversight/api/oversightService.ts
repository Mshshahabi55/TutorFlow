import { apiClient, unwrapValue } from "@/services/api/apiClient";
import type { ApiResult, PagedResult } from "@/services/api/apiTypes";
import type { SessionDto } from "@/services/api/dtos";

// Direct, 1:1 mapping to GET /sessions (TutorFlow.Web.Endpoints.OversightEndpoints)
// — verified against that source, not inferred. This is the only capability
// the Marketplace Oversight module exposes (ADM-3: view all schedules);
// ADM-1/ADM-2 (Tutor approve/suspend) are Identity & Relationship's own
// capabilities, already implemented in Sprint 6.

export async function fetchAllSessions(
  page: number,
  pageSize: number,
): Promise<PagedResult<SessionDto>> {
  const response = await apiClient.get<ApiResult<PagedResult<SessionDto>>>("/sessions", {
    params: { page, pageSize },
  });
  return unwrapValue(response.data);
}
