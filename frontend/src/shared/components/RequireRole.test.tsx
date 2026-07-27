import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { RequireRole } from "@/shared/components/RequireRole";
import { ActorProvider } from "@/shared/context/ActorProvider";
import { AuthProvider } from "@/shared/context/AuthProvider";
import { AuthHarness } from "@/test/AuthHarness";
import type { AuthenticatedUser } from "@/shared/context/AuthContext";
import type { ActorRole } from "@/shared/context/ActorContext";

function renderGuarded(roles: ActorRole[], authUser?: AuthenticatedUser) {
  return render(
    <AuthProvider>
      <ActorProvider>
        <MemoryRouter>
          {authUser ? <AuthHarness user={authUser} /> : null}
          <RequireRole roles={roles}>
            <div>Privileged content</div>
          </RequireRole>
        </MemoryRouter>
      </ActorProvider>
    </AuthProvider>,
  );
}

describe("RequireRole (Phase 4.9 Task 4)", () => {
  it("renders the guarded content for a matching authenticated role", () => {
    renderGuarded(["AdminStaff"], {
      token: "t",
      accountId: "a1",
      role: "AdminStaff",
      expiresAtUtc: "2999-01-01T00:00:00Z",
    });

    expect(screen.getByText("Privileged content")).toBeInTheDocument();
  });

  it("shows ForbiddenState, not the guarded content, for a mismatched authenticated role", () => {
    renderGuarded(["AdminStaff"], {
      token: "t",
      accountId: "a1",
      role: "Student",
      expiresAtUtc: "2999-01-01T00:00:00Z",
    });

    expect(screen.queryByText("Privileged content")).not.toBeInTheDocument();
    expect(screen.getByText("Not authorized")).toBeInTheDocument();
    expect(screen.getByText(/only available to Admin\/Staff/)).toBeInTheDocument();
  });

  it("denies by default when no role is known at all — unlike NavSidebar's permissive display convenience", () => {
    renderGuarded(["AdminStaff"]);

    expect(screen.queryByText("Privileged content")).not.toBeInTheDocument();
    expect(screen.getByText("Not authorized")).toBeInTheDocument();
  });

  it("still allows the dev-preview role to satisfy the guard when not really authenticated", () => {
    window.localStorage.setItem("tutorflow.devActorRole", "AdminStaff");

    renderGuarded(["AdminStaff"]);

    expect(screen.getByText("Privileged content")).toBeInTheDocument();
    window.localStorage.clear();
  });
});
