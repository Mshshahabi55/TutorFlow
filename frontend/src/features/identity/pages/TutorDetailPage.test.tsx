import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { TutorDetailPage } from "@/features/identity/pages/TutorDetailPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";
import * as schedulingService from "@/features/scheduling/api/schedulingService";

const TUTOR_ID = "11111111-1111-1111-1111-111111111111";

function renderPage() {
  return renderWithProviders(<TutorDetailPage />, {
    initialEntries: [`/identity/tutors/${TUTOR_ID}`],
    routePath: "/identity/tutors/:tutorId",
  });
}

const TUTOR = {
  tutorId: TUTOR_ID,
  isApproved: true,
  isSuspended: false,
  isDiscoverable: true,
  hourlyRate: 500_000,
  subject: "Mathematics",
  language: "English",
  location: "Remote",
  offeredDurations: ["00:30:00"],
};

describe("TutorDetailPage", () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([]);
  });

  it("shows a profile skeleton while loading", () => {
    vi.spyOn(identityService, "fetchTutorById").mockReturnValue(new Promise(() => {}));

    renderPage();

    expect(screen.getByTestId("tutor-profile-skeleton")).toBeInTheDocument();
  });

  it("shows the Tutor's real subject, language, location, rate, and durations to any viewer", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

    renderPage();

    expect(await screen.findByRole("heading", { name: "Mathematics" })).toBeInTheDocument();
    expect(screen.getByText("Remote")).toBeInTheDocument();
    expect(screen.getByText("English")).toBeInTheDocument();
    expect(screen.getByText("30 minutes")).toBeInTheDocument();
    // The Hero, the fixed mobile booking bar (Phase 4 PART 6), and the
    // sticky booking card (enriched in Phase 9) all show the real rate —
    // CSS/breakpoints control which is visible at a given viewport, jsdom
    // renders every node regardless, so there are genuinely three matches.
    expect(screen.getAllByText("50,000 Toman/hr")).toHaveLength(3);
  });

  // Phase 3 Step 3: the raw StatusPills (Approved/Suspended/Discoverable)
  // now render only for Admin/Tutor-own viewers (see the AdminStaff test
  // below) — every other viewer, including one with no role selected, sees
  // only the friendly "Verified" badge derived from the same isApproved
  // field, never the internal moderation-queue wording.
  it("shows a Verified badge instead of raw moderation state, for an approved Tutor", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

    renderPage();

    expect(await screen.findByText("Verified")).toBeInTheDocument();
    expect(screen.queryByText("Approved")).not.toBeInTheDocument();
    expect(screen.queryByText("Discoverable")).not.toBeInTheDocument();
  });

  it("shows no Verified badge for a Tutor that is not yet approved", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue({ ...TUTOR, isApproved: false });

    renderPage();

    await screen.findByRole("heading", { name: "Mathematics" });
    expect(screen.queryByText("Verified")).not.toBeInTheDocument();
  });

  it("shows a professional Reviews placeholder, since no review capability exists", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

    renderPage();

    expect(await screen.findByText("No reviews yet")).toBeInTheDocument();
  });

  it("links its Book Lesson action(s) to the existing booking route", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

    renderPage();

    const bookLinks = await screen.findAllByRole("link", { name: "Book Lesson" });
    expect(bookLinks.length).toBeGreaterThan(0);
    for (const link of bookLinks) {
      expect(link).toHaveAttribute("href", `/scheduling/sessions/book?tutorId=${TUTOR_ID}`);
    }
  });

  it("shows a section nav that jumps to each section by id", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

    renderPage();

    await screen.findByRole("heading", { name: "Mathematics" });
    expect(screen.getByRole("link", { name: "About" })).toHaveAttribute("href", "#about");
    expect(screen.getByRole("link", { name: "Learning Plans" })).toHaveAttribute(
      "href",
      "#learning-plans",
    );
    expect(screen.getByRole("link", { name: "Availability" })).toHaveAttribute(
      "href",
      "#availability",
    );
    expect(screen.getByRole("link", { name: "Reviews" })).toHaveAttribute("href", "#reviews");
    expect(screen.getByRole("link", { name: "FAQ" })).toHaveAttribute("href", "#faq");
    expect(screen.getByRole("link", { name: "Similar tutors" })).toHaveAttribute("href", "#similar-tutors");
  });

  it("shows an honest 'no learning plans yet' state, never a fabricated plan", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

    renderPage();

    expect(await screen.findByRole("heading", { name: "No learning plans yet" })).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: /Book a single lesson instead/ }),
    ).toHaveAttribute("href", `/scheduling/sessions/book?tutorId=${TUTOR_ID}`);
  });

  it("shows an honest FAQ placeholder, never fabricated Q&A", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

    renderPage();

    expect(await screen.findByText("No frequently asked questions yet")).toBeInTheDocument();
  });

  it("shows an honest Certificates & Experience placeholder when a Tutor hasn't filled that section in", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

    renderPage();

    expect(await screen.findByText("No certificates or experience added yet")).toBeInTheDocument();
  });

  it("shows real Certificates & Experience content once a Tutor has added it (Phase 9, ADR-024 fields)", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue({
      ...TUTOR,
      yearsOfExperience: 5,
      education: "BSc Mathematics",
      certifications: "TEFL",
    });

    renderPage();

    expect(await screen.findByText("5 years of teaching experience")).toBeInTheDocument();
    expect(screen.getByText("BSc Mathematics")).toBeInTheDocument();
    expect(screen.getByText("TEFL")).toBeInTheDocument();
    expect(screen.queryByText("No certificates or experience added yet")).not.toBeInTheDocument();
  });

  it("shows a real photo, display name, and headline once a Tutor has added them (Phase 9, ADR-024 fields)", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue({
      ...TUTOR,
      displayName: "Jane Doe",
      headline: "Friendly Math Tutor",
      photoUrl: "https://example.com/jane.jpg",
    });

    renderPage();

    expect(await screen.findByRole("heading", { name: "Jane Doe" })).toBeInTheDocument();
    expect(screen.getByText("Friendly Math Tutor")).toBeInTheDocument();
  });

  it("shows trial-lesson info on the sticky booking card only when the Tutor offers one", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue({
      ...TUTOR,
      trialLessonAvailable: true,
      trialLessonPrice: 100_000,
    });

    renderPage();

    expect(await screen.findByText("Trial lesson — 10,000 Toman")).toBeInTheDocument();
  });

  describe("Availability preview", () => {
    it("shows a read-only calendar with no day marked available when the Tutor has no open Availability Slots", async () => {
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);
      vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([]);

      renderPage();

      expect(await screen.findByText("Has open teaching times")).toBeInTheDocument();
      expect(screen.queryByLabelText(/— available/)).not.toBeInTheDocument();
    });

    it("marks a day with an open Availability Slot as available on the calendar, excluding consumed ones, with no click-to-book interaction", async () => {
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);
      vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([
        {
          availabilitySlotId: "22222222-2222-2222-2222-222222222222",
          tutorId: TUTOR_ID,
          startTimeUtc: "2026-08-01T14:00:00Z",
          endTimeUtc: "2026-08-01T15:00:00Z",
          duration: "01:00:00",
          deliveryMode: 0,
          isConsumed: false,
        },
        {
          availabilitySlotId: "33333333-3333-3333-3333-333333333333",
          tutorId: TUTOR_ID,
          startTimeUtc: "2026-08-02T14:00:00Z",
          endTimeUtc: "2026-08-02T15:00:00Z",
          duration: "01:00:00",
          deliveryMode: 0,
          isConsumed: true,
        },
      ]);

      renderPage();

      expect(await screen.findByLabelText(/Aug 01 — available/)).toBeInTheDocument();
      expect(screen.getByLabelText(/Aug 02 — no availability/)).toBeInTheDocument();
      // No booking logic inside the profile — the calendar's day cells are
      // purely informational, never a button/link into the wizard.
      expect(screen.queryAllByRole("button", { name: /Aug 01/ })).toHaveLength(0);
      // The Hero's trust row and the sticky booking card both surface the
      // same real availability data as a "Next available" fact (Phase 9).
      expect(screen.getAllByText(/Next available/)).toHaveLength(2);
    });
  });

  // Phase 4.5: Domain now rejects a new HourlyRate not divisible by 10, but
  // this page must still render a legacy row written before that
  // invariant existed without crashing — formatToman rounds instead of
  // throwing (frontend/src/shared/money/rial.ts).
  it("renders without throwing for a legacy hourly rate not divisible by 10", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue({ ...TUTOR, hourlyRate: 45 });

    renderPage();

    expect(await screen.findByRole("heading", { name: "Mathematics" })).toBeInTheDocument();
    // 45 Rial rounds to 5 Toman for display (4.5 rounds up) rather than throwing.
    // Rendered three times (Hero + fixed mobile booking bar + the sticky
    // booking card's own price, enriched in Phase 9) — see the note above.
    expect(screen.getAllByText("5 Toman/hr")).toHaveLength(3);
  });

  it("shows a friendly not-found panel with a Find Tutors action when the lookup fails, never the raw backend error", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockRejectedValue(new Error("404 Not Found"));

    renderPage();

    expect(
      await screen.findByRole("heading", { name: "Tutor unavailable" }),
    ).toBeInTheDocument();
    expect(screen.queryByText("404 Not Found")).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Find another tutor/ })).toHaveAttribute(
      "href",
      "/discovery/tutors/search",
    );
  });

  it("retries the Tutor lookup from the not-found panel's Try again action", async () => {
    const fetchTutorById = vi
      .spyOn(identityService, "fetchTutorById")
      .mockRejectedValueOnce(new Error("Network error"))
      .mockResolvedValueOnce(TUTOR);

    renderPage();

    await screen.findByRole("heading", { name: "Tutor unavailable" });
    await userEvent.click(screen.getByRole("button", { name: "Try again" }));

    expect(await screen.findByRole("heading", { name: "Mathematics" })).toBeInTheDocument();
    expect(fetchTutorById).toHaveBeenCalledTimes(2);
  });

  // Phase 4.9 Task 4: Edit offering and Approve/Suspend previously rendered
  // unconditionally for every viewer — the live-browser finding ("a Student
  // sees tutor Approve/Suspend, admin actions") this phase's brief reported.
  describe("role-gated actions", () => {
    it("shows Admin approve/suspend actions and raw moderation state only for the AdminStaff role", async () => {
      window.localStorage.setItem("tutorflow.devActorRole", "AdminStaff");
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue({
        ...TUTOR,
        isApproved: false,
        isDiscoverable: false,
      });

      renderPage();

      expect(await screen.findByText("Pending approval")).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Approve" })).toBeEnabled();
      expect(screen.getByRole("button", { name: "Suspend" })).toBeEnabled();
      expect(screen.queryByRole("link", { name: "Edit offering" })).not.toBeInTheDocument();
    });

    it("shows Edit offering and raw moderation state only for the Tutor role", async () => {
      window.localStorage.setItem("tutorflow.devActorRole", "Tutor");
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

      renderPage();

      expect(await screen.findByRole("link", { name: "Edit offering" })).toHaveAttribute(
        "href",
        `/identity/tutors/${TUTOR_ID}/edit`,
      );
      expect(screen.getByText("Approved")).toBeInTheDocument();
      expect(screen.queryByRole("button", { name: "Approve" })).not.toBeInTheDocument();
    });

    it("shows the Tutor's own Profile Completion checklist only for the Tutor role", async () => {
      window.localStorage.setItem("tutorflow.devActorRole", "Tutor");
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

      renderPage();

      expect(await screen.findByRole("heading", { name: "Profile Completion" })).toBeInTheDocument();
    });

    it("hides the Profile Completion checklist for an AdminStaff viewer", async () => {
      window.localStorage.setItem("tutorflow.devActorRole", "AdminStaff");
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

      renderPage();

      await screen.findByText("Approved");
      expect(screen.queryByRole("heading", { name: "Profile Completion" })).not.toBeInTheDocument();
    });

    it("shows no management action or raw moderation state for a Student viewer", async () => {
      window.localStorage.setItem("tutorflow.devActorRole", "Student");
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

      renderPage();

      expect(await screen.findByText("Verified")).toBeInTheDocument();
      expect(screen.queryByText("Approved")).not.toBeInTheDocument();
      expect(screen.queryByRole("link", { name: "Edit offering" })).not.toBeInTheDocument();
      expect(screen.queryByRole("button", { name: "Approve" })).not.toBeInTheDocument();
    });

    it("shows no management action or raw moderation state when no role is selected", async () => {
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

      renderPage();

      expect(await screen.findByText("Verified")).toBeInTheDocument();
      expect(screen.queryByText("Approved")).not.toBeInTheDocument();
      expect(screen.queryByRole("link", { name: "Edit offering" })).not.toBeInTheDocument();
      expect(screen.queryByRole("button", { name: "Approve" })).not.toBeInTheDocument();
    });

    // RC5.1: real messaging (docs/adr/ADR-022-...) — only a Student or
    // Parent/Guardian may start a new Conversation with a Tutor
    // (StartConversationCommandHandler's own restriction).
    it("shows Send Message only for the Student and ParentGuardian roles", async () => {
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

      window.localStorage.setItem("tutorflow.devActorRole", "Student");
      renderPage();
      expect(await screen.findByRole("button", { name: "Send Message" })).toBeInTheDocument();
    });

    it("shows Send Message for the ParentGuardian role", async () => {
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);
      window.localStorage.setItem("tutorflow.devActorRole", "ParentGuardian");

      renderPage();

      expect(await screen.findByRole("button", { name: "Send Message" })).toBeInTheDocument();
    });

    it("hides Send Message for the Tutor role", async () => {
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);
      window.localStorage.setItem("tutorflow.devActorRole", "Tutor");

      renderPage();

      await screen.findByText("Approved");
      expect(screen.queryByRole("button", { name: "Send Message" })).not.toBeInTheDocument();
    });

    it("hides Send Message for the AdminStaff role", async () => {
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);
      window.localStorage.setItem("tutorflow.devActorRole", "AdminStaff");

      renderPage();

      await screen.findByText("Approved");
      expect(screen.queryByRole("button", { name: "Send Message" })).not.toBeInTheDocument();
    });
  });
});
