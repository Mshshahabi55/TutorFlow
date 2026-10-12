export type TutorApplicationStatus =
  | 'Draft'
  | 'Submitted'
  | 'UnderReview'
  | 'Approved'
  | 'Rejected'
  | 'Published'
  | 'Suspended';

export type LanguageProficiency = 'Basic' | 'Conversational' | 'Fluent' | 'Native';

export interface SpokenLanguageDraft {
  languageCode: string;
  proficiency: LanguageProficiency;
}

export interface EducationEntryDraft {
  institution: string;
  degree: string;
  fieldOfStudy: string | null;
  startYear: number | null;
  endYear: number | null;
}

export interface CertificateEntryDraft {
  name: string;
  issuingOrganization: string;
  issueYear: number | null;
  fileReference: string | null;
}

export interface MoneyDraft {
  amountMinorUnits: number;
  currency: string;
}

/** Mirrors `GetCurrentApplication.Response` (Tutors module). */
export interface TutorApplication {
  id: string;
  userId: string;
  status: TutorApplicationStatus;
  publicSlug: string | null;
  displayName: string | null;
  headline: string | null;
  about: string | null;
  profilePhotoReference: string | null;
  introVideoReference: string | null;
  spokenLanguages: SpokenLanguageDraft[];
  subjectIds: string[];
  specialtyIds: string[];
  countryOfBirthCode: string | null;
  isProfessionalTutor: boolean;
  isSuperTutor: boolean;
  yearsOfExperience: number | null;
  experienceSummary: string | null;
  educationEntries: EducationEntryDraft[];
  certificateEntries: CertificateEntryDraft[];
  hourlyRateAmountMinorUnits: number | null;
  hourlyRateCurrency: string | null;
  availabilitySettingsConfigured: boolean;
  bookingSettingsConfigured: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  submittedAtUtc: string | null;
  reviewedAtUtc: string | null;
  publishedAtUtc: string | null;
  rejectionReason: string | null;
  suspensionReason: string | null;
}

/** Mirrors `UpdateDraft.Command` — every field optional; only sent fields are changed. */
export interface UpdateDraftCommand {
  displayName?: string;
  headline?: string;
  about?: string;
  profilePhotoReference?: string;
  introVideoReference?: string;
  spokenLanguages?: SpokenLanguageDraft[];
  subjectIds?: string[];
  specialtyIds?: string[];
  countryOfBirthCode?: string;
  yearsOfExperience?: number;
  experienceSummary?: string;
  educationEntries?: EducationEntryDraft[];
  certificateEntries?: CertificateEntryDraft[];
  hourlyRate?: MoneyDraft;
  availabilitySettingsConfigured?: boolean;
  bookingSettingsConfigured?: boolean;
}

export interface CreateApplicationResponse {
  id: string;
  userId: string;
  status: TutorApplicationStatus;
  createdAtUtc: string;
}

export interface SubmitApplicationResponse {
  id: string;
  status: TutorApplicationStatus;
  submittedAtUtc: string | null;
  publicSlug: string | null;
}

/** The 7 benchmark-confirmed sections (ADR-0026) — the sourced-but-never-identified "8th
 * section" is deliberately not invented. */
export const WIZARD_STEPS = [
  'basics',
  'photo',
  'teaching',
  'credentials',
  'video',
  'availability',
  'rate',
  'review',
] as const;

export type WizardStep = (typeof WIZARD_STEPS)[number];

/** Which steps the backend actually enforces at submission time
 * (`TutorApplication.GetSubmissionReadinessErrors`) — used only to decide where to resume the
 * wizard, never duplicated as a second validation ruleset the backend doesn't also enforce. */
export function firstIncompleteStep(application: TutorApplication): WizardStep {
  if (!application.displayName || !application.headline) return 'basics';
  if (!application.profilePhotoReference) return 'photo';
  if (application.subjectIds.length === 0 || application.spokenLanguages.length === 0) return 'teaching';
  if (!application.hourlyRateAmountMinorUnits) return 'rate';
  return 'review';
}
