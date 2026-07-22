import { describe, expect, it, vi } from "vitest";
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

describe("TutorDetailPage", () => {
  it("shows the Tutor's status pills and offering fields", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue({
      tutorId: TUTOR_ID,
      isApproved: true,
      isSuspended: false,
      isDiscoverable: true,
      hourlyRate: 500_000,
      subject: "Mathematics",
      language: "English",
      location: "Remote",
      offeredDurations: ["00:30:00"],
    });

    renderPage();

    expect(await screen.findByText("Approved")).toBeInTheDocument();
    expect(screen.getByText("Discoverable")).toBeInTheDocument();
    expect(screen.getByText(/Mathematics/)).toBeInTheDocument();
    // 500,000 Rial (the wire value) displays as 50,000 Toman.
    expect(screen.getByText("50,000 Toman")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Edit offering" })).toHaveAttribute(
      "href",
      `/identity/tutors/${TUTOR_ID}/edit`,
    );
  });

  // Phase 4.5: Domain now rejects a new HourlyRate not divisible by 10, but
  // this page must still render a legacy row written before that
  // invariant existed without crashing — formatToman rounds instead of
  // throwing (frontend/src/shared/money/rial.ts).
  it("renders without throwing for a legacy hourly rate not divisible by 10", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue({
      tutorId: TUTOR_ID,
      isApproved: true,
      isSuspended: false,
      isDiscoverable: true,
      hourlyRate: 45,
      subject: "Mathematics",
      language: "English",
      location: "Remote",
      offeredDurations: ["00:30:00"],
    });

    renderPage();

    expect(await screen.findByText("Approved")).toBeInTheDocument();
    // 45 Rial rounds to 5 Toman for display (4.5 rounds up) rather than
    // throwing.
    expect(screen.getByText("5 Toman")).toBeInTheDocument();
  });

  it("shows Admin approve/suspend actions", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue({
      tutorId: TUTOR_ID,
      isApproved: false,
      isSuspended: false,
      isDiscoverable: false,
      hourlyRate: null,
      subject: null,
      language: null,
      location: null,
      offeredDurations: [],
    });

    renderPage();

    expect(await screen.findByText("Pending approval")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Approve" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "Suspend" })).toBeEnabled();
  });

  it("shows an error state when the lookup fails", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockRejectedValue(new Error("Not found"));

    renderPage();

    expect(await screen.findByText("Not found")).toBeInTheDocument();
  });
});
