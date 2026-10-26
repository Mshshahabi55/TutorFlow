export type TutorApplicationStatus =
  | 'Draft'
  | 'Submitted'
  | 'UnderReview'
  | 'Approved'
  | 'Rejected'
  | 'Published'
  | 'Suspended';

/** CEFR scale + Native — matches Preply's evidenced Level dropdown
 * (docs/product/preply-reverse-engineering/24-about-stage-live-evidence-2026-10-18.md §2 item 6). */
export type LanguageProficiency = 'A1' | 'A2' | 'B1' | 'B2' | 'C1' | 'C2' | 'Native';

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

export type CertificateVerificationStatus = 'None' | 'PendingReview' | 'Verified' | 'Rejected';

export interface CertificateEntryDraft {
  id: string | null;
  name: string;
  issuingOrganization: string;
  description: string | null;
  subjectId: string | null;
  startYear: number | null;
  endYear: number | null;
  // No raw storage filename here (audit finding F3) — the upload control's own state already
  // tracks what to show; the server never needs to hand this back to the client.
  hasDocument: boolean;
  verificationStatus: CertificateVerificationStatus;
  rejectionReason: string | null;
}

/** The only shape `UpdateDraftCommand.certificateEntries` may send — mirrors the backend's
 * `CertificateEntryDraftInput` exactly. `hasDocument`/`verificationStatus`/`rejectionReason`
 * are server-controlled (set only by the document-upload/review endpoints below) and must never
 * be included here, even though the backend would silently ignore them if sent — the point is
 * for the client itself to never construct a payload that looks like it's trying to set them. */
export interface CertificateEntryDraftInput {
  id: string | null;
  name: string;
  issuingOrganization: string;
  description: string | null;
  subjectId: string | null;
  startYear: number | null;
  endYear: number | null;
}

export function toCertificateEntryDraftInput(entry: CertificateEntryDraft): CertificateEntryDraftInput {
  return {
    id: entry.id,
    name: entry.name,
    issuingOrganization: entry.issuingOrganization,
    description: entry.description,
    subjectId: entry.subjectId,
    startYear: entry.startYear,
    endYear: entry.endYear,
  };
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
  phoneNumber: string | null;
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
  phoneNumber?: string;
  yearsOfExperience?: number;
  experienceSummary?: string;
  educationEntries?: EducationEntryDraft[];
  certificateEntries?: CertificateEntryDraftInput[];
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

/** The 8 evidenced steps of Preply's real tutor-application wizard, in Preply's own confirmed
 * order (docs/product/preply-reverse-engineering/18-screenshot-evidence.md, SS01-SS13 — every
 * breadcrumb in that pack reads `About › Photo › Certification › Education › Description › Video
 * › Availability › Pricing`). Previously 9 steps: 'basics' (Headline/About you) sat at position 2
 * instead of matching Preply's real position 5 ("Description"); 'credentials' merged Preply's two
 * separate Certification/Education steps into one; 'teaching' (Specialties/Years of
 * experience/Experience summary) and 'review' both had zero Preply evidence as distinct steps
 * (07-wizard-inventory.md, 16-product-decisions-and-open-questions.md item 11). This array is now
 * a direct, evidence-backed match — not a redesign, a position/grouping correction. */
export const WIZARD_STEPS = [
  'about',
  'photo',
  'certification',
  'education',
  'description',
  'video',
  'availability',
  'pricing',
] as const;

export type WizardStep = (typeof WIZARD_STEPS)[number];

/** Which steps the backend actually enforces at submission time
 * (`TutorApplication.GetSubmissionReadinessErrors`) — used only to decide where to resume the
 * wizard, never duplicated as a second validation ruleset the backend doesn't also enforce.
 * Checked in the wizard's real positional order: About, then Photo, then (skipping the optional
 * Certification/Education steps, which have no required field) Description, then Video, then
 * Pricing — the last step, which now also carries the submit action since there is no separate
 * Review step. Explicitly checks introVideoReference (a pre-existing gap in the old version of
 * this function, now closed): Review used to be the only remaining place a missing intro video
 * was ever flagged before submission; with Review removed, resume routing is the only safety net
 * left, so this fix is required by this change, not a scope-expanding addition to it. */
export function firstIncompleteStep(application: TutorApplication): WizardStep {
  if (!application.displayName || application.subjectIds.length === 0 || application.spokenLanguages.length === 0) return 'about';
  if (!application.profilePhotoReference) return 'photo';
  if (!application.headline) return 'description';
  if (!application.introVideoReference) return 'video';
  return 'pricing';
}
