import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import * as healthService from "@/services/api/healthService";

describe("App", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("renders the app shell and the home page", async () => {
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");

    render(<App />);

    expect(screen.getAllByText("TutorFlow").length).toBeGreaterThan(0);
    // RoleSwitcher is now lazy-loaded (Phase 4.9 Task 2, gated out of
    // production builds), so it resolves after the initial render.
    expect(await screen.findByLabelText("Acting as (dev only)")).toBeInTheDocument();
    expect(await screen.findByText("Healthy")).toBeInTheDocument();
  });

  // Phase D4: proves ColorModeProvider -> ThemeProvider -> ThemeToggle is
  // actually wired end to end in the real app tree, not just built and
  // proven in isolation (Phase D3's own StyleGuidePage-only preview).
  it("the real header's theme toggle switches the live app theme and persists the choice", async () => {
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");

    render(<App />);

    const toggle = await screen.findByLabelText("Switch to dark mode");
    await userEvent.click(toggle);

    expect(await screen.findByLabelText("Switch to light mode")).toBeInTheDocument();
    expect(window.localStorage.getItem("tutorflow.colorMode")).toBe("dark");
  });
});
