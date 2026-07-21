import { apiClient } from "@/services/api/apiClient";

/**
 * GET /health returns a plain-text body ("Healthy"/"Unhealthy") from
 * ASP.NET Core's built-in health check middleware — it is not wrapped in
 * the ApiResult envelope other endpoints use (verified against
 * TutorFlow.Web.Tests.ObservabilityTests).
 */
export async function fetchHealthStatus(): Promise<string> {
  const response = await apiClient.get<string>("/health", {
    responseType: "text",
  });
  return response.data;
}
