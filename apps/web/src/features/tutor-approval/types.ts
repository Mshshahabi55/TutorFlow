export type TutorApplicationStatus =
  | 'Draft'
  | 'Submitted'
  | 'UnderReview'
  | 'Approved'
  | 'Rejected'
  | 'Published'
  | 'Suspended';

export interface PendingApplicationSummary {
  id: string;
  userId: string;
  status: TutorApplicationStatus;
  displayName: string | null;
  headline: string | null;
  profilePhotoReference: string | null;
  subjectIds: string[];
  submittedAtUtc: string | null;
}

export interface PendingApplicationsPage {
  items: PendingApplicationSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/** Mirrors `GetCurrentApplication.Response` (Tutors module), reused by the admin-side
 * `GetTutorApplicationForAdmin` endpoint — the same shape as `tutor-onboarding`'s
 * `TutorApplication` type, intentionally duplicated here rather than shared: that feature is
 * tutor-facing self-service, this one is the read-only admin view, kept as separate feature
 * folders per this project's one-feature-one-folder convention. */
export interface AdminTutorApplicationDetail {
  id: string;
  userId: string;
  status: TutorApplicationStatus;
  publicSlug: string | null;
  displayName: string | null;
  headline: string | null;
  about: string | null;
  profilePhotoReference: string | null;
  introVideoReference: string | null;
  spokenLanguages: { languageCode: string; proficiency: string }[];
  subjectIds: string[];
  specialtyIds: string[];
  countryOfBirthCode: string | null;
  isProfessionalTutor: boolean;
  isSuperTutor: boolean;
  yearsOfExperience: number | null;
  experienceSummary: string | null;
  educationEntries: {
    institution: string;
    degree: string;
    fieldOfStudy: string | null;
    startYear: number | null;
    endYear: number | null;
  }[];
  certificateEntries: {
    id: string;
    name: string;
    issuingOrganization: string;
    description: string | null;
    subjectId: string | null;
    startYear: number | null;
    endYear: number | null;
    // No raw storage filename here (audit finding F3) — "View document" addresses the document
    // by certificate id through the dedicated streaming endpoint, never by filename.
    hasDocument: boolean;
    verificationStatus: 'None' | 'PendingReview' | 'Verified' | 'Rejected';
    rejectionReason: string | null;
  }[];
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
