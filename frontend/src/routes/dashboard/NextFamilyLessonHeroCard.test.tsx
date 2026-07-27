import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { NextFamilyLessonHeroCard } from "@/routes/dashboard/NextFamilyLessonHeroCard";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

const SESSION: SessionDto = {
  sessionId: "44444444-4444-4444-4444-444444444444",
  tutorId: "t1",
  studentId: "st1",
  parentGuardianId: null,
  availabilitySlotId: "a1",
  scheduledTimeUtc: "2026-08-01T14:00:00Z",
  endTimeUtc: "2026-08-01T15:00:00Z",
  duration: "01:00:00",
  deliveryMode: DeliveryMode.Online,
  status: SessionStatus.Scheduled,
};

const NOW = Date.parse("2026-08-01T12:30:00Z");

describe("NextFamilyLessonHeroCard", () => {
  it("shows the Tutor's subject, the Child's id, and a countdown", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue({
      tutorId: "t1",
      isApproved: true,
      isSuspended: false,
      isDiscoverable: true,
      hourlyRate: 500_000,
      subject: "Mathematics",
      language: "English",
      location: "Remote",
      offeredDurations: ["01:00:00"],
    });

    renderWithProviders(<NextFamilyLessonHeroCard session={SESSION} now={NOW} />);

    expect(await screen.findByText("Mathematics")).toBeInTheDocument();
    expect(screen.getByText(/Child: st1/)).toBeInTheDocument();
    expect(screen.getByText("in 1h 30min")).toBeInTheDocument();
  });

  it("falls back to a generic label while the Tutor hasn't resolved yet", () => {
    vi.spyOn(identityService, "fetchTutorById").mockReturnValue(new Promise(() => {}));

    renderWithProviders(<NextFamilyLessonHeroCard session={SESSION} now={NOW} />);

    expect(screen.getByText("Lesson")).toBeInTheDocument();
  });

  it("links View Lesson to the Session's real detail page", () => {
    vi.spyOn(identityService, "fetchTutorById").mockReturnValue(new Promise(() => {}));

    renderWithProviders(<NextFamilyLessonHeroCard session={SESSION} now={NOW} />);

    expect(screen.getByRole("link", { name: "View Lesson" })).toHaveAttribute(
      "href",
      "/scheduling/sessions/44444444-4444-4444-4444-444444444444",
    );
  });
});
