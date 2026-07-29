import type { TutorDto } from "@/services/api/dtos";

export interface ProfileCompletionItem {
  label: string;
  done: boolean;
}

export interface ProfileCompletion {
  items: ProfileCompletionItem[];
  completedCount: number;
  totalCount: number;
  isComplete: boolean;
}

/**
 * Every item here is read straight off `TutorDto` (or `hasAvailability`,
 * already derived by the caller from the same `useTutorAvailabilitySlots`
 * result the rest of the Tutor workspace fetches) — no invented field, no
 * placeholder value. "Verification completed" is informational only
 * (`isApproved` is an Admin decision, not something the Tutor can act on
 * from this checklist).
 */
export function deriveProfileCompletion(tutor: TutorDto, hasAvailability: boolean): ProfileCompletion {
  const items: ProfileCompletionItem[] = [
    { label: "Profile completed", done: tutor.hourlyRate !== null && tutor.offeredDurations.length > 0 },
    { label: "Availability added", done: hasAvailability },
    { label: "Teaching subjects added", done: tutor.subject !== null },
    { label: "Languages added", done: tutor.language !== null },
    { label: "Verification completed", done: tutor.isApproved },
    // ADR-024 (Accepted, 2026-07-28) — extends, does not replace, the
    // checklist above with the new onboarding-wizard fields.
    {
      label: "Personal introduction added",
      done: Boolean(tutor.displayName || tutor.headline || tutor.biography),
    },
    { label: "Profile photo added", done: Boolean(tutor.photoUrl) },
  ];

  const completedCount = items.filter((item) => item.done).length;

  return {
    items,
    completedCount,
    totalCount: items.length,
    isComplete: completedCount === items.length,
  };
}
