import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
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
    expect(screen.getByText(/Speaks English/)).toBeInTheDocument();
    expect(screen.getByText("50,000 Toman/hr")).toBeInTheDocument();
    expect(screen.getByText("30 minutes")).toBeInTheDocument();
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
    expect(screen.getByRole("link", { name: "Subjects" })).toHaveAttribute("href", "#subjects");
    expect(screen.getByRole("link", { name: "Teaching Info" })).toHaveAttribute(
      "href",
      "#teaching-information",
    );
    expect(screen.getByRole("link", { name: "Availability" })).toHaveAttribute(
      "href",
      "#availability",
    );
    expect(screen.getByRole("link", { name: "Reviews" })).toHaveAttribute("href", "#reviews");
  });

  describe("Availability preview", () => {
    it("shows an honest 'no open times' message when the Tutor has no open Availability Slots", async () => {
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);
      vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([]);

      renderPage();

      expect(await screen.findByText("No open times right now — check back later.")).toBeInTheDocument();
    });

    it("shows the Tutor's next open Availability Slots, excluding consumed ones", async () => {
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

      expect(await screen.findByRole("button", { name: /min · Online/ })).toBeInTheDocument();
      expect(screen.getAllByRole("button", { name: /min · Online/ })).toHaveLength(1);
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
    expect(screen.getByText("5 Toman/hr")).toBeInTheDocument();
  });

  it("shows a professional not-found panel with a Back to search action when the lookup fails", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockRejectedValue(new Error("Not found"));

    renderPage();

    expect(await screen.findByRole("heading", { name: "Tutor not found" })).toBeInTheDocument();
    expect(screen.getByText("Not found")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Back to search/ })).toHaveAttribute(
      "href",
      "/discovery/tutors/search",
    );
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
  });
});
