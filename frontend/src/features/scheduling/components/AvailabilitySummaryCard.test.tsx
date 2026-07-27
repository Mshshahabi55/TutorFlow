import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { AvailabilitySummaryCard } from "@/features/scheduling/components/AvailabilitySummaryCard";
import { DeliveryMode } from "@/services/api/dtos";
import type { AvailabilitySlotDto } from "@/services/api/dtos";

const SLOT: AvailabilitySlotDto = {
  availabilitySlotId: "22222222-2222-2222-2222-222222222222",
  tutorId: "11111111-1111-1111-1111-111111111111",
  startTimeUtc: "2026-08-01T14:00:00Z",
  endTimeUtc: "2026-08-01T15:00:00Z",
  duration: "01:00:00",
  deliveryMode: DeliveryMode.Online,
  isConsumed: false,
};

function renderCard(slot: AvailabilitySlotDto) {
  return render(
    <MemoryRouter>
      <AvailabilitySummaryCard slot={slot} />
    </MemoryRouter>,
  );
}

describe("AvailabilitySummaryCard", () => {
  it("shows Open for a not-yet-booked slot", () => {
    renderCard(SLOT);

    expect(screen.getByText("Open")).toBeInTheDocument();
    expect(screen.getByText("60 min · Online")).toBeInTheDocument();
  });

  it("shows Booked for a consumed slot", () => {
    renderCard({ ...SLOT, isConsumed: true });

    expect(screen.getByText("Booked")).toBeInTheDocument();
  });

  it("links to the slot's existing detail route", () => {
    renderCard(SLOT);

    expect(screen.getByRole("link", { name: "View details" })).toHaveAttribute(
      "href",
      "/scheduling/availability/22222222-2222-2222-2222-222222222222",
    );
  });
});
