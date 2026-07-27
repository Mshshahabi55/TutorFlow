import { beforeEach, describe, expect, it, vi } from "vitest";
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

/** Drives Choose Tutor -> Choose Date -> Choose Time, leaving the wizard on the Review step with `slot` selected. */
async function advanceToReview(slot = OPEN_SLOT) {
  await userEvent.click(await screen.findByRole("button", { name: "Continue" }));
  await userEvent.click(await screen.findByRole("button", { name: /Aug 01/ }));
  await userEvent.click(await screen.findByRole("button", { name: /min · Online/ }));
  expect(await screen.findByRole("heading", { name: "Who is this lesson for?" })).toBeInTheDocument();
  return slot;
}

describe("BookSessionPage", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("prompts to find a tutor first, with no Tutor context at all", () => {
    renderWithProviders(<BookSessionPage />);

    expect(screen.getByRole("heading", { name: "Choose a tutor to get started" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Find Tutors" })).toHaveAttribute(
      "href",
      "/discovery/tutors/search",
    );
  });

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

    it("shows the Choose Tutor step first, with a step-by-step progress indicator", async () => {
      mockTutorAndSlots();

      renderWithProviders(<BookSessionPage />, {
        initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`],
      });

      expect(await screen.findByText("Mathematics")).toBeInTheDocument();
      expect(screen.getByText("Verified")).toBeInTheDocument();
      expect(screen.getByText("Speaks English")).toBeInTheDocument();
      expect(screen.getByText("50,000 Toman/hr")).toBeInTheDocument();
      expect(screen.getByText("Choose Tutor")).toBeInTheDocument();
      expect(screen.getByText("Choose Date")).toBeInTheDocument();
      expect(screen.getByText("Choose Time")).toBeInTheDocument();
      expect(screen.getByText("Review")).toBeInTheDocument();
      expect(screen.getByText("Confirm")).toBeInTheDocument();
    });

    it("groups open Availability Slots by date, excluding consumed ones, and only shows a date's own slots as time choices", async () => {
      mockTutorAndSlots([
        OPEN_SLOT,
        { ...OPEN_SLOT, availabilitySlotId: OPEN_SLOT_ID_2, startTimeUtc: "2026-08-02T10:00:00Z" },
        { ...OPEN_SLOT, availabilitySlotId: CONSUMED_SLOT_ID, isConsumed: true },
      ]);

      renderWithProviders(<BookSessionPage />, {
        initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`],
      });

      await userEvent.click(await screen.findByRole("button", { name: "Continue" }));

      expect(await screen.findByRole("heading", { name: "Choose a date" })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /Aug 01/ })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /Aug 02/ })).toBeInTheDocument();

      await userEvent.click(screen.getByRole("button", { name: /Aug 01/ }));

      expect(await screen.findByRole("heading", { name: "Choose a time" })).toBeInTheDocument();
      expect(screen.getAllByRole("button", { name: /min · Online/ })).toHaveLength(1);
    });

    it("moves to Review once a time is selected, showing a Booking Summary and no raw id fields for the slot", async () => {
      mockTutorAndSlots();

      renderWithProviders(<BookSessionPage />, {
        initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`],
      });

      await advanceToReview();

      expect(screen.getByRole("heading", { name: "Booking Summary" })).toBeInTheDocument();
      expect(screen.queryByLabelText("Availability Slot id")).not.toBeInTheDocument();
    });

    it("books the selected slot after Review + Confirm, remembering the Student id for next time", async () => {
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

      await advanceToReview();
      await userEvent.type(screen.getByLabelText("Student id"), STUDENT_ID);
      await userEvent.click(screen.getByRole("button", { name: "Continue" }));

      await userEvent.click(await screen.findByRole("button", { name: "Confirm Your Lesson" }));

      expect(bookSession).toHaveBeenCalledWith({
        availabilitySlotId: SLOT_ID,
        studentId: STUDENT_ID,
        parentGuardianId: null,
      });
      expect(await screen.findByText("Session booked")).toBeInTheDocument();
      expect(window.localStorage.getItem("tutorflow.rememberedId.student")).toBe(STUDENT_ID);
    });

    it("prefills the Student id from a remembered id, so a returning Student doesn't retype it", async () => {
      window.localStorage.setItem("tutorflow.rememberedId.student", STUDENT_ID);
      mockTutorAndSlots();

      renderWithProviders(<BookSessionPage />, {
        initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`],
      });

      await advanceToReview();

      expect(screen.getByLabelText("Student id")).toHaveValue(STUDENT_ID);
    });

    it("does not advance from Review without a Student id", async () => {
      mockTutorAndSlots();

      renderWithProviders(<BookSessionPage />, {
        initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`],
      });

      await advanceToReview();
      await userEvent.click(screen.getByRole("button", { name: "Continue" }));

      expect(screen.queryByRole("heading", { name: "Confirm Your Lesson" })).not.toBeInTheDocument();
    });

    it("skips straight to Review when arriving with a specific Availability Slot already chosen", async () => {
      mockTutorAndSlots();
      vi.spyOn(schedulingService, "fetchAvailabilitySlotById").mockResolvedValue(OPEN_SLOT);

      renderWithProviders(<BookSessionPage />, {
        initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}&availabilitySlotId=${SLOT_ID}`],
      });

      expect(
        await screen.findByRole("heading", { name: "Who is this lesson for?" }),
      ).toBeInTheDocument();
      expect(screen.getByRole("heading", { name: "Booking Summary" })).toBeInTheDocument();
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

    it("shows a friendly error panel with a Find Tutors action when the Tutor cannot be loaded, never the raw backend error", async () => {
      vi.spyOn(identityService, "fetchTutorById").mockRejectedValue(new Error("500 Internal Server Error"));
      vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([]);

      renderWithProviders(<BookSessionPage />, {
        initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`],
      });

      expect(
        await screen.findByRole("heading", { name: "We couldn’t load this tutor" }),
      ).toBeInTheDocument();
      expect(screen.queryByText("500 Internal Server Error")).not.toBeInTheDocument();
      expect(screen.getByRole("link", { name: /Find Tutors/ })).toHaveAttribute(
        "href",
        "/discovery/tutors/search",
      );
    });
  });
});
