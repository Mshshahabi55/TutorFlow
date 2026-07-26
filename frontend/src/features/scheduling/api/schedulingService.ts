import { apiClient, unwrapValue } from "@/services/api/apiClient";
import type { ApiResult, VoidApiResult } from "@/services/api/apiTypes";
import type { AvailabilitySlotDto, DeliveryMode, SessionDto } from "@/services/api/dtos";

// Every function here is a direct, 1:1 mapping to one endpoint already
// implemented by TutorFlow.Web.Endpoints.SchedulingEndpoints — verified
// against that source, not inferred. No field, endpoint, or DTO is added
// beyond what the backend already exposes. Notably absent, because the
// backend exposes no such capability: cancel/delete an Availability Slot
// (see the Sprint 7 Completion Report). fetchTutorAvailabilitySlots below
// (Phase 4.7) fills the "list/browse" gap that report flagged — the
// backend endpoint already existed (GET /tutors/{id}/availability-slots,
// serving the List/Calendar views), this file simply had no wrapper for it
// yet.

export interface DeclareAvailabilityInput {
  tutorId: string;
  startTimeUtc: string;
  /** .NET TimeSpan "c"-format string (e.g. "01:00:00") — see shared/utils/duration.ts. */
  duration: string;
  deliveryMode: DeliveryMode;
}

export async function declareAvailability(
  input: DeclareAvailabilityInput,
): Promise<AvailabilitySlotDto> {
  const response = await apiClient.post<ApiResult<AvailabilitySlotDto>>(
    "/availability-slots",
    input,
  );
  return unwrapValue(response.data);
}

export async function fetchAvailabilitySlotById(
  availabilitySlotId: string,
): Promise<AvailabilitySlotDto> {
  const response = await apiClient.get<ApiResult<AvailabilitySlotDto>>(
    `/availability-slots/${availabilitySlotId}`,
  );
  return unwrapValue(response.data);
}

export interface BookSessionInput {
  availabilitySlotId: string;
  studentId: string;
  parentGuardianId: string | null;
}

export async function bookSession(input: BookSessionInput): Promise<SessionDto> {
  const response = await apiClient.post<ApiResult<SessionDto>>("/sessions", input);
  return unwrapValue(response.data);
}

export async function rescheduleSession(
  sessionId: string,
  newAvailabilitySlotId: string,
): Promise<void> {
  await apiClient.post<VoidApiResult>(`/sessions/${sessionId}/reschedule`, {
    newAvailabilitySlotId,
  });
}

export async function cancelSession(sessionId: string): Promise<void> {
  await apiClient.post<VoidApiResult>(`/sessions/${sessionId}/cancel`);
}

export async function completeSession(sessionId: string): Promise<void> {
  await apiClient.post<VoidApiResult>(`/sessions/${sessionId}/complete`);
}

export async function markSessionNoShow(sessionId: string): Promise<void> {
  await apiClient.post<VoidApiResult>(`/sessions/${sessionId}/no-show`);
}

export async function fetchSessionById(sessionId: string): Promise<SessionDto> {
  const response = await apiClient.get<ApiResult<SessionDto>>(`/sessions/${sessionId}`);
  return unwrapValue(response.data);
}

/** GET /students/{id}/schedule — every Session for the Student, unpaginated (matches the endpoint's own shape). */
export async function fetchStudentSchedule(studentId: string): Promise<SessionDto[]> {
  const response = await apiClient.get<ApiResult<SessionDto[]>>(`/students/${studentId}/schedule`);
  return unwrapValue(response.data);
}

/** GET /tutors/{id}/schedule — every Session for the Tutor, unpaginated (matches the endpoint's own shape). */
export async function fetchTutorSchedule(tutorId: string): Promise<SessionDto[]> {
  const response = await apiClient.get<ApiResult<SessionDto[]>>(`/tutors/${tutorId}/schedule`);
  return unwrapValue(response.data);
}

/** GET /tutors/{id}/availability-slots — every Availability Slot for the Tutor, unpaginated (matches the endpoint's own shape; serves both List and Calendar views on the backend). */
export async function fetchTutorAvailabilitySlots(tutorId: string): Promise<AvailabilitySlotDto[]> {
  const response = await apiClient.get<ApiResult<AvailabilitySlotDto[]>>(
    `/tutors/${tutorId}/availability-slots`,
  );
  return unwrapValue(response.data);
}
