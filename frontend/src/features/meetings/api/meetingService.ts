import { apiClient, unwrapValue } from "@/services/api/apiClient";
import type { ApiResult } from "@/services/api/apiTypes";
import type { MeetingDto } from "@/services/api/dtos";

// Every function here is a direct, 1:1 mapping to one endpoint implemented
// by TutorFlow.Web.Endpoints.MeetingEndpoints
// (docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md). No
// field, endpoint, or DTO is added beyond what the backend already exposes.

/** POST /sessions/{id}/meeting — starts ("Start Lesson"), or returns the already-started Meeting for this Session (idempotent). Tutor-only. */
export async function startMeeting(sessionId: string): Promise<MeetingDto> {
  const response = await apiClient.post<ApiResult<MeetingDto>>(`/sessions/${sessionId}/meeting`);
  return unwrapValue(response.data);
}

/** GET /sessions/{id}/meeting — the Meeting for a Session, if one has been started. */
export async function fetchMeetingBySession(sessionId: string): Promise<MeetingDto> {
  const response = await apiClient.get<ApiResult<MeetingDto>>(`/sessions/${sessionId}/meeting`);
  return unwrapValue(response.data);
}

/** GET /conversations/{id}/active-meeting — the Meeting for the nearest relevant online Session between a Conversation's two participants, or null if there isn't one (a normal, non-error outcome). */
export async function fetchActiveMeetingForConversation(conversationId: string): Promise<MeetingDto | null> {
  const response = await apiClient.get<ApiResult<MeetingDto | null>>(
    `/conversations/${conversationId}/active-meeting`,
  );
  return unwrapValue(response.data);
}
