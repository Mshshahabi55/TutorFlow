import { beforeEach, describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { Breadcrumbs } from "@/layouts/Breadcrumbs";
import { ActorProvider } from "@/shared/context/ActorProvider";
import { AuthProvider } from "@/shared/context/AuthProvider";
import { paths } from "@/routes/paths";

function renderAt(pathname: string, role?: string) {
  if (role) {
    window.localStorage.setItem("tutorflow.devActorRole", role);
  }
  return render(
    <AuthProvider>
      <ActorProvider>
        <MemoryRouter initialEntries={[pathname]}>
          <Breadcrumbs />
        </MemoryRouter>
      </ActorProvider>
    </AuthProvider>,
  );
}

describe("Breadcrumbs", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("shows just the home label, not a link, on the home route, with no role selected", () => {
    renderAt(paths.home);

    expect(screen.getByText("Home")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Home" })).not.toBeInTheDocument();
  });

  it("uses the role-appropriate home label (Dashboard for a Tutor, Home for a Student)", () => {
    renderAt(paths.home, "Tutor");

    expect(screen.getByText("Dashboard")).toBeInTheDocument();
  });

  it("shows a flat Home > Page trail for a known route, with no section title", () => {
    renderAt(paths.discovery.tutorSearch);

    expect(screen.getByRole("link", { name: "Home" })).toHaveAttribute("href", paths.home);
    expect(screen.getByText("Find Tutors")).toBeInTheDocument();
  });

  it("falls back to just the home crumb for a route the nav doesn't name", () => {
    renderAt("/some/unknown/route");

    expect(screen.getByRole("link", { name: "Home" })).toBeInTheDocument();
  });
});
