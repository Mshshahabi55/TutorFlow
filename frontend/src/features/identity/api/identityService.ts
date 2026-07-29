import { apiClient, unwrapValue } from "@/services/api/apiClient";
import type { ApiResult, PagedResult, VoidApiResult } from "@/services/api/apiTypes";
import type {
  ParentGuardianDto,
  RelationshipDto,
  StudentDto,
  TutorDto,
  TutorSubjectDto,
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

// ADR-024 (Accepted, 2026-07-28) — Tutor Onboarding Wizard. Every
// function below is a direct 1:1 mapping to one of the five new endpoints,
// same convention as every existing Tutor self-service function above.

export interface SetTutorPersonalInfoRequest {
  displayName: string | null;
  headline: string | null;
  biography: string | null;
  country: string | null;
  city: string | null;
  otherLanguages: string[];
}

export async function setTutorPersonalInfo(
  tutorId: string,
  request: SetTutorPersonalInfoRequest,
): Promise<void> {
  await apiClient.patch<VoidApiResult>(`/tutors/${tutorId}/personal-info`, request);
}

export interface SetTutorTeachingInfoRequest {
  tutorSubjects: TutorSubjectDto[];
  yearsOfExperience: number | null;
  education: string | null;
  certifications: string | null;
  teachingMethodology: string | null;
  lessonSpecialties: string[];
}

export async function setTutorTeachingInfo(
  tutorId: string,
  request: SetTutorTeachingInfoRequest,
): Promise<void> {
  await apiClient.patch<VoidApiResult>(`/tutors/${tutorId}/teaching-info`, request);
}

export interface SetTutorMediaRequest {
  photoUrl: string | null;
  introVideoUrl: string | null;
  galleryImageUrls: string[];
}

export async function setTutorMedia(tutorId: string, request: SetTutorMediaRequest): Promise<void> {
  await apiClient.patch<VoidApiResult>(`/tutors/${tutorId}/media`, request);
}

export interface SetTutorPricingRequest {
  hourlyRateAmount: number | null;
  trialLessonAvailable: boolean;
  trialLessonPriceAmount: number | null;
}

export async function setTutorPricing(tutorId: string, request: SetTutorPricingRequest): Promise<void> {
  await apiClient.patch<VoidApiResult>(`/tutors/${tutorId}/pricing`, request);
}

export async function submitTutorProfile(tutorId: string): Promise<void> {
  await apiClient.post<VoidApiResult>(`/tutors/${tutorId}/submit`);
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
