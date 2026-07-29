import { describe, expect, it } from "vitest";
import { countsFromDto, deriveStatusCounts } from "@/features/scheduling/utils/sessionStatusCounts";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

function session(status: SessionStatus): SessionDto {
  return {
    sessionId: `s-${Math.random()}`,
    tutorId: "t1",
    studentId: "st1",
    parentGuardianId: null,
    availabilitySlotId: "a1",
    scheduledTimeUtc: "2026-08-01T10:00:00Z",
    endTimeUtc: "2026-08-01T11:00:00Z",
    duration: "01:00:00",
    deliveryMode: DeliveryMode.Online,
    status,
  };
}

describe("deriveStatusCounts", () => {
  it("counts sessions into their own status bucket", () => {
    const counts = deriveStatusCounts([
      session(SessionStatus.Scheduled),
      session(SessionStatus.Scheduled),
      session(SessionStatus.Completed),
      session(SessionStatus.Cancelled),
      session(SessionStatus.NoShow),
    ]);

    expect(counts).toEqual({ scheduled: 2, completed: 1, cancelled: 1, noShow: 1 });
  });

  it("returns all zeros for an empty list", () => {
    expect(deriveStatusCounts([])).toEqual({ scheduled: 0, completed: 0, cancelled: 0, noShow: 0 });
  });
});

describe("countsFromDto", () => {
  it("maps the backend DTO's fields straight through", () => {
    expect(countsFromDto({ scheduled: 3, completed: 2, cancelled: 1, noShow: 0 })).toEqual({
      scheduled: 3,
      completed: 2,
      cancelled: 1,
      noShow: 0,
    });
  });
});
