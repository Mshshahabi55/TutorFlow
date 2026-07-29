import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AvailabilityPreviewCalendar } from "@/features/identity/components/AvailabilityPreviewCalendar";
import { DeliveryMode } from "@/services/api/dtos";
import type { AvailabilitySlotDto } from "@/services/api/dtos";

const NOW = new Date("2026-08-15T10:00:00Z");

function slot(overrides: Partial<AvailabilitySlotDto> & Pick<AvailabilitySlotDto, "availabilitySlotId" | "startTimeUtc" | "isConsumed">): AvailabilitySlotDto {
  return {
    tutorId: "t1",
    endTimeUtc: "2026-08-20T15:00:00Z",
    duration: "01:00:00",
    deliveryMode: DeliveryMode.Online,
    ...overrides,
  };
}

describe("AvailabilityPreviewCalendar", () => {
  it("marks a day with an open slot as available, purely informationally (no click handler)", () => {
    render(
      <AvailabilityPreviewCalendar
        slots={[slot({ availabilitySlotId: "s1", startTimeUtc: "2026-08-20T14:00:00Z", isConsumed: false })]}
        now={NOW}
      />,
    );

    expect(screen.getByLabelText(/Aug 20 — available/)).toBeInTheDocument();
    // No day cell renders as a button — this preview is read-only, unlike
    // the Tutor's own or the booking wizard's calendars.
    expect(screen.queryAllByRole("button", { name: /Aug/ })).toHaveLength(0);
  });

  it("marks a day with only a consumed (booked) slot as unavailable, since a Student preview has no reason to distinguish booked from empty", () => {
    render(
      <AvailabilityPreviewCalendar
        slots={[slot({ availabilitySlotId: "s1", startTimeUtc: "2026-08-20T14:00:00Z", isConsumed: true })]}
        now={NOW}
      />,
    );

    expect(screen.getByLabelText(/Aug 20 — no availability/)).toBeInTheDocument();
  });

  it("shows a legend explaining the availability indicator", () => {
    render(<AvailabilityPreviewCalendar slots={[]} now={NOW} />);

    expect(screen.getByText("Has open teaching times")).toBeInTheDocument();
  });

  it("still allows navigating between months (browsing, not booking)", async () => {
    render(<AvailabilityPreviewCalendar slots={[]} now={NOW} />);

    expect(screen.getByText("August 2026")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Next month" }));

    expect(screen.getByText("September 2026")).toBeInTheDocument();
  });
});
