import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { WeeklyAvailabilityCalendar } from "@/features/scheduling/components/WeeklyAvailabilityCalendar";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";
import type { AvailabilitySlotDto, SessionDto } from "@/services/api/dtos";

const NOW = new Date("2026-08-01T10:00:00Z");

function slot(overrides: Partial<AvailabilitySlotDto> & Pick<AvailabilitySlotDto, "availabilitySlotId" | "startTimeUtc" | "isConsumed">): AvailabilitySlotDto {
  return {
    tutorId: "t1",
    endTimeUtc: "2026-08-01T15:00:00Z",
    duration: "01:00:00",
    deliveryMode: DeliveryMode.Online,
    ...overrides,
  };
}

function session(overrides: Partial<SessionDto> & Pick<SessionDto, "sessionId" | "availabilitySlotId">): SessionDto {
  return {
    tutorId: "t1",
    studentId: "st1",
    parentGuardianId: null,
    scheduledTimeUtc: "2026-08-01T14:00:00Z",
    endTimeUtc: "2026-08-01T15:00:00Z",
    duration: "01:00:00",
    deliveryMode: DeliveryMode.Online,
    status: SessionStatus.Scheduled,
    ...overrides,
  };
}

describe("WeeklyAvailabilityCalendar", () => {
  it("shows a 'No teaching time' placeholder for a day with no declared slots", () => {
    render(<WeeklyAvailabilityCalendar slots={[]} sessions={[]} onOpenSession={() => {}} now={NOW} />);

    expect(screen.getAllByText("No teaching time").length).toBeGreaterThan(0);
  });

  it("highlights today's column", () => {
    render(<WeeklyAvailabilityCalendar slots={[]} sessions={[]} onOpenSession={() => {}} now={NOW} />);

    expect(screen.getByText(/· Today/)).toBeInTheDocument();
  });

  it("shows an open slot as clickable when it is Booked and links through to its Session", async () => {
    const bookedSlot = slot({
      availabilitySlotId: "slot-1",
      startTimeUtc: "2026-08-01T14:00:00Z",
      isConsumed: true,
    });
    const linkedSession = session({ sessionId: "session-1", availabilitySlotId: "slot-1" });
    const onOpenSession = vi.fn();

    render(
      <WeeklyAvailabilityCalendar
        slots={[bookedSlot]}
        sessions={[linkedSession]}
        onOpenSession={onOpenSession}
        now={NOW}
      />,
    );

    await userEvent.click(screen.getByText(/17:30/));

    expect(onOpenSession).toHaveBeenCalledWith(linkedSession);
  });

  it("does not make an open (not yet booked) slot clickable", async () => {
    const openSlot = slot({
      availabilitySlotId: "slot-2",
      startTimeUtc: "2026-08-02T14:00:00Z",
      isConsumed: false,
    });
    const onOpenSession = vi.fn();

    render(
      <WeeklyAvailabilityCalendar slots={[openSlot]} sessions={[]} onOpenSession={onOpenSession} now={NOW} />,
    );

    await userEvent.click(screen.getByText(/17:30/));

    expect(onOpenSession).not.toHaveBeenCalled();
  });

  it("shows the legend for every colour state", () => {
    render(<WeeklyAvailabilityCalendar slots={[]} sessions={[]} onOpenSession={() => {}} now={NOW} />);

    expect(screen.getByText("Available")).toBeInTheDocument();
    expect(screen.getByText("Booked")).toBeInTheDocument();
    expect(screen.getByText("Past (unbooked)")).toBeInTheDocument();
  });
});
