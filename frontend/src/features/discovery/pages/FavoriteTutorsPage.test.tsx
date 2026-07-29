import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { FavoriteTutorsPage } from "@/features/discovery/pages/FavoriteTutorsPage";
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
    offeredDurations: [],
    ...overrides,
  };
}

describe("FavoriteTutorsPage", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("shows an empty state when nothing is favorited", () => {
    renderWithProviders(<FavoriteTutorsPage />);

    expect(screen.getByText("No favorites yet")).toBeInTheDocument();
  });

  it("renders a card for each favorited Tutor still available", async () => {
    window.localStorage.setItem("tutorflow.favoriteTutorIds", JSON.stringify(["a", "b"]));
    vi.spyOn(identityService, "fetchTutorById").mockImplementation((id: string) =>
      Promise.resolve(tutor({ tutorId: id, displayName: id === "a" ? "Ada" : "Bo" })),
    );

    renderWithProviders(<FavoriteTutorsPage />);

    expect(await screen.findByText("Ada")).toBeInTheDocument();
    expect(await screen.findByText("Bo")).toBeInTheDocument();
  });

  it("shows an unavailable message when every favorited Tutor has been removed", async () => {
    window.localStorage.setItem("tutorflow.favoriteTutorIds", JSON.stringify(["a"]));
    vi.spyOn(identityService, "fetchTutorById").mockRejectedValue(new Error("not found"));

    renderWithProviders(<FavoriteTutorsPage />);

    expect(await screen.findByText("Your favorited Tutors are no longer available")).toBeInTheDocument();
  });
});
