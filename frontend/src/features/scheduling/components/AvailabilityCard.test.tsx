import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AvailabilityCard } from "@/features/scheduling/components/AvailabilityCard";
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

describe("AvailabilityCard", () => {
  it("shows the slot's real start time, duration, and delivery mode", () => {
    render(<AvailabilityCard slot={SLOT} selected={false} onSelect={() => {}} />);

    expect(screen.getByText("60 min · Online")).toBeInTheDocument();
  });

  it("calls onSelect with the slot when clicked, and is keyboard-activatable", async () => {
    const onSelect = vi.fn();
    render(<AvailabilityCard slot={SLOT} selected={false} onSelect={onSelect} />);

    const card = screen.getByRole("button");
    card.focus();
    await userEvent.keyboard("{Enter}");

    expect(onSelect).toHaveBeenCalledWith(SLOT);
  });

  it("marks itself pressed and shows a Selected indicator once selected", () => {
    render(<AvailabilityCard slot={SLOT} selected={true} onSelect={() => {}} />);

    expect(screen.getByRole("button")).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByTitle("Selected")).toBeInTheDocument();
  });

  it("is a plain button, never a submit control, so it cannot accidentally submit a surrounding form", () => {
    render(<AvailabilityCard slot={SLOT} selected={false} onSelect={() => {}} />);

    expect(screen.getByRole("button")).toHaveAttribute("type", "button");
  });
});
