import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { App } from "@/app/App";
import * as healthService from "@/services/api/healthService";

describe("App", () => {
  it("renders the app shell and the home page", async () => {
    vi.spyOn(healthService, "fetchHealthStatus").mockResolvedValue("Healthy");

    render(<App />);

    expect(screen.getAllByText("TutorFlow").length).toBeGreaterThan(0);
    expect(screen.getByLabelText("Acting as (dev only)")).toBeInTheDocument();
    expect(await screen.findByText("Healthy")).toBeInTheDocument();
  });
});
