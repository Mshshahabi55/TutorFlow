import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BookSessionPage } from "@/features/scheduling/pages/BookSessionPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import * as identityService from "@/features/identity/api/identityService";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";

const TUTOR_ID = "11111111-1111-1111-1111-111111111111";
const SLOT_ID = "22222222-2222-2222-2222-222222222222";
const OPEN_SLOT_ID_2 = "22222222-2222-2222-2222-222222222223";
const CONSUMED_SLOT_ID = "22222222-2222-2222-2222-222222222224";
const STUDENT_ID = "33333333-3333-3333-3333-333333333333";
const SESSION_ID = "44444444-4444-4444-4444-444444444444";

const TUTOR = {
  tutorId: TUTOR_ID,
  isApproved: true,
  isSuspended: false,
  isDiscoverable: true,
  hourlyRate: 500_000,
  subject: "Mathematics",
  language: "English",
  location: "Remote",
  offeredDurations: ["01:00:00"],
};

const OPEN_SLOT = {
  availabilitySlotId: SLOT_ID,
  tutorId: TUTOR_ID,
  startTimeUtc: "2026-08-01T14:00:00Z",
  endTimeUtc: "2026-08-01T15:00:00Z",
  duration: "01:00:00",
  deliveryMode: DeliveryMode.Online,
  isConsumed: false,
};

describe("BookSessionPage", () => {
  it("pre-fills the Availability Slot id from the query string, with no Tutor context", () => {
    vi.spyOn(schedulingService, "fetchAvailabilitySlotById").mockRejectedValue(
      new Error("no tutorId in this scenario — the slot lookup is not expected to resolve"),
    );

    renderWithProviders(<BookSessionPage />, {
      initialEntries: [`/scheduling/sessions/book?availabilitySlotId=${SLOT_ID}`],
    });

    expect(screen.getByLabelText("Availability Slot id")).toHaveValue(SLOT_ID);
  });

  it("books a session with parentGuardianId as null when left blank", async () => {
    const bookSession = vi.spyOn(schedulingService, "bookSession").mockResolvedValue({
      sessionId: SESSION_ID,
      tutorId: "t1",
      studentId: STUDENT_ID,
      parentGuardianId: null,
      availabilitySlotId: SLOT_ID,
      scheduledTimeUtc: "2026-08-01T14:00:00Z",
      endTimeUtc: "2026-08-01T15:00:00Z",
      duration: "01:00:00",
      deliveryMode: DeliveryMode.Online,
      status: SessionStatus.Scheduled,
    });

    renderWithProviders(<BookSessionPage />);

    await userEvent.type(screen.getByLabelText("Availability Slot id"), SLOT_ID);
    await userEvent.type(screen.getByLabelText("Student id"), STUDENT_ID);
    await userEvent.click(screen.getByRole("button", { name: "Book session" }));

    expect(bookSession).toHaveBeenCalledWith({
      availabilitySlotId: SLOT_ID,
      studentId: STUDENT_ID,
      parentGuardianId: null,
    });
    expect(await screen.findByText(SESSION_ID)).toBeInTheDocument();
  });

  // Phase 3 Step 4: arriving from the Tutor Profile's "Book Session" CTA
  // (`?tutorId=...`) reuses `useTutor` and the existing
  // `GET /tutors/{id}/availability-slots` capability (already used by
  // SessionDetailPage's reschedule picker) to show who/what/when instead
  // of three blank id fields.
  describe("with Tutor context (?tutorId=...)", () => {
    function mockTutorAndSlots(slots = [OPEN_SLOT]) {
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);
      vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue(slots);
    }

    it("shows a booking page skeleton while Tutor and availability context load", () => {
      mockTutorAndSlots();
      vi.spyOn(identityService, "fetchTutorById").mockReturnValue(new Promise(() => {}));

      renderWithProviders(<BookSessionPage />, {
        initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`],
      });

      expect(screen.getByTestId("booking-page-skeleton")).toBeInTheDocument();
    });

    it("shows a Tutor Summary card with real Tutor data, never a fabricated name or rating", async () => {
      mockTutorAndSlots();

      renderWithProviders(<BookSessionPage />, {
        initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`],
      });

      expect(await screen.findByText("Mathematics")).toBeInTheDocument();
      expect(screen.getByText("Verified")).toBeInTheDocument();
      expect(screen.getByText("Speaks English")).toBeInTheDocument();
      expect(screen.getByText("50,000 Toman/hr")).toBeInTheDocument();
    });

    it("shows only the open Availability Slots as selectable cards, excluding consumed ones", async () => {
      mockTutorAndSlots([
        OPEN_SLOT,
        { ...OPEN_SLOT, availabilitySlotId: OPEN_SLOT_ID_2, startTimeUtc: "2026-08-02T10:00:00Z" },
        { ...OPEN_SLOT, availabilitySlotId: CONSUMED_SLOT_ID, isConsumed: true },
      ]);

      renderWithProviders(<BookSessionPage />, {
        initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`],
      });

      await screen.findByRole("heading", { name: "Availability" });
      expect(screen.getAllByRole("button", { name: /min · Online/ })).toHaveLength(2);
      expect(screen.queryByLabelText("Availability Slot id")).not.toBeInTheDocument();
    });

    it("selects an Availability Slot by clicking its card, shows a Booking Summary, and books without typing an id", async () => {
      mockTutorAndSlots();
      const bookSession = vi.spyOn(schedulingService, "bookSession").mockResolvedValue({
        sessionId: SESSION_ID,
        tutorId: TUTOR_ID,
        studentId: STUDENT_ID,
        parentGuardianId: null,
        availabilitySlotId: SLOT_ID,
        scheduledTimeUtc: OPEN_SLOT.startTimeUtc,
        endTimeUtc: OPEN_SLOT.endTimeUtc,
        duration: OPEN_SLOT.duration,
        deliveryMode: DeliveryMode.Online,
        status: SessionStatus.Scheduled,
      });

      renderWithProviders(<BookSessionPage />, {
        initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`],
      });

      const slotCard = await screen.findByRole("button", { name: /min · Online/ });
      await userEvent.click(slotCard);

      expect(await screen.findByRole("heading", { name: "Selected Session" })).toBeInTheDocument();
      expect(await screen.findByRole("heading", { name: "Booking Summary" })).toBeInTheDocument();

      await userEvent.type(screen.getByLabelText("Student id"), STUDENT_ID);
      await userEvent.click(screen.getByRole("button", { name: "Book session" }));

      expect(bookSession).toHaveBeenCalledWith({
        availabilitySlotId: SLOT_ID,
        studentId: STUDENT_ID,
        parentGuardianId: null,
      });
      expect(await screen.findByText("Session booked")).toBeInTheDocument();
    });

    it("shows a professional empty state with a return path when the Tutor has no open Availability Slots", async () => {
      mockTutorAndSlots([]);

      renderWithProviders(<BookSessionPage />, {
        initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`],
      });

      expect(
        await screen.findByRole("heading", { name: "No availability right now" }),
      ).toBeInTheDocument();
      expect(screen.getByRole("link", { name: "Back to search" })).toHaveAttribute(
        "href",
        "/discovery/tutors/search",
      );
      expect(screen.getByRole("link", { name: "View tutor profile" })).toHaveAttribute(
        "href",
        `/identity/tutors/${TUTOR_ID}`,
      );
    });

    it("shows an error panel with a Back to search action when the Tutor cannot be loaded", async () => {
      vi.spyOn(identityService, "fetchTutorById").mockRejectedValue(new Error("Not found"));
      vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([]);

      renderWithProviders(<BookSessionPage />, {
        initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`],
      });

      expect(
        await screen.findByRole("heading", { name: "This tutor could not be loaded" }),
      ).toBeInTheDocument();
      expect(screen.getByText("Not found")).toBeInTheDocument();
      expect(screen.getByRole("link", { name: /Back to search/ })).toHaveAttribute(
        "href",
        "/discovery/tutors/search",
      );
    });
  });
});
