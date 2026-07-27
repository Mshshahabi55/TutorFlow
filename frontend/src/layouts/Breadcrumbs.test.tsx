import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { Breadcrumbs } from "@/layouts/Breadcrumbs";
import { paths } from "@/routes/paths";

function renderAt(pathname: string) {
  return render(
    <MemoryRouter initialEntries={[pathname]}>
      <Breadcrumbs />
    </MemoryRouter>,
  );
}

describe("Breadcrumbs", () => {
  it("shows just 'Dashboard', not a link, on the home route", () => {
    renderAt(paths.home);

    expect(screen.getByText("Dashboard")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Dashboard" })).not.toBeInTheDocument();
  });

  it("shows Dashboard (link) > Section > Page for a known route", () => {
    renderAt(paths.discovery.tutorSearch);

    expect(screen.getByRole("link", { name: "Dashboard" })).toHaveAttribute("href", paths.home);
    expect(screen.getByText("Discovery")).toBeInTheDocument();
    expect(screen.getByText("Search Tutors")).toBeInTheDocument();
  });

  it("also resolves a sub-route of a known entry", () => {
    renderAt(`${paths.identity.studentDetailBase}/some-id`);

    expect(screen.getByText("Identity & Relationship")).toBeInTheDocument();
    expect(screen.getByText("Students")).toBeInTheDocument();
  });

  it("falls back to just Dashboard for a route SECTIONS doesn't name", () => {
    renderAt("/some/unknown/route");

    expect(screen.getByRole("link", { name: "Dashboard" })).toBeInTheDocument();
  });
});
