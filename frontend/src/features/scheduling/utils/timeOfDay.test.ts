import { describe, expect, it } from "vitest";
import { groupSlotsByTimeOfDay, timeOfDayFor } from "@/features/scheduling/utils/timeOfDay";
import { DeliveryMode } from "@/services/api/dtos";
import type { AvailabilitySlotDto } from "@/services/api/dtos";

function slot(id: string, startTimeUtc: string): AvailabilitySlotDto {
  return {
    availabilitySlotId: id,
    tutorId: "tutor-1",
    startTimeUtc,
    endTimeUtc: startTimeUtc,
    duration: "01:00:00",
    deliveryMode: DeliveryMode.Online,
    isConsumed: false,
  };
}

describe("timeOfDayFor", () => {
  // Tehran is UTC+03:30 — 05:00 UTC is 08:30 Tehran (morning), 10:00 UTC is
  // 13:30 Tehran (afternoon), 15:00 UTC is 18:30 Tehran (evening).
  it("buckets a Tehran-local morning slot", () => {
    expect(timeOfDayFor(slot("a", "2026-08-01T05:00:00.000Z"))).toBe("morning");
  });

  it("buckets a Tehran-local afternoon slot", () => {
    expect(timeOfDayFor(slot("a", "2026-08-01T10:00:00.000Z"))).toBe("afternoon");
  });

  it("buckets a Tehran-local evening slot", () => {
    expect(timeOfDayFor(slot("a", "2026-08-01T15:00:00.000Z"))).toBe("evening");
  });

  it("treats exactly noon Tehran as afternoon, not morning", () => {
    // 08:30 UTC == 12:00 Tehran
    expect(timeOfDayFor(slot("a", "2026-08-01T08:30:00.000Z"))).toBe("afternoon");
  });
});

describe("groupSlotsByTimeOfDay", () => {
  it("groups in Morning/Afternoon/Evening order, omitting empty buckets", () => {
    const groups = groupSlotsByTimeOfDay([
      slot("evening-1", "2026-08-01T15:00:00.000Z"),
      slot("morning-1", "2026-08-01T05:00:00.000Z"),
    ]);

    expect(groups.map((g) => g.timeOfDay)).toEqual(["morning", "evening"]);
    expect(groups[0].slots.map((s) => s.availabilitySlotId)).toEqual(["morning-1"]);
    expect(groups[1].slots.map((s) => s.availabilitySlotId)).toEqual(["evening-1"]);
  });

  it("returns an empty array for no slots", () => {
    expect(groupSlotsByTimeOfDay([])).toEqual([]);
  });
});
