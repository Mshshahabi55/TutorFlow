import type { AvailabilitySlotDto } from "@/services/api/dtos";

export type AvailabilitySlotState = "booked" | "available" | "past";

export function classifyAvailabilitySlot(slot: AvailabilitySlotDto, nowMs: number): AvailabilitySlotState {
  if (slot.isConsumed) {
    return "booked";
  }
  return Date.parse(slot.startTimeUtc) < nowMs ? "past" : "available";
}
