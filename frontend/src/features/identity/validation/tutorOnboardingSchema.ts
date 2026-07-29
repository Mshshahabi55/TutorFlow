import { z } from "zod";
import { isValidTomanAmount } from "@/shared/money/rial";
import { parseCommaList } from "@/features/identity/utils/commaList";

// Merge Readiness audit Critical 2 / High 1 — mirrors, for UX only, the
// same ceilings Tutor (Domain) actually enforces (TutorProfileLimits,
// MediaUrl). This is Presentation-layer convenience validation only
// (ADR-007 "Presentation Validation Responsibilities") — the backend
// remains the sole authority; a value that slips past this schema (or a
// caller that bypasses the UI entirely) is still rejected by Tutor itself.
const MAX_COLLECTION_ENTRIES = 20;
const MAX_ENTRY_LENGTH = 200;

function isWithinCollectionLimits(value: string): boolean {
  const entries = parseCommaList(value);
  return entries.length <= MAX_COLLECTION_ENTRIES && entries.every((entry) => entry.length <= MAX_ENTRY_LENGTH);
}

function isPositiveNumber(value: string): boolean {
  const parsed = Number(value);
  return value.trim().length > 0 && Number.isFinite(parsed) && parsed > 0;
}

function isHttpOrHttpsUrl(value: string): boolean {
  try {
    const url = new URL(value);
    return url.protocol === "http:" || url.protocol === "https:";
  } catch {
    return false;
  }
}

const collectionListMessage = `At most ${MAX_COLLECTION_ENTRIES} entries, each ${MAX_ENTRY_LENGTH} characters or fewer.`;
const mediaUrlMessage = "Must be a full http:// or https:// URL.";

// ADR-024 (Accepted, 2026-07-28) — Tutor Onboarding Wizard. Every field
// below is self-declared and optional (the ADR's own Verification
// section) except the two the backend's own SubmitProfile already
// requires (Subject, Hourly rate) — validated at the Review & Publish
// step, not earlier, so a Tutor can genuinely save an incomplete draft at
// any point (PROF-5). One shared schema for the whole wizard, matching
// `BookSessionPage`'s own "one form instance across every step" pattern.
export const tutorOnboardingSchema = z.object({
  // Step 1 — Personal Information
  displayName: z.string().trim().max(200, "Must be 200 characters or fewer."),
  headline: z.string().trim().max(200, "Must be 200 characters or fewer."),
  biography: z.string().trim().max(4000, "Must be 4000 characters or fewer."),
  country: z.string().trim().max(200, "Must be 200 characters or fewer."),
  city: z.string().trim().max(200, "Must be 200 characters or fewer."),
  // Pre-ADR-024 DISC-1 search/filter attribute (Tutor.Location) — grouped
  // here with country/city rather than left on its own legacy page, since
  // all three are place-based. Kept a distinct field, not merged into
  // country/city: Location.Of's own meaning ("a specific address, a
  // city/region, or a travel radius") is still DOMAIN_MODEL.md Open
  // Question 11, unresolved by ADR-024, so this schema does not assume an
  // answer either.
  location: z.string().trim().max(200, "Must be 200 characters or fewer."),
  // The existing single Tutor.Language field (already set via the
  // pre-ADR-024 /tutors/{id}/language endpoint) stands in for "Native
  // language" — OtherLanguages (new) is additive alongside it, never a
  // replacement.
  nativeLanguage: z.string().trim(),
  otherLanguages: z.string().trim().refine(isWithinCollectionLimits, collectionListMessage),

  // Step 2 — Teaching Information
  tutorSubjects: z
    .array(
      z.object({
        subject: z.string().trim().max(200, "Must be 200 characters or fewer."),
        level: z.string().trim().max(200, "Must be 200 characters or fewer."),
      }),
    )
    .max(MAX_COLLECTION_ENTRIES, `At most ${MAX_COLLECTION_ENTRIES} additional subjects.`),
  primarySubject: z.string().trim(),
  yearsOfExperience: z
    .string()
    .trim()
    .refine(
      (value) => value === "" || (Number.isInteger(Number(value)) && Number(value) >= 0),
      "Years of experience must be a whole number, zero or greater.",
    ),
  education: z.string().trim().max(4000, "Must be 4000 characters or fewer."),
  certifications: z.string().trim().max(4000, "Must be 4000 characters or fewer."),
  teachingMethodology: z.string().trim().max(4000, "Must be 4000 characters or fewer."),
  lessonSpecialties: z.string().trim().refine(isWithinCollectionLimits, collectionListMessage),

  // Step 3 — Profile Media (URLs only — see ADR-024's own Media section for
  // why; http/https-only — see the Merge Readiness audit's High 1 finding)
  photoUrl: z.string().trim().refine((value) => value === "" || isHttpOrHttpsUrl(value), mediaUrlMessage),
  introVideoUrl: z.string().trim().refine((value) => value === "" || isHttpOrHttpsUrl(value), mediaUrlMessage),
  galleryImageUrls: z
    .string()
    .trim()
    .refine(isWithinCollectionLimits, collectionListMessage)
    .refine(
      (value) => parseCommaList(value).every(isHttpOrHttpsUrl),
      "Every gallery image must be a full http:// or https:// URL.",
    ),

  // Step 4 — Pricing (single currency, Rial/Toman — ADR-019, no currency field)
  hourlyRate: z
    .string()
    .trim()
    .refine((value) => value === "" || isValidTomanAmount(value), "Hourly rate must be a whole number of Toman, greater than zero."),
  trialLessonAvailable: z.boolean(),
  trialLessonPrice: z
    .string()
    .trim()
    .refine((value) => value === "" || isValidTomanAmount(value), "Trial lesson price must be a whole number of Toman, greater than zero."),
  // Comma-separated minutes, same convention tutorOfferingSchema (now
  // retired in favor of this wizard) used — optional here, unlike that
  // page's own required field, matching this wizard's whole "nothing is
  // force-required except at Publish, and Tutor.SubmitProfile itself only
  // requires Subject + HourlyRate" philosophy.
  offeredDurationsMinutes: z
    .string()
    .trim()
    .refine(
      (value) => value === "" || value.split(",").every((part) => isPositiveNumber(part)),
      "Enter one or more positive numbers of minutes, separated by commas (e.g. 30, 60).",
    ),
});

export type TutorOnboardingFormValues = z.infer<typeof tutorOnboardingSchema>;

export const TUTOR_ONBOARDING_DEFAULT_VALUES: TutorOnboardingFormValues = {
  displayName: "",
  headline: "",
  biography: "",
  country: "",
  city: "",
  location: "",
  nativeLanguage: "",
  otherLanguages: "",
  tutorSubjects: [],
  primarySubject: "",
  yearsOfExperience: "",
  education: "",
  certifications: "",
  teachingMethodology: "",
  lessonSpecialties: "",
  photoUrl: "",
  introVideoUrl: "",
  galleryImageUrls: "",
  hourlyRate: "",
  trialLessonAvailable: false,
  trialLessonPrice: "",
  offeredDurationsMinutes: "",
};
