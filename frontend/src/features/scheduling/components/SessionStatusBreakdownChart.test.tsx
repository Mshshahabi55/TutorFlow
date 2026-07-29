import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { SessionStatusBreakdownChart } from "@/features/scheduling/components/SessionStatusBreakdownChart";
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

describe("SessionStatusBreakdownChart", () => {
  it("counts sessions into their own status bucket", () => {
    render(
      <SessionStatusBreakdownChart
        sessions={[
          session(SessionStatus.Scheduled),
          session(SessionStatus.Scheduled),
          session(SessionStatus.Completed),
          session(SessionStatus.Cancelled),
          session(SessionStatus.NoShow),
        ]}
      />,
    );

    expect(screen.getByText("Upcoming")).toBeInTheDocument();
    expect(screen.getByText("2")).toBeInTheDocument();
    expect(screen.getByText("Completed")).toBeInTheDocument();
    expect(screen.getByText("Cancelled")).toBeInTheDocument();
    expect(screen.getByText("No-Show")).toBeInTheDocument();
  });

  it("shows every status bucket at zero when there are no sessions, rather than omitting them", () => {
    render(<SessionStatusBreakdownChart sessions={[]} />);

    expect(screen.getAllByText("0")).toHaveLength(4);
  });
});
