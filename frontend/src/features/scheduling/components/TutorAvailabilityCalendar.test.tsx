import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { TutorAvailabilityCalendar } from "@/features/scheduling/components/TutorAvailabilityCalendar";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";
import type { AvailabilitySlotDto, SessionDto } from "@/services/api/dtos";

const NOW = new Date("2026-08-15T10:00:00Z");

function slot(overrides: Partial<AvailabilitySlotDto> & Pick<AvailabilitySlotDto, "availabilitySlotId" | "startTimeUtc" | "isConsumed">): AvailabilitySlotDto {
  return {
    tutorId: "t1",
    endTimeUtc: "2026-08-15T15:00:00Z",
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
    scheduledTimeUtc: "2026-08-15T14:00:00Z",
    endTimeUtc: "2026-08-15T15:00:00Z",
    duration: "01:00:00",
    deliveryMode: DeliveryMode.Online,
    status: SessionStatus.Scheduled,
    ...overrides,
  };
}

describe("TutorAvailabilityCalendar", () => {
  it("shows today's date selected by default with a prompt to add teaching time when empty", () => {
    render(
      <TutorAvailabilityCalendar slots={[]} sessions={[]} onOpenSession={vi.fn()} onAddTeachingTime={vi.fn()} now={NOW} />,
    );

    expect(screen.getByText("No teaching time on this day.")).toBeInTheDocument();
  });

  it("shows the selected day's slots as chips", () => {
    const daySlot = slot({ availabilitySlotId: "slot-1", startTimeUtc: "2026-08-15T14:00:00Z", isConsumed: false });

    render(
      <TutorAvailabilityCalendar
        slots={[daySlot]}
        sessions={[]}
        onOpenSession={vi.fn()}
        onAddTeachingTime={vi.fn()}
        now={NOW}
      />,
    );

    expect(screen.getByText(/17:30/)).toBeInTheDocument();
  });

  it("selecting a different day updates the daily panel", async () => {
    const otherDaySlot = slot({ availabilitySlotId: "slot-2", startTimeUtc: "2026-08-20T14:00:00Z", isConsumed: false });

    render(
      <TutorAvailabilityCalendar
        slots={[otherDaySlot]}
        sessions={[]}
        onOpenSession={vi.fn()}
        onAddTeachingTime={vi.fn()}
        now={NOW}
      />,
    );

    expect(screen.getByText("No teaching time on this day.")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: /Aug 20 — 1 teaching time/ }));

    expect(screen.getByText(/17:30/)).toBeInTheDocument();
  });

  it("calls onAddTeachingTime with the selected day when clicked", async () => {
    const onAddTeachingTime = vi.fn();
    render(
      <TutorAvailabilityCalendar
        slots={[]}
        sessions={[]}
        onOpenSession={vi.fn()}
        onAddTeachingTime={onAddTeachingTime}
        now={NOW}
      />,
    );

    await userEvent.click(screen.getByRole("button", { name: "Add teaching time" }));

    expect(onAddTeachingTime).toHaveBeenCalledWith("2026-08-15");
  });

  it("clicking a booked slot chip opens its Session", async () => {
    const bookedSlot = slot({ availabilitySlotId: "slot-3", startTimeUtc: "2026-08-15T14:00:00Z", isConsumed: true });
    const linkedSession = session({ sessionId: "session-1", availabilitySlotId: "slot-3" });
    const onOpenSession = vi.fn();

    render(
      <TutorAvailabilityCalendar
        slots={[bookedSlot]}
        sessions={[linkedSession]}
        onOpenSession={onOpenSession}
        onAddTeachingTime={vi.fn()}
        now={NOW}
      />,
    );

    await userEvent.click(screen.getByText(/17:30/));

    expect(onOpenSession).toHaveBeenCalledWith(linkedSession);
  });

  it("navigating to the next month keeps the calendar usable", async () => {
    render(
      <TutorAvailabilityCalendar slots={[]} sessions={[]} onOpenSession={vi.fn()} onAddTeachingTime={vi.fn()} now={NOW} />,
    );

    expect(screen.getByText("August 2026")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Next month" }));

    expect(screen.getByText("September 2026")).toBeInTheDocument();
  });
});
