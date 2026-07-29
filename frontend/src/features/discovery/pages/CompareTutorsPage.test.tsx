import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { CompareTutorsPage } from "@/features/discovery/pages/CompareTutorsPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";
import type { TutorDto } from "@/services/api/dtos";

function tutor(overrides: Partial<TutorDto>): TutorDto {
  return {
    tutorId: "11111111-1111-1111-1111-111111111111",
    isApproved: true,
    isSuspended: false,
    isDiscoverable: true,
    hourlyRate: 500_000,
    subject: "Mathematics",
    language: "English",
    location: "Remote",
    offeredDurations: ["01:00:00"],
    ...overrides,
  };
}

function renderPage(ids: string) {
  return renderWithProviders(<CompareTutorsPage />, { initialEntries: [`/discovery/tutors/compare?ids=${ids}`] });
}

describe("CompareTutorsPage", () => {
  it("prompts to select at least two Tutors when fewer than two ids are given", () => {
    renderPage("a");

    expect(screen.getByText("Select at least two Tutors to compare")).toBeInTheDocument();
  });

  it("renders a comparison table with a column per Tutor once loaded", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockImplementation((id: string) =>
      Promise.resolve(tutor({ tutorId: id, displayName: id === "a" ? "Ada" : "Bo", subject: id === "a" ? "Math" : "Physics" })),
    );

    renderPage("a,b");

    expect(await screen.findByText("Ada")).toBeInTheDocument();
    expect(screen.getByText("Bo")).toBeInTheDocument();
    expect(screen.getByText("Math")).toBeInTheDocument();
    expect(screen.getByText("Physics")).toBeInTheDocument();
    expect(screen.getByText("Hourly rate")).toBeInTheDocument();
  });

  it("shows an unavailable message when fewer than two selected Tutors still exist", async () => {
    vi.spyOn(identityService, "fetchTutorById").mockRejectedValue(new Error("not found"));

    renderPage("a,b");

    expect(await screen.findByText("Not enough of these Tutors are still available")).toBeInTheDocument();
  });
});
