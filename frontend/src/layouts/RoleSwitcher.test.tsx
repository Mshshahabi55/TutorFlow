import { beforeEach, describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RoleSwitcher } from "@/layouts/RoleSwitcher";
import { ActorProvider } from "@/shared/context/ActorProvider";
import { AuthProvider } from "@/shared/context/AuthProvider";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { ConfirmDialogProvider } from "@/shared/context/ConfirmDialogProvider";
import { AuthHarness } from "@/test/AuthHarness";
import type { AuthenticatedUser } from "@/shared/context/AuthContext";

function renderRoleSwitcher(authUser?: AuthenticatedUser) {
  return render(
    <AuthProvider>
      <ActorProvider>
        <NotificationProvider>
          <ConfirmDialogProvider>
            {authUser ? <AuthHarness user={authUser} /> : null}
            <RoleSwitcher />
          </ConfirmDialogProvider>
        </NotificationProvider>
      </ActorProvider>
    </AuthProvider>,
  );
}

describe("RoleSwitcher", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("notifies when a role is selected", async () => {
    renderRoleSwitcher();

    await userEvent.click(screen.getByLabelText("Acting as (dev only)"));
    await userEvent.click(await screen.findByRole("option", { name: "Tutor" }));

    expect(
      await screen.findByText(/now acting as tutor \(dev only/i),
    ).toBeInTheDocument();
  });

  it("asks for confirmation before clearing a selected role, and keeps it if cancelled", async () => {
    renderRoleSwitcher();

    await userEvent.click(screen.getByLabelText("Acting as (dev only)"));
    await userEvent.click(await screen.findByRole("option", { name: "Student" }));
    await screen.findByText(/now acting as student/i);

    await userEvent.click(screen.getByLabelText("Clear role selection"));
    expect(await screen.findByText("Clear the selected role?")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Cancel" }));

    expect(screen.getByLabelText("Acting as (dev only)")).toHaveTextContent("Student");
  });

  it("clears the role once the clear action is confirmed", async () => {
    renderRoleSwitcher();

    await userEvent.click(screen.getByLabelText("Acting as (dev only)"));
    await userEvent.click(await screen.findByRole("option", { name: "Student" }));
    await screen.findByText(/now acting as student/i);

    await userEvent.click(screen.getByLabelText("Clear role selection"));
    await userEvent.click(await screen.findByRole("button", { name: "Clear role" }));

    // The "role selected" notification is still showing (single-at-a-time
    // queue), so the button's disappearance — not a second toast — is what
    // proves the role itself was actually cleared.
    expect(screen.queryByLabelText("Clear role selection")).not.toBeInTheDocument();
  });

  // Phase 4.9 Task 2: a signed-in user's real role can never be overridden
  // by this control (useEffectiveRole already guarantees that), but showing
  // an interactive selector that visibly does nothing would still confuse a
  // signed-in user — so it renders nothing at all once authenticated.
  it("renders nothing while a real session is signed in", () => {
    renderRoleSwitcher({
      token: "t",
      accountId: "a1",
      role: "Student",
      expiresAtUtc: "2999-01-01T00:00:00Z",
      email: "student@example.com",
    });

    expect(screen.queryByLabelText("Acting as (dev only)")).not.toBeInTheDocument();
  });
});
