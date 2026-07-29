import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { RecentlyViewedSection } from "@/features/discovery/components/RecentlyViewedSection";
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

describe("RecentlyViewedSection", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("renders nothing when nothing has been viewed yet", () => {
    const { container } = renderWithProviders(<RecentlyViewedSection />);

    expect(container).toBeEmptyDOMElement();
  });

  it("renders a link per recently viewed Tutor still available", async () => {
    window.localStorage.setItem("tutorflow.recentlyViewedTutorIds", JSON.stringify(["a"]));
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(tutor({ tutorId: "a", displayName: "Ada" }));

    renderWithProviders(<RecentlyViewedSection />);

    expect(await screen.findByText("Recently viewed")).toBeInTheDocument();
    expect(await screen.findByText("Ada")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Ada/ })).toHaveAttribute("href", "/identity/tutors/a");
  });
});
