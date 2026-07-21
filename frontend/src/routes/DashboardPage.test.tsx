import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
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
        <DashboardPage />
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

  it("shows every available module, with no role selected", () => {
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");

    renderDashboard();

    expect(screen.getByText("Available modules")).toBeInTheDocument();
    expect(screen.getByText("Scheduling & Booking")).toBeInTheDocument();
    expect(screen.getByText(/select a role/i)).toBeInTheDocument();
  });

  it("shows a role-specific summary once a role is selected", async () => {
    window.localStorage.setItem("tutorflow.devActorRole", "Tutor");
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");

    renderDashboard();

    expect(await screen.findByText(/hourly rate/i)).toBeInTheDocument();
  });
});
