import { SpokenLanguageDto } from '../search/types';

export interface EducationEntryDto {
  institution: string;
  degree: string;
  fieldOfStudy: string | null;
  startYear: number | null;
  endYear: number | null;
}

export interface CertificateEntryDto {
  name: string;
  issuingOrganization: string;
  issueYear: number | null;
  fileReference: string | null;
}

/** Mirrors `GetPublicProfile.Response` (Tutors module). `tutorUserId` is plumbing only — used
 * for subsequent API calls (availability, trial eligibility, favorites), never rendered or put
 * in a URL (ADR-0022). */
export interface TutorPublicProfile {
  tutorUserId: string;
  slug: string;
  displayName: string;
  headline: string | null;
  about: string | null;
  profilePhotoReference: string | null;
  introVideoReference: string | null;
  spokenLanguages: SpokenLanguageDto[];
  subjectIds: string[];
  specialtyIds: string[];
  countryOfBirthCode: string | null;
  isProfessionalTutor: boolean;
  isSuperTutor: boolean;
  yearsOfExperience: number | null;
  experienceSummary: string | null;
  educationEntries: EducationEntryDto[];
  certificateEntries: CertificateEntryDto[];
  hourlyRateAmountMinorUnits: number;
  hourlyRateCurrency: string;
  isAcceptingNewStudents: boolean;
}

/** Mirrors `GetTutorAvailabilityPreview.Response` (Availability module). */
export interface AvailabilityPreview {
  hasUpcomingAvailability: boolean;
  nextAvailableStartUtc: string | null;
  timeZoneId: string | null;
}

/** Mirrors `GetTrialEligibility.Response` (Bookings module). */
export interface TrialEligibility {
  hasUsedTrial: boolean;
}

/** Approved product decision (ADR-0025) — fixed, not fetched from config. Must match
 * Bookings' own `BookingsOptions.AllowedTrialDurationsMinutes` default; if that default is ever
 * changed server-side, this is the one place to update on the frontend too. */
export const TRIAL_DURATIONS_MINUTES = [25, 50] as const;
export type TrialDurationMinutes = (typeof TRIAL_DURATIONS_MINUTES)[number];

/** Mirrors `GetFavoriteStatus`/`AddFavorite` responses (Students module). */
export interface FavoriteStatus {
  isFavorited: boolean;
}
