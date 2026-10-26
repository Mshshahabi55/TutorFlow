import { apiGet, apiPost } from '@/lib/api/client';
import { AdminTutorApplicationDetail, PendingApplicationsPage, TutorApplicationStatus } from './types';

export function getPendingTutorApplications(
  params: { status?: TutorApplicationStatus; page: number; pageSize: number },
  signal?: AbortSignal,
): Promise<PendingApplicationsPage> {
  return apiGet<PendingApplicationsPage>(
    '/api/tutors/admin/applications',
    { status: params.status, page: params.page.toString(), pageSize: params.pageSize.toString() },
    { signal },
  );
}

export function getTutorApplicationForAdmin(id: string, signal?: AbortSignal): Promise<AdminTutorApplicationDetail> {
  return apiGet<AdminTutorApplicationDetail>(`/api/tutors/admin/applications/${id}`, undefined, { signal });
}

export function beginReviewTutorApplication(id: string): Promise<AdminTutorApplicationDetail> {
  return apiPost<AdminTutorApplicationDetail>(`/api/tutors/admin/applications/${id}/begin-review`);
}

export function approveTutorApplication(id: string): Promise<AdminTutorApplicationDetail> {
  return apiPost<AdminTutorApplicationDetail>(`/api/tutors/admin/applications/${id}/approve`);
}

export function rejectTutorApplication(id: string, reason: string): Promise<AdminTutorApplicationDetail> {
  return apiPost<AdminTutorApplicationDetail, { reason: string }>(`/api/tutors/admin/applications/${id}/reject`, { reason });
}

export function publishTutorApplication(id: string): Promise<AdminTutorApplicationDetail> {
  return apiPost<AdminTutorApplicationDetail>(`/api/tutors/admin/applications/${id}/publish`);
}

export function suspendTutorApplication(id: string, reason: string): Promise<void> {
  return apiPost<void, { reason: string }>(`/api/tutors/admin/applications/${id}/suspend`, { reason });
}

export function verifyCertificate(applicationId: string, certificateId: string): Promise<void> {
  return apiPost<void>(`/api/tutors/admin/applications/${applicationId}/certificates/${certificateId}/verify`);
}

export function rejectCertificate(applicationId: string, certificateId: string, reason: string): Promise<void> {
  return apiPost<void, { reason: string }>(
    `/api/tutors/admin/applications/${applicationId}/certificates/${certificateId}/reject`,
    { reason },
  );
}
