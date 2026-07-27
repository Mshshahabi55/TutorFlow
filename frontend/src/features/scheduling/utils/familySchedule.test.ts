import { describe, expect, it } from "vitest";
import type { UseQueryResult } from "@tanstack/react-query";
import { aggregateFamilySchedule } from "@/features/scheduling/utils/familySchedule";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

function session(sessionId: string, studentId: string): SessionDto {
  return {
    sessionId,
    tutorId: "t1",
    studentId,
    parentGuardianId: null,
    availabilitySlotId: "a1",
    scheduledTimeUtc: "2026-08-01T14:00:00Z",
    endTimeUtc: "2026-08-01T15:00:00Z",
    duration: "01:00:00",
    deliveryMode: DeliveryMode.Online,
    status: SessionStatus.Scheduled,
  };
}

function result(overrides: Partial<UseQueryResult<SessionDto[]>>): UseQueryResult<SessionDto[]> {
  return {
    isPending: false,
    isError: false,
    data: undefined,
    ...overrides,
  } as UseQueryResult<SessionDto[]>;
}

describe("aggregateFamilySchedule", () => {
  it("flattens every child's sessions into one list", () => {
    const aggregate = aggregateFamilySchedule([
      result({ data: [session("s1", "st-1")] }),
      result({ data: [session("s2", "st-2"), session("s3", "st-2")] }),
    ]);

    expect(aggregate.sessions.map((s) => s.sessionId)).toEqual(["s1", "s2", "s3"]);
    expect(aggregate.isPending).toBe(false);
    expect(aggregate.isError).toBe(false);
  });

  it("is pending while any child's query is still pending", () => {
    const aggregate = aggregateFamilySchedule([
      result({ data: [session("s1", "st-1")] }),
      result({ isPending: true }),
    ]);

    expect(aggregate.isPending).toBe(true);
  });

  it("is an error when any child's query failed, and surfaces that query's own real error", () => {
    const realError = new Error("Network error");
    const aggregate = aggregateFamilySchedule([
      result({ data: [session("s1", "st-1")] }),
      result({ isError: true, error: realError }),
    ]);

    expect(aggregate.isError).toBe(true);
    expect(aggregate.error).toBe(realError);
  });

  it("returns an empty list for no children", () => {
    expect(aggregateFamilySchedule([]).sessions).toEqual([]);
  });
});
