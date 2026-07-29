import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { AdminDashboardPage } from "@/features/oversight/pages/AdminDashboardPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";
import * as oversightService from "@/features/oversight/api/oversightService";
import * as healthService from "@/services/api/healthService";
import * as communicationService from "@/features/communication/api/communicationService";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";

describe("AdminDashboardPage", () => {
  // RC5.1: every dashboard now renders RecentConversationsSection —
  // mocked once here since none of these tests are about messaging itself.
  beforeEach(() => {
    vi.spyOn(communicationService, "fetchMyConversations").mockResolvedValue([]);
  });

  it("shows the real counts from each existing capability", async () => {
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");
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
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");
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

  it("shows Quick actions without a self-referential link to the current page", () => {
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");
    vi.spyOn(identityService, "fetchPendingTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 1,
    });
    vi.spyOn(oversightService, "fetchAllSessions").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 1,
    });
    vi.spyOn(identityService, "fetchTutorDirectory").mockResolvedValue([]);

    renderWithProviders(<AdminDashboardPage />);

    expect(screen.getByRole("link", { name: /Pending Tutor approvals/ })).toHaveAttribute(
      "href",
      "/identity/tutors/pending",
    );
    expect(screen.getByRole("link", { name: /All sessions/ })).toHaveAttribute(
      "href",
      "/oversight/sessions",
    );
    expect(screen.queryByRole("link", { name: /^Admin dashboard$/ })).not.toBeInTheDocument();
  });

  it("shows Platform Health reusing the existing GET /health capability", async () => {
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");
    vi.spyOn(identityService, "fetchPendingTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 1,
    });
    vi.spyOn(oversightService, "fetchAllSessions").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 1,
    });
    vi.spyOn(identityService, "fetchTutorDirectory").mockResolvedValue([]);

    renderWithProviders(<AdminDashboardPage />);

    expect(await screen.findByRole("heading", { name: "Platform Health" })).toBeInTheDocument();
    expect(await screen.findByText("Healthy")).toBeInTheDocument();
    expect(screen.getByText("GET /health")).toBeInTheDocument();
  });

  it("shows a Recent Sessions preview with both Tutor and Student ids, sourced from the existing all-sessions capability", async () => {
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");
    vi.spyOn(identityService, "fetchPendingTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 1,
    });
    vi.spyOn(oversightService, "fetchAllSessions").mockResolvedValue({
      items: [
        {
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
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 5,
    });
    vi.spyOn(identityService, "fetchTutorDirectory").mockResolvedValue([]);

    renderWithProviders(<AdminDashboardPage />);

    expect(await screen.findByRole("heading", { name: "Recent Sessions" })).toBeInTheDocument();
    expect(await screen.findByText("Tutor: t1")).toBeInTheDocument();
    expect(screen.getByText("Student: st1")).toBeInTheDocument();
  });

  // RC5.1 Step 8: Admin's own support conversations, via the same
  // GET /conversations/mine every role uses — reused, not a bespoke
  // Admin-only messaging system.
  it("shows a Messages section for Admin support conversations", async () => {
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");
    vi.spyOn(identityService, "fetchPendingTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 1,
    });
    vi.spyOn(oversightService, "fetchAllSessions").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 1,
    });
    vi.spyOn(identityService, "fetchTutorDirectory").mockResolvedValue([]);

    renderWithProviders(<AdminDashboardPage />);

    expect(await screen.findByRole("heading", { name: "Messages" })).toBeInTheDocument();
    expect(await screen.findByText("No conversations yet")).toBeInTheDocument();
  });
});
