import { apiDelete, apiGet, apiPut } from '@/lib/api/client';
import { SearchTutorsResponse } from '../search/types';
import { AvailabilityPreview, FavoriteStatus, TrialEligibility, TutorPublicProfile } from './types';

export interface TutorSlotDto {
  startUtc: string;
  endUtc: string;
}

export interface TutorSlotsResponse {
  timeZoneId: string;
  durationMinutes: number;
  maxBookingWindowDays: number;
  slots: TutorSlotDto[];
}

export interface GetTutorSlotsParams {
  durationMinutes?: number;
  /** ISO 8601 instants — the backend remains the source of truth for what's open; these only
   * scope the requested window (ADR-0025), they never compute availability client-side. */
  fromUtc?: string;
  toUtc?: string;
}

export function getTutorProfile(slug: string, options?: { signal?: AbortSignal }): Promise<TutorPublicProfile> {
  return apiGet<TutorPublicProfile>(`/api/tutors/${encodeURIComponent(slug)}`, undefined, options);
}

export function getAvailabilityPreview(
  tutorUserId: string,
  options?: { signal?: AbortSignal },
): Promise<AvailabilityPreview> {
  return apiGet<AvailabilityPreview>(`/api/availability/tutors/${tutorUserId}/preview`, undefined, options);
}

/** Full slot grid — requires an authenticated Student. Duration/date-range-aware (ADR-0025): a
 * different durationMinutes is a real backend recomputation, never a client-side filter of one
 * fixed-duration list. */
export function getTutorSlots(
  tutorUserId: string,
  params?: GetTutorSlotsParams,
  options?: { signal?: AbortSignal },
): Promise<TutorSlotsResponse> {
  return apiGet<TutorSlotsResponse>(
    `/api/availability/tutors/${tutorUserId}/slots`,
    {
      durationMinutes: params?.durationMinutes?.toString(),
      fromUtc: params?.fromUtc,
      toUtc: params?.toUtc,
    },
    options,
  );
}

export function getTrialEligibility(
  tutorUserId: string,
  options?: { signal?: AbortSignal },
): Promise<TrialEligibility> {
  return apiGet<TrialEligibility>(`/api/bookings/me/trial-eligibility/${tutorUserId}`, undefined, options);
}

export function getFavoriteStatus(tutorUserId: string, options?: { signal?: AbortSignal }): Promise<FavoriteStatus> {
  return apiGet<FavoriteStatus>(`/api/students/me/favorites/${tutorUserId}`, undefined, options);
}

export interface FavoriteStatusesResponse {
  favoritedTutorUserIds: string[];
}

/** Bulk counterpart to {@link getFavoriteStatus} (ADR-0024) — one request for a whole page of
 * search results instead of one per card. Single-tutor get/add/remove stay as-is and are reused
 * unchanged by both Tutor Profile and Search. */
export function getFavoriteStatuses(
  tutorUserIds: string[],
  options?: { signal?: AbortSignal },
): Promise<FavoriteStatusesResponse> {
  if (tutorUserIds.length === 0) {
    return Promise.resolve({ favoritedTutorUserIds: [] });
  }
  return apiGet<FavoriteStatusesResponse>(
    '/api/students/me/favorites',
    { tutorUserIds: tutorUserIds.join(',') },
    options,
  );
}

export function addFavorite(tutorUserId: string): Promise<FavoriteStatus> {
  return apiPut<FavoriteStatus>(`/api/students/me/favorites/${tutorUserId}`);
}

export function removeFavorite(tutorUserId: string): Promise<void> {
  return apiDelete(`/api/students/me/favorites/${tutorUserId}`);
}

/** "Similar tutors" reuses Search's existing public endpoint (same subject, excluding the
 * profile being viewed) rather than a bespoke recommendation query (ADR-0022,
 * PREPLY_TUTOR_PROFILE_PARITY_SPEC.md §8) — deliberately bypasses the full `SearchFilters`
 * object/`filtersToApiParams` pipeline since none of that UI (sort, price range, etc.) applies
 * here; only the two params this section needs are sent. */
export function getSimilarTutors(
  subjectId: string | undefined,
  excludeTutorUserId: string,
  options?: { signal?: AbortSignal },
): Promise<SearchTutorsResponse> {
  return apiGet<SearchTutorsResponse>(
    '/api/search/tutors',
    { subjectId, excludeTutorUserId, pageSize: '4' },
    options,
  );
}
