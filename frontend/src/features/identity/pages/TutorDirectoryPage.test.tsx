import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { TutorDirectoryPage } from "@/features/identity/pages/TutorDirectoryPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";

describe("TutorDirectoryPage", () => {
  it("shows an empty state when there are no discoverable Tutors", async () => {
    vi.spyOn(identityService, "fetchTutorDirectory").mockResolvedValue([]);

    renderWithProviders(<TutorDirectoryPage />);

    expect(await screen.findByText("No discoverable Tutors yet")).toBeInTheDocument();
  });

  it("shows skeleton cards while loading, not an abrupt spinner", () => {
    vi.spyOn(identityService, "fetchTutorDirectory").mockReturnValue(new Promise(() => {}));

    renderWithProviders(<TutorDirectoryPage />);

    expect(screen.getAllByTestId("tutor-card-skeleton").length).toBeGreaterThan(0);
  });

  it("lists discoverable Tutors as marketplace cards, linking to the detail page", async () => {
    vi.spyOn(identityService, "fetchTutorDirectory").mockResolvedValue([
      {
        tutorId: "11111111-1111-1111-1111-111111111111",
        isApproved: true,
        isSuspended: false,
        isDiscoverable: true,
        hourlyRate: 500_000,
        subject: "Mathematics",
        language: "English",
        location: "Remote",
        offeredDurations: ["00:30:00"],
      },
    ]);

    renderWithProviders(<TutorDirectoryPage />);

    expect(await screen.findByRole("heading", { name: "Mathematics" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "View profile" })).toHaveAttribute(
      "href",
      "/identity/tutors/11111111-1111-1111-1111-111111111111",
    );
    expect(screen.getByRole("link", { name: "Book Lesson" })).toHaveAttribute(
      "href",
      "/scheduling/sessions/book?tutorId=11111111-1111-1111-1111-111111111111",
    );
  });

  it("shows a friendly error state with a retry action when the directory fails to load", async () => {
    vi.spyOn(identityService, "fetchTutorDirectory").mockRejectedValue(new Error("Network error"));

    renderWithProviders(<TutorDirectoryPage />);

    expect(await screen.findByRole("button", { name: "Try again" })).toBeInTheDocument();
  });
});
