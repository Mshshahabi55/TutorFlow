import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { DashboardSectionCard } from "@/routes/dashboard/DashboardSectionCard";

describe("DashboardSectionCard", () => {
  it("renders the title as a heading and the children", () => {
    render(
      <DashboardSectionCard title="Upcoming Sessions">
        <p>Nothing yet</p>
      </DashboardSectionCard>,
    );

    expect(screen.getByRole("heading", { name: "Upcoming Sessions" })).toBeInTheDocument();
    expect(screen.getByText("Nothing yet")).toBeInTheDocument();
  });

  it("labels the section region with its own heading for assistive tech", () => {
    render(
      <DashboardSectionCard title="Recent Activity">
        <p>content</p>
      </DashboardSectionCard>,
    );

    const region = screen.getByRole("region", { name: "Recent Activity" });
    expect(region).toBeInTheDocument();
  });

  it("renders an optional action alongside the title", () => {
    render(
      <DashboardSectionCard title="Recommended Tutors" action={<button>Browse all tutors</button>}>
        <p>content</p>
      </DashboardSectionCard>,
    );

    expect(screen.getByRole("button", { name: "Browse all tutors" })).toBeInTheDocument();
  });
});
