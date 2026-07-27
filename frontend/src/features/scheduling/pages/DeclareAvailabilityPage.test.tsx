import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { DeclareAvailabilityPage } from "@/features/scheduling/pages/DeclareAvailabilityPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";

const TUTOR_ID = "11111111-1111-1111-1111-111111111111";
const SLOT_ID = "22222222-2222-2222-2222-222222222222";

function rememberTutor() {
  window.localStorage.setItem("tutorflow.rememberedId.tutor", TUTOR_ID);
}

describe("DeclareAvailabilityPage", () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.spyOn(schedulingService, "fetchTutorSchedule").mockResolvedValue([]);
  });

  it("shows a friendly identity prompt, not a raw Tutor id field, when no id is known", () => {
    renderWithProviders(<DeclareAvailabilityPage />);

    expect(screen.getByRole("heading", { name: "Let's set up your schedule" })).toBeInTheDocument();
    expect(screen.getByLabelText("Tutor id")).toBeInTheDocument();
  });

  it("shows an empty state with an Add Availability CTA, never a raw form, when there is no teaching time yet", async () => {
    rememberTutor();
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([]);

    renderWithProviders(<DeclareAvailabilityPage />);

    expect(await screen.findByText("You haven't added any teaching time")).toBeInTheDocument();
    expect(screen.queryByLabelText("Start time (Tehran)")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Add Availability" })).toBeInTheDocument();
  });

  it("shows the calendar first, once at least one slot has been declared", async () => {
    rememberTutor();
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([
      {
        availabilitySlotId: SLOT_ID,
        tutorId: TUTOR_ID,
        startTimeUtc: new Date(Date.now() + 60 * 60_000).toISOString(),
        endTimeUtc: new Date(Date.now() + 2 * 60 * 60_000).toISOString(),
        duration: "01:00:00",
        deliveryMode: DeliveryMode.Online,
        isConsumed: false,
      },
    ]);

    renderWithProviders(<DeclareAvailabilityPage />);

    // "Available" only exists in the calendar's own legend once the slots
    // query has actually resolved — awaiting it directly (rather than the
    // "Teaching Schedule" heading, which a transient pending-state
    // SectionCard shares the same title with) avoids a stale-node race.
    expect(await screen.findByText("Available")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Teaching Schedule" })).toBeInTheDocument();
  });

  it("opens the Add Teaching Time dialog from its own button, without exposing the form up front", async () => {
    rememberTutor();
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([]);

    renderWithProviders(<DeclareAvailabilityPage />);
    await screen.findByText("You haven't added any teaching time");

    expect(screen.queryByLabelText("Start time (Tehran)")).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Add Teaching Time" }));

    expect(await screen.findByLabelText("Start time (Tehran)")).toBeInTheDocument();
  });

  it("declares availability for the already-known Tutor id, converting minutes to a TimeSpan and the mode to its numeric value", async () => {
    rememberTutor();
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([]);
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

    renderWithProviders(<DeclareAvailabilityPage />);
    await userEvent.click(await screen.findByRole("button", { name: "Add Teaching Time" }));

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
  });

  it("shows booked Availability Slots collapsed under a secondary History section", async () => {
    rememberTutor();
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([
      {
        availabilitySlotId: SLOT_ID,
        tutorId: TUTOR_ID,
        startTimeUtc: "2020-01-01T14:00:00Z",
        endTimeUtc: "2020-01-01T15:00:00Z",
        duration: "01:00:00",
        deliveryMode: DeliveryMode.Online,
        isConsumed: true,
      },
    ]);
    vi.spyOn(schedulingService, "fetchTutorSchedule").mockResolvedValue([
      {
        sessionId: "44444444-4444-4444-4444-444444444444",
        tutorId: TUTOR_ID,
        studentId: "st1",
        parentGuardianId: null,
        availabilitySlotId: SLOT_ID,
        scheduledTimeUtc: "2020-01-01T14:00:00Z",
        endTimeUtc: "2020-01-01T15:00:00Z",
        duration: "01:00:00",
        deliveryMode: DeliveryMode.Online,
        status: SessionStatus.Completed,
      },
    ]);

    renderWithProviders(<DeclareAvailabilityPage />);

    expect(await screen.findByRole("heading", { name: "History" })).toBeInTheDocument();
    expect(screen.getByText("1 booked lesson on record.")).toBeInTheDocument();
    // "Booked" already appears once, in the calendar's own legend — the
    // collapsed History section itself must not add a second one.
    expect(screen.getAllByText("Booked")).toHaveLength(1);

    await userEvent.click(screen.getByRole("button", { name: "Show" }));

    expect(await screen.findAllByText("Booked")).toHaveLength(2);
  });
});
