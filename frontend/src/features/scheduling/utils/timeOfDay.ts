import { tehranHour } from "@/shared/time/tehranTime";
import type { AvailabilitySlotDto } from "@/services/api/dtos";

export type TimeOfDay = "morning" | "afternoon" | "evening";

export const TIME_OF_DAY_LABELS: Record<TimeOfDay, string> = {
  morning: "Morning",
  afternoon: "Afternoon",
  evening: "Evening",
};

/**
 * Presentation-only bucketing (no Domain concept, no stored value) — same
 * boundaries most consumer booking UIs use: Morning before noon, Afternoon
 * noon-to-5pm, Evening from 5pm on, all in Tehran local time.
 */
export function timeOfDayFor(slot: AvailabilitySlotDto): TimeOfDay {
  const hour = tehranHour(slot.startTimeUtc);
  if (hour < 12) {
    return "morning";
  }
  if (hour < 17) {
    return "afternoon";
  }
  return "evening";
}

/** Groups slots into Morning/Afternoon/Evening, in that order, omitting any bucket with nothing in it. Within a bucket, slots keep whatever order the caller already sorted them in. */
export function groupSlotsByTimeOfDay(
  slots: AvailabilitySlotDto[],
): Array<{ timeOfDay: TimeOfDay; slots: AvailabilitySlotDto[] }> {
  const buckets: Record<TimeOfDay, AvailabilitySlotDto[]> = { morning: [], afternoon: [], evening: [] };
  for (const slot of slots) {
    buckets[timeOfDayFor(slot)].push(slot);
  }
  return (["morning", "afternoon", "evening"] as const)
    .map((timeOfDay) => ({ timeOfDay, slots: buckets[timeOfDay] }))
    .filter((group) => group.slots.length > 0);
}
