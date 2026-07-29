import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { TutorProfileHero } from "@/features/identity/components/TutorProfileHero";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import type { TutorDto } from "@/services/api/dtos";

const BASE_TUTOR: TutorDto = {
  tutorId: "11111111-1111-1111-1111-111111111111",
  isApproved: false,
  isSuspended: false,
  isDiscoverable: true,
  hourlyRate: 500_000,
  subject: "Mathematics",
  language: "English",
  location: "Remote",
  offeredDurations: [],
};

describe("TutorProfileHero", () => {
  it("falls back to subject as the heading when no displayName is set", async () => {
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([]);

    renderWithProviders(<TutorProfileHero tutor={BASE_TUTOR} />);

    expect(await screen.findByRole("heading", { name: "Mathematics" })).toBeInTheDocument();
  });

  it("shows the real displayName, headline, and photo when a Tutor has added them (Phase 9, ADR-024 fields)", async () => {
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([]);

    renderWithProviders(
      <TutorProfileHero
        tutor={{
          ...BASE_TUTOR,
          displayName: "Jane Doe",
          headline: "Friendly Math Tutor",
          photoUrl: "https://example.com/jane.jpg",
        }}
      />,
    );

    expect(await screen.findByRole("heading", { name: "Jane Doe" })).toBeInTheDocument();
    expect(screen.getByText("Friendly Math Tutor")).toBeInTheDocument();
    expect(screen.getByRole("img")).toHaveAttribute("src", "https://example.com/jane.jpg");
  });
});
