import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { DeclareAvailabilityPage } from "@/features/scheduling/pages/DeclareAvailabilityPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import { DeliveryMode } from "@/services/api/dtos";

const TUTOR_ID = "11111111-1111-1111-1111-111111111111";
const SLOT_ID = "22222222-2222-2222-2222-222222222222";

describe("DeclareAvailabilityPage", () => {
  it("declares availability, converting minutes to a TimeSpan and the mode to its numeric value", async () => {
    const declareAvailability = vi
      .spyOn(schedulingService, "declareAvailability")
      .mockResolvedValue({
        availabilitySlotId: SLOT_ID,
        tutorId: TUTOR_ID,
        startTimeUtc: "2026-08-01T14:00:00Z",
        endTimeUtc: "2026-08-01T15:00:00Z",
        duration: "01:00:00",
        deliveryMode: DeliveryMode.Online,
      });

    renderWithProviders(<DeclareAvailabilityPage />);

    await userEvent.type(screen.getByLabelText("Tutor id"), TUTOR_ID);
    // 2026-08-01T17:30 Tehran (UTC+03:30) is 2026-08-01T14:00:00Z.
    await userEvent.type(screen.getByLabelText("Start time (Tehran)"), "2026-08-01T17:30");
    await userEvent.type(screen.getByLabelText("Duration (minutes)"), "60");
    await userEvent.click(screen.getByLabelText("Delivery mode"));
    await userEvent.click(await screen.findByRole("option", { name: "Online" }));
    await userEvent.click(screen.getByRole("button", { name: "Declare availability" }));

    expect(declareAvailability).toHaveBeenCalledWith({
      tutorId: TUTOR_ID,
      startTimeUtc: "2026-08-01T14:00:00.000Z",
      duration: "01:00:00",
      deliveryMode: 0,
    });
    expect(await screen.findByText(SLOT_ID)).toBeInTheDocument();
    expect(
      screen.getByText(/there is no way to browse open slots/i),
    ).toBeInTheDocument();
  });

  // Phase 3 replaced the free-typed UTC ISO-string field with a native
  // `<input type="datetime-local">`, so a user can no longer type an
  // arbitrary malformed string into it — the browser widget only ever
  // produces a well-formed value or an empty one. This inverts the old
  // "rejects a malformed UTC timestamp" assertion (Phase 1B/2.5 precedent)
  // into "rejects a missing one".
  it("rejects a missing start time instead of submitting", async () => {
    const declareAvailability = vi.spyOn(schedulingService, "declareAvailability");

    renderWithProviders(<DeclareAvailabilityPage />);

    await userEvent.type(screen.getByLabelText("Tutor id"), TUTOR_ID);
    await userEvent.type(screen.getByLabelText("Duration (minutes)"), "60");
    await userEvent.click(screen.getByLabelText("Delivery mode"));
    await userEvent.click(await screen.findByRole("option", { name: "Online" }));
    await userEvent.click(screen.getByRole("button", { name: "Declare availability" }));

    expect(await screen.findByText(/a date and time is required/i)).toBeInTheDocument();
    expect(declareAvailability).not.toHaveBeenCalled();
  });
});
