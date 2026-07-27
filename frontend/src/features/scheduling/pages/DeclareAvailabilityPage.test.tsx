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
        isConsumed: false,
      });
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([]);

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
    expect(screen.getByText(/share this id with whoever should book it/i)).toBeInTheDocument();
  });

  // Phase 3 replaced the free-typed UTC ISO-string field with a native
  // `<input type="datetime-local">`, so a user can no longer type an
  // arbitrary malformed string into it — the browser widget only ever
  // produces a well-formed value or an empty one. This inverts the old
  // "rejects a malformed UTC timestamp" assertion (Phase 1B/2.5 precedent)
  // into "rejects a missing one".
  it("rejects a missing start time instead of submitting", async () => {
    const declareAvailability = vi.spyOn(schedulingService, "declareAvailability");
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([]);

    renderWithProviders(<DeclareAvailabilityPage />);

    await userEvent.type(screen.getByLabelText("Tutor id"), TUTOR_ID);
    await userEvent.type(screen.getByLabelText("Duration (minutes)"), "60");
    await userEvent.click(screen.getByLabelText("Delivery mode"));
    await userEvent.click(await screen.findByRole("option", { name: "Online" }));
    await userEvent.click(screen.getByRole("button", { name: "Declare availability" }));

    expect(await screen.findByText(/a date and time is required/i)).toBeInTheDocument();
    expect(declareAvailability).not.toHaveBeenCalled();
  });

  // Phase 3 Step 6: reuses the existing GET /tutors/{id}/availability-slots
  // capability (already used by the reschedule picker and the Booking
  // flow) once the Tutor id already being typed for the declare form looks
  // like a real id — not a new endpoint, not a new interaction step.
  it("shows the Tutor's own open and booked Availability Slots once a valid Tutor id is entered", async () => {
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([
      {
        availabilitySlotId: "33333333-3333-3333-3333-333333333333",
        tutorId: TUTOR_ID,
        startTimeUtc: "2026-08-03T14:00:00Z",
        endTimeUtc: "2026-08-03T15:00:00Z",
        duration: "01:00:00",
        deliveryMode: DeliveryMode.Online,
        isConsumed: false,
      },
      {
        availabilitySlotId: "44444444-4444-4444-4444-444444444444",
        tutorId: TUTOR_ID,
        startTimeUtc: "2026-08-02T14:00:00Z",
        endTimeUtc: "2026-08-02T15:00:00Z",
        duration: "01:00:00",
        deliveryMode: DeliveryMode.Online,
        isConsumed: true,
      },
    ]);

    renderWithProviders(<DeclareAvailabilityPage />);

    expect(screen.queryByRole("heading", { name: "Your Availability" })).not.toBeInTheDocument();

    await userEvent.type(screen.getByLabelText("Tutor id"), TUTOR_ID);

    expect(await screen.findByRole("heading", { name: "Your Availability" })).toBeInTheDocument();
    expect(screen.getByText("Open")).toBeInTheDocument();
    expect(await screen.findByRole("heading", { name: "Availability History" })).toBeInTheDocument();
    expect(screen.getByText("Booked")).toBeInTheDocument();
  });
});
