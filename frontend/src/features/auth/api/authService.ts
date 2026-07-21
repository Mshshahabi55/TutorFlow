import { apiClient, unwrapValue } from "@/services/api/apiClient";
import type { ApiResult, VoidApiResult } from "@/services/api/apiTypes";
import type { LoginResultDto } from "@/services/api/dtos";

// Direct 1:1 mapping to TutorFlow.Web.Endpoints.AuthEndpoints
// (docs/adr/ADR-017-authentication-mechanism-decision.md) — no field added
// beyond what the backend already exposes.

export async function login(email: string, password: string): Promise<LoginResultDto> {
  const response = await apiClient.post<ApiResult<LoginResultDto>>("/auth/login", { email, password });
  return unwrapValue(response.data);
}

export async function logout(token: string): Promise<void> {
  await apiClient.post<VoidApiResult>("/auth/logout", { token });
}

// Admin-assisted reset only — no self-service recovery flow exists yet.
// Revokes every active session for the account immediately.
export async function resetPassword(accountId: string, newPassword: string): Promise<void> {
  await apiClient.post<VoidApiResult>(`/auth/accounts/${accountId}/reset-password`, { newPassword });
}
