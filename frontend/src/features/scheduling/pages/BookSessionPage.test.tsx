import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BookSessionPage } from "@/features/scheduling/pages/BookSessionPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import { AuthHarness } from "@/test/AuthHarness";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import * as identityService from "@/features/identity/api/identityService";
import { DeliveryMode, RelationshipStatus, SessionStatus } from "@/services/api/dtos";
import type { AuthenticatedUser } from "@/shared/context/AuthContext";

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
      // "Choose Tutor" (the current step) renders twice — once in the
      // desktop Stepper, once in the mobile-only progress heading; CSS
      // controls which is visible per viewport, jsdom renders both.
      expect(screen.getAllByText("Choose Tutor")).toHaveLength(2);
      expect(screen.getByText("Choose Date")).toBeInTheDocument();
      expect(screen.getByText("Choose Time")).toBeInTheDocument();
      expect(screen.getByText("Review")).toBeInTheDocument();
      expect(screen.getByText("Confirm")).toBeInTheDocument();
    });

    it("shows a mobile progress bar reflecting the current step, and moves focus to each new step for screen-reader users", async () => {
      mockTutorAndSlots();

      renderWithProviders(<BookSessionPage />, {
        initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`],
      });

      await screen.findByText("Mathematics");
      expect(screen.getByLabelText("Step 1 of 5: Choose Tutor")).toHaveAttribute(
        "aria-valuenow",
        "20",
      );
      expect(screen.getByRole("group", { name: "Choose Tutor" })).toHaveFocus();

      await userEvent.click(screen.getByRole("button", { name: "Continue" }));

      expect(await screen.findByRole("group", { name: "Choose Date" })).toHaveFocus();
      expect(screen.getByLabelText("Step 2 of 5: Choose Date")).toHaveAttribute(
        "aria-valuenow",
        "40",
      );
    });

    it("keeps the Tutor summary (identity + price) visible across every step, never repeated inside the Booking Summary", async () => {
      mockTutorAndSlots();

      renderWithProviders(<BookSessionPage />, {
        initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`],
      });

      await advanceToReview();

      // The Tutor summary card is still visible at Review…
      expect(screen.getByText("Mathematics")).toBeInTheDocument();
      expect(screen.getByText("50,000 Toman/hr")).toBeInTheDocument();
      // …exactly once each — the Booking Summary itself no longer repeats them.
      expect(screen.getAllByText("Mathematics")).toHaveLength(1);
      expect(screen.getAllByText("50,000 Toman/hr")).toHaveLength(1);
      expect(screen.queryByText("Tutor")).not.toBeInTheDocument();
      expect(screen.queryByText("Price")).not.toBeInTheDocument();
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
      expect(await screen.findByText("Your lesson is booked!")).toBeInTheDocument();
      expect(screen.getByRole("link", { name: "My Lessons" })).toHaveAttribute(
        "href",
        "/scheduling/students",
      );
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
        await screen.findByRole("heading", { name: "Tutor unavailable" }),
      ).toBeInTheDocument();
      expect(screen.queryByText("500 Internal Server Error")).not.toBeInTheDocument();
      expect(screen.getByRole("link", { name: /Find another tutor/ })).toHaveAttribute(
        "href",
        "/discovery/tutors/search",
      );
    });
  });

  describe("RC4.3: identity resolves from a real signed-in session, never a typed id", () => {
    function mockTutorAndSlots(slots = [OPEN_SLOT]) {
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);
      vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue(slots);
    }

    const STUDENT_USER: AuthenticatedUser = {
      token: "t",
      accountId: STUDENT_ID,
      role: "Student",
      expiresAtUtc: "2999-01-01T00:00:00Z",
      email: "student@example.com",
    };

    const PARENT_ID = "55555555-5555-5555-5555-555555555555";
    const PARENT_USER: AuthenticatedUser = {
      token: "t",
      accountId: PARENT_ID,
      role: "ParentGuardian",
      expiresAtUtc: "2999-01-01T00:00:00Z",
      email: "parent@example.com",
    };

    it("a signed-in Student never sees a Student id field — it books for themselves automatically", async () => {
      mockTutorAndSlots();

      renderWithProviders(
        <>
          <AuthHarness user={STUDENT_USER} />
          <BookSessionPage />
        </>,
        { initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`] },
      );

      await advanceToReview();

      expect(screen.getByText("Booking for yourself.")).toBeInTheDocument();
      expect(screen.queryByLabelText("Student id")).not.toBeInTheDocument();
      expect(screen.queryByLabelText(/Parent\/Guardian id/)).not.toBeInTheDocument();

      await userEvent.click(screen.getByRole("button", { name: "Continue" }));
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
      await userEvent.click(await screen.findByRole("button", { name: "Confirm Your Lesson" }));

      expect(bookSession).toHaveBeenCalledWith({
        availabilitySlotId: SLOT_ID,
        studentId: STUDENT_ID,
        parentGuardianId: null,
      });
    });

    it("a signed-in Parent/Guardian chooses a confirmed child from a list — never types a Student id", async () => {
      mockTutorAndSlots();
      vi.spyOn(identityService, "fetchRelationshipsForAccount").mockResolvedValue([
        {
          relationshipId: "r1",
          parentGuardianId: PARENT_ID,
          studentId: STUDENT_ID,
          status: RelationshipStatus.Confirmed,
        },
      ]);

      renderWithProviders(
        <>
          <AuthHarness user={PARENT_USER} />
          <BookSessionPage />
        </>,
        { initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`] },
      );

      await advanceToReview();

      expect(await screen.findByLabelText("Choose your child")).toBeInTheDocument();
      expect(screen.queryByLabelText("Student id")).not.toBeInTheDocument();
    });

    it("a signed-in Parent/Guardian with no confirmed child sees an 'Add a child' prompt, and Continue is disabled", async () => {
      mockTutorAndSlots();
      vi.spyOn(identityService, "fetchRelationshipsForAccount").mockResolvedValue([]);

      renderWithProviders(
        <>
          <AuthHarness user={PARENT_USER} />
          <BookSessionPage />
        </>,
        { initialEntries: [`/scheduling/sessions/book?tutorId=${TUTOR_ID}`] },
      );

      await advanceToReview();

      expect(await screen.findByRole("heading", { name: "Add a child first" })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Continue" })).toBeDisabled();
    });
  });
});
