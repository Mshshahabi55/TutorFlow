import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { TutorDetailPage } from "@/features/identity/pages/TutorDetailPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";

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
  });

  it("shows the Tutor's status pills and offering fields", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

    renderPage();

    expect(await screen.findByText("Approved")).toBeInTheDocument();
    expect(screen.getByText("Discoverable")).toBeInTheDocument();
    expect(screen.getByText(/Mathematics/)).toBeInTheDocument();
    // 500,000 Rial (the wire value) displays as 50,000 Toman.
    expect(screen.getByText("50,000 Toman")).toBeInTheDocument();
  });

  // Phase 4.5: Domain now rejects a new HourlyRate not divisible by 10, but
  // this page must still render a legacy row written before that
  // invariant existed without crashing — formatToman rounds instead of
  // throwing (frontend/src/shared/money/rial.ts).
  it("renders without throwing for a legacy hourly rate not divisible by 10", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue({ ...TUTOR, hourlyRate: 45 });

    renderPage();

    expect(await screen.findByText("Approved")).toBeInTheDocument();
    // 45 Rial rounds to 5 Toman for display (4.5 rounds up) rather than
    // throwing.
    expect(screen.getByText("5 Toman")).toBeInTheDocument();
  });

  it("shows an error state when the lookup fails", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockRejectedValue(new Error("Not found"));

    renderPage();

    expect(await screen.findByText("Not found")).toBeInTheDocument();
  });

  // Phase 4.9 Task 4: Edit offering and Approve/Suspend previously rendered
  // unconditionally for every viewer — the live-browser finding ("a Student
  // sees tutor Approve/Suspend, admin actions") this phase's brief reported.
  // The pre-existing "shows Admin approve/suspend actions" assertion (no
  // role selected) asserted exactly that old, now-wrong behavior; inverted
  // here rather than silently dropped (Phase 1B precedent: an explained
  // test change, not a loosened one).
  describe("role-gated actions", () => {
    it("shows Admin approve/suspend actions only for the AdminStaff role", async () => {
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

    it("shows Edit offering only for the Tutor role", async () => {
      window.localStorage.setItem("tutorflow.devActorRole", "Tutor");
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

      renderPage();

      expect(await screen.findByRole("link", { name: "Edit offering" })).toHaveAttribute(
        "href",
        `/identity/tutors/${TUTOR_ID}/edit`,
      );
      expect(screen.queryByRole("button", { name: "Approve" })).not.toBeInTheDocument();
    });

    it("shows no action for a Student viewer", async () => {
      window.localStorage.setItem("tutorflow.devActorRole", "Student");
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

      renderPage();

      expect(await screen.findByText("Approved")).toBeInTheDocument();
      expect(screen.queryByRole("link", { name: "Edit offering" })).not.toBeInTheDocument();
      expect(screen.queryByRole("button", { name: "Approve" })).not.toBeInTheDocument();
    });

    it("shows no action when no role is selected", async () => {
      vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);

      renderPage();

      expect(await screen.findByText("Approved")).toBeInTheDocument();
      expect(screen.queryByRole("link", { name: "Edit offering" })).not.toBeInTheDocument();
      expect(screen.queryByRole("button", { name: "Approve" })).not.toBeInTheDocument();
    });
  });
});
