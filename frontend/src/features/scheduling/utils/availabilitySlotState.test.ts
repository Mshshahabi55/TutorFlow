import { describe, expect, it } from "vitest";
import { classifyAvailabilitySlot } from "@/features/scheduling/utils/availabilitySlotState";
import { DeliveryMode } from "@/services/api/dtos";
import type { AvailabilitySlotDto } from "@/services/api/dtos";

const NOW_MS = Date.parse("2026-08-15T10:00:00Z");

function slot(overrides: Partial<AvailabilitySlotDto> & Pick<AvailabilitySlotDto, "startTimeUtc" | "isConsumed">): AvailabilitySlotDto {
  return {
    availabilitySlotId: "s1",
    tutorId: "t1",
    endTimeUtc: "2026-08-15T15:00:00Z",
    duration: "01:00:00",
    deliveryMode: DeliveryMode.Online,
    ...overrides,
  };
}

describe("classifyAvailabilitySlot", () => {
  it("returns booked for a consumed slot regardless of time", () => {
    expect(classifyAvailabilitySlot(slot({ startTimeUtc: "2020-01-01T00:00:00Z", isConsumed: true }), NOW_MS)).toBe(
      "booked",
    );
  });

  it("returns past for an unconsumed slot whose start time has already passed", () => {
    expect(classifyAvailabilitySlot(slot({ startTimeUtc: "2020-01-01T00:00:00Z", isConsumed: false }), NOW_MS)).toBe(
      "past",
    );
  });

  it("returns available for an unconsumed, future slot", () => {
    expect(classifyAvailabilitySlot(slot({ startTimeUtc: "2026-08-20T00:00:00Z", isConsumed: false }), NOW_MS)).toBe(
      "available",
    );
  });
});
