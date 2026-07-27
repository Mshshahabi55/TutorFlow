import { describe, expect, it } from "vitest";
import { deriveStudentRoster } from "@/features/scheduling/utils/studentRoster";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

function session(overrides: Partial<SessionDto> & Pick<SessionDto, "sessionId" | "studentId" | "status">): SessionDto {
  return {
    tutorId: "t1",
    parentGuardianId: null,
    availabilitySlotId: "a1",
    scheduledTimeUtc: "2026-08-01T14:00:00Z",
    endTimeUtc: "2026-08-01T15:00:00Z",
    duration: "01:00:00",
    deliveryMode: DeliveryMode.Online,
    ...overrides,
  };
}

describe("deriveStudentRoster", () => {
  it("groups sessions by Student and counts totals/upcoming", () => {
    const roster = deriveStudentRoster([
      session({ sessionId: "s1", studentId: "st-1", status: SessionStatus.Scheduled }),
      session({ sessionId: "s2", studentId: "st-1", status: SessionStatus.Completed }),
      session({ sessionId: "s3", studentId: "st-2", status: SessionStatus.Completed }),
    ]);

    const st1 = roster.find((entry) => entry.studentId === "st-1");
    expect(st1?.totalSessions).toBe(2);
    expect(st1?.upcomingSessions).toBe(1);
    expect(roster.find((entry) => entry.studentId === "st-2")?.totalSessions).toBe(1);
  });

  it("picks the soonest Scheduled session as nextSession", () => {
    const later = session({
      sessionId: "later",
      studentId: "st-1",
      status: SessionStatus.Scheduled,
      scheduledTimeUtc: "2026-08-05T14:00:00Z",
    });
    const sooner = session({
      sessionId: "sooner",
      studentId: "st-1",
      status: SessionStatus.Scheduled,
      scheduledTimeUtc: "2026-08-02T14:00:00Z",
    });

    const roster = deriveStudentRoster([later, sooner]);

    expect(roster[0].nextSession?.sessionId).toBe("sooner");
  });

  it("picks the most recent non-Scheduled session as mostRecentPastSession", () => {
    const older = session({
      sessionId: "older",
      studentId: "st-1",
      status: SessionStatus.Completed,
      scheduledTimeUtc: "2026-07-01T14:00:00Z",
    });
    const newer = session({
      sessionId: "newer",
      studentId: "st-1",
      status: SessionStatus.Cancelled,
      scheduledTimeUtc: "2026-07-20T14:00:00Z",
    });

    const roster = deriveStudentRoster([older, newer]);

    expect(roster[0].mostRecentPastSession?.sessionId).toBe("newer");
  });

  it("leaves nextSession/mostRecentPastSession undefined when there is no matching session", () => {
    const roster = deriveStudentRoster([
      session({ sessionId: "s1", studentId: "st-1", status: SessionStatus.Scheduled }),
    ]);

    expect(roster[0].nextSession?.sessionId).toBe("s1");
    expect(roster[0].mostRecentPastSession).toBeUndefined();
  });
});
