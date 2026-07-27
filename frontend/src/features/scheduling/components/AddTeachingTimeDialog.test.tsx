import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AddTeachingTimeDialog } from "@/features/scheduling/components/AddTeachingTimeDialog";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import { DeliveryMode } from "@/services/api/dtos";

const TUTOR_ID = "11111111-1111-1111-1111-111111111111";
const SLOT_ID = "22222222-2222-2222-2222-222222222222";

describe("AddTeachingTimeDialog", () => {
  it("does not render a Tutor id field — the Tutor id is already known from context", () => {
    renderWithProviders(
      <AddTeachingTimeDialog tutorId={TUTOR_ID} open onClose={() => {}} />,
    );

    expect(screen.queryByLabelText("Tutor id")).not.toBeInTheDocument();
  });

  it("prefills the start time from the given day when opened by clicking an empty calendar day", () => {
    renderWithProviders(
      <AddTeachingTimeDialog tutorId={TUTOR_ID} open onClose={() => {}} initialDateKey="2026-08-05" />,
    );

    expect(screen.getByLabelText("Start time (Tehran)")).toHaveValue("2026-08-05T09:00");
  });

  it("declares availability for the already-known Tutor id and closes on success", async () => {
    const declareAvailability = vi.spyOn(schedulingService, "declareAvailability").mockResolvedValue({
      availabilitySlotId: SLOT_ID,
      tutorId: TUTOR_ID,
      startTimeUtc: "2026-08-01T14:00:00Z",
      endTimeUtc: "2026-08-01T15:00:00Z",
      duration: "01:00:00",
      deliveryMode: DeliveryMode.Online,
      isConsumed: false,
    });
    const onClose = vi.fn();

    renderWithProviders(<AddTeachingTimeDialog tutorId={TUTOR_ID} open onClose={onClose} />);

    // 2026-08-01T17:30 Tehran (UTC+03:30) is 2026-08-01T14:00:00Z.
    await userEvent.type(screen.getByLabelText("Start time (Tehran)"), "2026-08-01T17:30");
    await userEvent.type(screen.getByLabelText("Duration (minutes)"), "60");
    await userEvent.click(screen.getByLabelText("Delivery mode"));
    await userEvent.click(await screen.findByRole("option", { name: "Online" }));
    await userEvent.click(screen.getByRole("button", { name: "Save" }));

    expect(declareAvailability).toHaveBeenCalledWith({
      tutorId: TUTOR_ID,
      startTimeUtc: "2026-08-01T14:00:00.000Z",
      duration: "01:00:00",
      deliveryMode: 0,
    });
    expect(onClose).toHaveBeenCalled();
  });
});
