import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { DashboardPage } from "@/routes/DashboardPage";
import { ActorProvider } from "@/shared/context/ActorProvider";
import * as healthService from "@/services/api/healthService";
import * as discoveryService from "@/features/discovery/api/discoveryService";

function renderDashboard() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <ActorProvider>
        <MemoryRouter>
          <DashboardPage />
        </MemoryRouter>
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

  it("shows a role-specific summary once a role is selected", async () => {
    window.localStorage.setItem("tutorflow.devActorRole", "Tutor");
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");

    renderDashboard();

    expect(await screen.findByText(/hourly rate/i)).toBeInTheDocument();
  });

  // Phase D2 Task 4: the new "role-appropriate next actions" requirement —
  // each quick action is a real link to an already-existing route (the
  // same ones NavSidebar exposes for that role), not new UI-only content.
  it("shows the Tutor's quick actions as real links to already-existing routes", () => {
    window.localStorage.setItem("tutorflow.devActorRole", "Tutor");
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");

    renderDashboard();

    expect(screen.getByText("Quick actions")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Declare availability/ })).toHaveAttribute(
      "href",
      "/scheduling/availability/declare",
    );
    expect(screen.getByRole("link", { name: /My sessions/ })).toHaveAttribute(
      "href",
      "/scheduling/tutors",
    );
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
