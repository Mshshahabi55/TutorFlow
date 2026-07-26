import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { DashboardPage } from "@/routes/DashboardPage";
import { ActorProvider } from "@/shared/context/ActorProvider";
import * as healthService from "@/services/api/healthService";

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

    renderDashboard();

    expect(screen.getByRole("link", { name: /Search Tutors/ })).toHaveAttribute(
      "href",
      "/discovery/tutors/search",
    );
    expect(screen.queryByRole("link", { name: /Declare availability/ })).not.toBeInTheDocument();
  });
});
