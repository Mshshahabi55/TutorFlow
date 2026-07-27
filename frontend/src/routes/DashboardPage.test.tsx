import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { DashboardPage } from "@/routes/DashboardPage";
import { ActorProvider } from "@/shared/context/ActorProvider";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { ConfirmDialogProvider } from "@/shared/context/ConfirmDialogProvider";
import * as healthService from "@/services/api/healthService";
import * as discoveryService from "@/features/discovery/api/discoveryService";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import * as identityService from "@/features/identity/api/identityService";
import { DeliveryMode, RelationshipStatus, SessionStatus } from "@/services/api/dtos";

function renderDashboard() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <ActorProvider>
        <NotificationProvider>
          <ConfirmDialogProvider>
            <MemoryRouter>
              <DashboardPage />
            </MemoryRouter>
          </ConfirmDialogProvider>
        </NotificationProvider>
      </ActorProvider>
    </QueryClientProvider>,
  );
}

describe("DashboardPage", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("shows a loading state, then the health status once resolved", async () => {
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");

    renderDashboard();

    expect(screen.getByRole("status")).toBeInTheDocument();
    expect(await screen.findByText("Healthy")).toBeInTheDocument();
  });

  it("shows an error state when the health check fails", async () => {
    vi.spyOn(healthService, "fetchHealthStatus").mockRejectedValue(new Error("Network Error"));

    renderDashboard();

    expect(await screen.findByText("Network Error")).toBeInTheDocument();
  });

  // Phase D2 Task 4: replaces the old "shows every available module" test.
  // The static, non-interactive "Available modules" pill list is gone
  // (Task 1 audit's clutter finding — four pills naming bounded contexts,
  // no link, no distinction); with no role known, there is nothing yet to
  // recommend a next action for, so neither the "Quick actions" heading
  // nor any of its links should render — only the role-selection prompt.
  it("shows the role-selection prompt and no quick actions, with no role selected", () => {
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");

    renderDashboard();

    expect(screen.getByText(/select a role/i)).toBeInTheDocument();
    expect(screen.queryByText("Quick actions")).not.toBeInTheDocument();
  });

  // Phase 3 Step 6: the Tutor role now gets its own workspace dashboard
  // (`TutorDashboard`, asserted in its own describe block below) — this
  // generic-dashboard behavior is only still exercised by roles that keep
  // using it (AdminStaff, ParentGuardian).
  it("shows a role-specific summary once a role is selected", async () => {
    window.localStorage.setItem("tutorflow.devActorRole", "AdminStaff");
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");

    renderDashboard();

    expect(await screen.findByText(/pending tutor applications/i)).toBeInTheDocument();
  });

  it("shows a different quick-action set for the Student role", () => {
    window.localStorage.setItem("tutorflow.devActorRole", "Student");
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");
    vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 4,
    });

    renderDashboard();

    expect(screen.getByRole("link", { name: /Search Tutors/ })).toHaveAttribute(
      "href",
      "/discovery/tutors/search",
    );
    expect(screen.queryByRole("link", { name: /Declare availability/ })).not.toBeInTheDocument();
  });
});

// Phase 3 Step 1: the Student role now gets its own marketplace-style home
// (`StudentDashboard`) instead of the generic role-summary dashboard every
// other role still sees — that generic path (asserted above) is unchanged.
describe("DashboardPage — Student dashboard", () => {
  beforeEach(() => {
    window.localStorage.clear();
    window.localStorage.setItem("tutorflow.devActorRole", "Student");
  });

  it("shows a Welcome heading and every required section", async () => {
    vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 4,
    });

    renderDashboard();

    expect(screen.getByRole("heading", { name: "Welcome back" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Upcoming Sessions" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Continue Learning" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Recommended Tutors" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Recent Activity" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Quick actions" })).toBeInTheDocument();

    expect(screen.getByText("No upcoming sessions yet")).toBeInTheDocument();
    expect(screen.getByText("Nothing in progress yet")).toBeInTheDocument();
    expect(screen.getByText("No recent activity yet")).toBeInTheDocument();
    expect(await screen.findByText("No tutors available yet")).toBeInTheDocument();
  });

  it("offers a Book a session action from the empty Upcoming Sessions section", () => {
    vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 4,
    });

    renderDashboard();

    const upcomingSessions = screen.getByRole("region", { name: "Upcoming Sessions" });
    expect(within(upcomingSessions).getByRole("link", { name: /Book a session/ })).toHaveAttribute(
      "href",
      "/scheduling/sessions/book",
    );
  });

  it("shows Recommended Tutors sourced from the existing Discovery search capability", async () => {
    vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [
        {
          tutorId: "22222222-2222-2222-2222-222222222222",
          isApproved: true,
          isSuspended: false,
          isDiscoverable: true,
          hourlyRate: 300_000,
          subject: "Physics",
          language: "Persian",
          location: "Tehran",
          offeredDurations: [],
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 4,
    });

    renderDashboard();

    expect(await screen.findByText("Physics")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "View profile" })).toHaveAttribute(
      "href",
      "/identity/tutors/22222222-2222-2222-2222-222222222222",
    );
  });
});

// Phase 3 Step 6: the Tutor role now gets its own workspace dashboard
// instead of the generic role-summary dashboard every other remaining role
// still sees (asserted above).
describe("DashboardPage — Tutor dashboard", () => {
  const TUTOR_ID = "11111111-1111-1111-1111-111111111111";

  beforeEach(() => {
    window.localStorage.clear();
    window.localStorage.setItem("tutorflow.devActorRole", "Tutor");
  });

  it("shows a Welcome heading and Quick actions with no Tutor id entered yet", () => {
    renderDashboard();

    expect(screen.getByRole("heading", { name: "Welcome back" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Declare availability/ })).toHaveAttribute(
      "href",
      "/scheduling/availability/declare",
    );
    expect(screen.getByRole("link", { name: /My sessions/ })).toHaveAttribute(
      "href",
      "/scheduling/tutors",
    );
    expect(screen.getByLabelText("Tutor id")).toBeInTheDocument();
  });

  it("shows the teaching overview, grouped by Today/Upcoming/Recent Activity, once a Tutor id is entered", async () => {
    const today = new Date();
    const todaySession = {
      sessionId: "44444444-4444-4444-4444-444444444444",
      tutorId: TUTOR_ID,
      studentId: "st1",
      parentGuardianId: null,
      availabilitySlotId: "a1",
      scheduledTimeUtc: today.toISOString(),
      endTimeUtc: today.toISOString(),
      duration: "01:00:00",
      deliveryMode: DeliveryMode.Online,
      status: SessionStatus.Scheduled,
    };
    const completedSession = {
      ...todaySession,
      sessionId: "55555555-5555-5555-5555-555555555555",
      scheduledTimeUtc: "2020-01-01T10:00:00Z",
      status: SessionStatus.Completed,
    };
    vi.spyOn(schedulingService, "fetchTutorSchedule").mockResolvedValue([todaySession, completedSession]);
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([]);

    renderDashboard();

    await userEvent.type(screen.getByLabelText("Tutor id"), TUTOR_ID);
    await userEvent.click(screen.getByRole("button", { name: "Look up" }));

    expect(await screen.findByRole("heading", { name: "Today's Sessions" })).toBeInTheDocument();
    expect(screen.getAllByText("Student: st1")).toHaveLength(2);
    expect(await screen.findByRole("heading", { name: "Recent Activity" })).toBeInTheDocument();
  });

  it("shows an Availability Summary sourced from the existing Availability Slot capability", async () => {
    vi.spyOn(schedulingService, "fetchTutorSchedule").mockResolvedValue([]);
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue([
      {
        availabilitySlotId: "22222222-2222-2222-2222-222222222222",
        tutorId: TUTOR_ID,
        startTimeUtc: "2026-08-01T14:00:00Z",
        endTimeUtc: "2026-08-01T15:00:00Z",
        duration: "01:00:00",
        deliveryMode: DeliveryMode.Online,
        isConsumed: false,
      },
    ]);

    renderDashboard();

    await userEvent.type(screen.getByLabelText("Tutor id"), TUTOR_ID);
    await userEvent.click(screen.getByRole("button", { name: "Look up" }));

    expect(await screen.findByRole("heading", { name: "Availability Summary" })).toBeInTheDocument();
    expect(screen.getByText("Open")).toBeInTheDocument();
  });
});

// Phase 3 Step 7: the ParentGuardian role now gets its own family
// workspace dashboard instead of the generic role-summary dashboard
// AdminStaff still sees (asserted above).
describe("DashboardPage — Parent dashboard", () => {
  const ACCOUNT_ID = "11111111-1111-1111-1111-111111111111";

  beforeEach(() => {
    window.localStorage.clear();
    window.localStorage.setItem("tutorflow.devActorRole", "ParentGuardian");
  });

  it("shows a Welcome heading and Quick actions with no Parent/Guardian id entered yet", () => {
    renderDashboard();

    expect(screen.getByRole("heading", { name: "Welcome back" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Relationships/ })).toHaveAttribute(
      "href",
      "/identity/relationships",
    );
    expect(screen.getByRole("link", { name: /Book a session/ })).toHaveAttribute(
      "href",
      "/scheduling/sessions/book",
    );
    expect(screen.getByLabelText("Parent/Guardian id")).toBeInTheDocument();
  });

  it("shows a professional empty state guiding toward inviting a Relationship, when there are no children linked", async () => {
    vi.spyOn(identityService, "fetchRelationshipsForAccount").mockResolvedValue([]);

    renderDashboard();

    await userEvent.type(screen.getByLabelText("Parent/Guardian id"), ACCOUNT_ID);
    await userEvent.click(screen.getByRole("button", { name: "Look up" }));

    expect(await screen.findByText("No children linked yet")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Invite a Relationship/ })).toHaveAttribute(
      "href",
      "/identity/relationships",
    );
  });

  it("shows each child as a Children Overview card, and the single confirmed child's Upcoming Sessions", async () => {
    vi.spyOn(identityService, "fetchRelationshipsForAccount").mockResolvedValue([
      {
        relationshipId: "r1",
        parentGuardianId: ACCOUNT_ID,
        studentId: "st1",
        status: RelationshipStatus.Confirmed,
      },
      {
        relationshipId: "r2",
        parentGuardianId: ACCOUNT_ID,
        studentId: "st2",
        status: RelationshipStatus.Invited,
      },
    ]);
    vi.spyOn(schedulingService, "fetchStudentSchedule").mockResolvedValue([]);

    renderDashboard();

    await userEvent.type(screen.getByLabelText("Parent/Guardian id"), ACCOUNT_ID);
    await userEvent.click(screen.getByRole("button", { name: "Look up" }));

    // "st1" only ever appears once the relationships query resolves — never
    // during its loading skeleton — so waiting for it first avoids a race
    // where findByRole resolves against the loading state's own heading
    // (rendered with the same title) just before it's replaced.
    expect(await screen.findByText("st1")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Children Overview" })).toBeInTheDocument();
    expect(screen.getByText("st2")).toBeInTheDocument();
    expect(screen.getByText("Confirmed")).toBeInTheDocument();
    expect(screen.getByText("Invited")).toBeInTheDocument();
    expect(await screen.findByText("No upcoming sessions yet")).toBeInTheDocument();
  });

  it("does not aggregate schedules across multiple confirmed children, to avoid an N+1 query pattern", async () => {
    vi.spyOn(identityService, "fetchRelationshipsForAccount").mockResolvedValue([
      {
        relationshipId: "r1",
        parentGuardianId: ACCOUNT_ID,
        studentId: "st1",
        status: RelationshipStatus.Confirmed,
      },
      {
        relationshipId: "r2",
        parentGuardianId: ACCOUNT_ID,
        studentId: "st2",
        status: RelationshipStatus.Confirmed,
      },
    ]);
    const fetchStudentSchedule = vi.spyOn(schedulingService, "fetchStudentSchedule");

    renderDashboard();

    await userEvent.type(screen.getByLabelText("Parent/Guardian id"), ACCOUNT_ID);
    await userEvent.click(screen.getByRole("button", { name: "Look up" }));

    expect(
      await screen.findByText(/view each child.s sessions from their card above/i),
    ).toBeInTheDocument();
    expect(fetchStudentSchedule).not.toHaveBeenCalled();
  });
});
