import { apiGet, apiPatch, apiPost, ApiError } from '@/lib/api/client';
import {
  CreateApplicationResponse,
  SubmitApplicationResponse,
  TutorApplication,
  UpdateDraftCommand,
} from './types';

export async function getCurrentTutorApplication(signal?: AbortSignal): Promise<TutorApplication | null> {
  try {
    return await apiGet<TutorApplication>('/api/tutors/applications/me', undefined, { signal });
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      return null;
    }
    throw error;
  }
}

export function createTutorApplication(): Promise<CreateApplicationResponse> {
  return apiPost<CreateApplicationResponse>('/api/tutors/applications');
}

export function updateTutorApplicationDraft(command: UpdateDraftCommand): Promise<TutorApplication> {
  return apiPatch<TutorApplication, UpdateDraftCommand>('/api/tutors/applications/me', command);
}

export function submitTutorApplication(): Promise<SubmitApplicationResponse> {
  return apiPost<SubmitApplicationResponse>('/api/tutors/applications/me/submit');
}

export function reopenTutorApplicationForRevision(): Promise<TutorApplication> {
  return apiPost<TutorApplication>('/api/tutors/applications/me/reopen');
}
