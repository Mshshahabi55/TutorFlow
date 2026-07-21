import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { AdminDashboardPage } from "@/features/oversight/pages/AdminDashboardPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";
import * as oversightService from "@/features/oversight/api/oversightService";

describe("AdminDashboardPage", () => {
  it("shows the real counts from each existing capability", async () => {
    vi.spyOn(identityService, "fetchPendingTutors").mockResolvedValue({
      items: [],
      totalCount: 3,
      page: 1,
      pageSize: 1,
    });
    vi.spyOn(oversightService, "fetchAllSessions").mockResolvedValue({
      items: [],
      totalCount: 12,
      page: 1,
      pageSize: 1,
    });
    vi.spyOn(identityService, "fetchTutorDirectory").mockResolvedValue([
      {
        tutorId: "t1",
        isApproved: true,
        isSuspended: false,
        isDiscoverable: true,
        hourlyRate: 40,
        subject: "Mathematics",
        language: "English",
        location: "Remote",
        offeredDurations: [],
      },
    ]);

    renderWithProviders(<AdminDashboardPage />);

    expect(await screen.findByText("3")).toBeInTheDocument();
    expect(screen.getByText("12")).toBeInTheDocument();
    expect(screen.getByText("1")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Review queue" })).toHaveAttribute(
      "href",
      "/identity/tutors/pending",
    );
    expect(screen.getByRole("link", { name: "View all sessions" })).toHaveAttribute(
      "href",
      "/oversight/sessions",
    );
  });

  it("shows an error state for a widget whose query fails, independently of the others", async () => {
    vi.spyOn(identityService, "fetchPendingTutors").mockRejectedValue(new Error("Network Error"));
    vi.spyOn(oversightService, "fetchAllSessions").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 1,
    });
    vi.spyOn(identityService, "fetchTutorDirectory").mockResolvedValue([]);

    renderWithProviders(<AdminDashboardPage />);

    expect(await screen.findByText("Network Error")).toBeInTheDocument();
  });
});
