import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "@/test/renderWithProviders";
import { RecommendedTutors } from "@/routes/dashboard/RecommendedTutors";
import * as discoveryService from "@/features/discovery/api/discoveryService";

describe("RecommendedTutors", () => {
  it("shows skeleton placeholders while loading", () => {
    vi.spyOn(discoveryService, "searchTutors").mockReturnValue(new Promise(() => {}));

    renderWithProviders(<RecommendedTutors />);

    expect(screen.getAllByTestId("recommended-tutor-skeleton")).toHaveLength(4);
  });

  it("shows an empty state when no Tutors are returned", async () => {
    vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 4,
    });

    renderWithProviders(<RecommendedTutors />);

    expect(await screen.findByText("No tutors available yet")).toBeInTheDocument();
  });

  it("shows an error state when the search fails", async () => {
    vi.spyOn(discoveryService, "searchTutors").mockRejectedValue(new Error("Network Error"));

    renderWithProviders(<RecommendedTutors />);

    expect(await screen.findByText("Network Error")).toBeInTheDocument();
  });

  it("renders each returned Tutor as a card linking to their profile", async () => {
    vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [
        {
          tutorId: "11111111-1111-1111-1111-111111111111",
          isApproved: true,
          isSuspended: false,
          isDiscoverable: true,
          hourlyRate: 500_000,
          subject: "Mathematics",
          language: "English",
          location: "Remote",
          offeredDurations: [],
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 4,
    });

    renderWithProviders(<RecommendedTutors />);

    expect(await screen.findByText("Mathematics")).toBeInTheDocument();
    expect(screen.getByText("Speaks English")).toBeInTheDocument();
    expect(screen.getByText("50,000 Toman/hr")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "View profile" })).toHaveAttribute(
      "href",
      "/identity/tutors/11111111-1111-1111-1111-111111111111",
    );
  });
});
