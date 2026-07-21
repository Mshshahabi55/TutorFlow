import { apiClient, unwrapValue } from "@/services/api/apiClient";
import type { ApiResult, PagedResult, VoidApiResult } from "@/services/api/apiTypes";
import type {
  ParentGuardianDto,
  RelationshipDto,
  StudentDto,
  TutorDto,
} from "@/services/api/dtos";

// Every function here is a direct, 1:1 mapping to one endpoint already
// implemented by TutorFlow.Web.Endpoints.IdentityEndpoints — verified
// against that source, not inferred. No field, endpoint, or DTO is added
// beyond what the backend already exposes.

export async function registerTutor(email: string, password: string): Promise<TutorDto> {
  const response = await apiClient.post<ApiResult<TutorDto>>("/tutors", { email, password });
  return unwrapValue(response.data);
}

export async function registerStudent(
  email: string,
  password: string,
  isMinor: boolean,
): Promise<StudentDto> {
  const response = await apiClient.post<ApiResult<StudentDto>>("/students", { email, password, isMinor });
  return unwrapValue(response.data);
}

export async function registerParentGuardian(
  email: string,
  password: string,
): Promise<ParentGuardianDto> {
  const response = await apiClient.post<ApiResult<ParentGuardianDto>>("/parent-guardians", {
    email,
    password,
  });
  return unwrapValue(response.data);
}

export async function approveTutor(tutorId: string): Promise<void> {
  await apiClient.post<VoidApiResult>(`/tutors/${tutorId}/approve`);
}

export async function suspendTutor(tutorId: string): Promise<void> {
  await apiClient.post<VoidApiResult>(`/tutors/${tutorId}/suspend`);
}

export async function setTutorHourlyRate(tutorId: string, amount: number): Promise<void> {
  await apiClient.patch<VoidApiResult>(`/tutors/${tutorId}/hourly-rate`, { amount });
}

export async function setTutorSubject(tutorId: string, subject: string): Promise<void> {
  await apiClient.patch<VoidApiResult>(`/tutors/${tutorId}/subject`, { subject });
}

export async function setTutorLanguage(tutorId: string, language: string): Promise<void> {
  await apiClient.patch<VoidApiResult>(`/tutors/${tutorId}/language`, { language });
}

export async function setTutorLocation(tutorId: string, location: string): Promise<void> {
  await apiClient.patch<VoidApiResult>(`/tutors/${tutorId}/location`, { location });
}

/** durations must already be .NET TimeSpan "c"-format strings (e.g. "01:30:00") — see shared/utils/duration.ts. */
export async function setTutorOfferedDurations(
  tutorId: string,
  durations: string[],
): Promise<void> {
  await apiClient.patch<VoidApiResult>(`/tutors/${tutorId}/offered-durations`, { durations });
}

export async function fetchPendingTutors(
  page: number,
  pageSize: number,
): Promise<PagedResult<TutorDto>> {
  const response = await apiClient.get<ApiResult<PagedResult<TutorDto>>>("/tutors/pending", {
    params: { page, pageSize },
  });
  return unwrapValue(response.data);
}

export async function fetchTutorById(tutorId: string): Promise<TutorDto> {
  const response = await apiClient.get<ApiResult<TutorDto>>(`/tutors/${tutorId}`);
  return unwrapValue(response.data);
}

/** GET /tutors returns only discoverable Tutors (approved and not suspended) — the backend's own filter, not a frontend one. */
export async function fetchTutorDirectory(): Promise<TutorDto[]> {
  const response = await apiClient.get<ApiResult<TutorDto[]>>("/tutors");
  return unwrapValue(response.data);
}

export async function fetchStudentById(studentId: string): Promise<StudentDto> {
  const response = await apiClient.get<ApiResult<StudentDto>>(`/students/${studentId}`);
  return unwrapValue(response.data);
}

export async function fetchParentGuardianById(
  parentGuardianId: string,
): Promise<ParentGuardianDto> {
  const response = await apiClient.get<ApiResult<ParentGuardianDto>>(
    `/parent-guardians/${parentGuardianId}`,
  );
  return unwrapValue(response.data);
}

export async function inviteRelationship(
  parentGuardianId: string,
  studentId: string,
): Promise<RelationshipDto> {
  const response = await apiClient.post<ApiResult<RelationshipDto>>("/relationships", {
    parentGuardianId,
    studentId,
  });
  return unwrapValue(response.data);
}

export async function confirmRelationship(relationshipId: string): Promise<void> {
  await apiClient.post<VoidApiResult>(`/relationships/${relationshipId}/confirm`);
}

export async function fetchRelationshipById(relationshipId: string): Promise<RelationshipDto> {
  const response = await apiClient.get<ApiResult<RelationshipDto>>(
    `/relationships/${relationshipId}`,
  );
  return unwrapValue(response.data);
}

export async function fetchRelationshipsForAccount(
  accountId: string,
): Promise<RelationshipDto[]> {
  const response = await apiClient.get<ApiResult<RelationshipDto[]>>(
    `/accounts/${accountId}/relationships`,
  );
  return unwrapValue(response.data);
}
