import { useTutorAvailabilitySlots } from "@/features/scheduling/hooks/useAvailabilitySlotQueries";
import { tehranDateKey, tehranDateLabel } from "@/shared/time/tehranTime";

/**
 * A Tehran-formatted label for a Tutor's soonest open, non-consumed
 * Availability Slot (e.g. `"Sat, Aug 01"`), or `null` if they have none
 * right now. Returns `undefined` while pending/on error — a caller should
 * simply omit the fact rather than show a stale or wrong one.
 *
 * Extracted from `TutorProfileHero` (Phase 9) once a second real consumer
 * appeared (the Tutor Profile's sticky booking card) — the same
 * "two real usages, extract" precedent Phase 8a already established for
 * `AvailabilitySlotChip`/`AvailabilityLegend`. Deliberately not used by
 * Discovery's `TutorCard`: computing this per card in a search-results grid
 * would mean one extra HTTP request per card (an N+1 pattern), which is
 * exactly why this hook was originally kept private to a single-tutor page
 * rather than reused there.
 */
export function useNextAvailableLabel(tutorId: string): string | null | undefined {
  const slotsQuery = useTutorAvailabilitySlots(tutorId);

  if (!slotsQuery.isSuccess) {
    return undefined;
  }

  const nextSlot = slotsQuery.data
    .filter((slot) => !slot.isConsumed)
    .sort((a, b) => Date.parse(a.startTimeUtc) - Date.parse(b.startTimeUtc))[0];

  return nextSlot ? tehranDateLabel(tehranDateKey(nextSlot.startTimeUtc)) : null;
}
