import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { UserMenu } from "@/layouts/UserMenu";
import { AuthProvider } from "@/shared/context/AuthProvider";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { AuthHarness } from "@/test/AuthHarness";
import type { AuthenticatedUser } from "@/shared/context/AuthContext";
import * as authService from "@/features/auth/api/authService";

function renderUserMenu(authUser?: AuthenticatedUser) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <NotificationProvider>
          {authUser ? <AuthHarness user={authUser} /> : null}
          <UserMenu />
        </NotificationProvider>
      </AuthProvider>
    </QueryClientProvider>,
  );
}

const STUDENT: AuthenticatedUser = {
  token: "t",
  accountId: "11111111-1111-1111-1111-111111111111",
  role: "ParentGuardian",
  expiresAtUtc: "2999-01-01T00:00:00Z",
};

describe("UserMenu", () => {
  it("renders nothing when signed out", () => {
    const { container } = renderUserMenu();

    expect(container).toBeEmptyDOMElement();
  });

  it("shows the account menu button once signed in", async () => {
    renderUserMenu(STUDENT);

    expect(await screen.findByRole("button", { name: "Account menu (Parent/Guardian)" })).toBeInTheDocument();
  });

  it("opening the menu shows the role label, account id, and a sign out action", async () => {
    renderUserMenu(STUDENT);

    await userEvent.click(await screen.findByRole("button", { name: "Account menu (Parent/Guardian)" }));

    expect(screen.getByText("Parent/Guardian")).toBeInTheDocument();
    expect(screen.getByText(STUDENT.accountId)).toBeInTheDocument();
    expect(screen.getByRole("menuitem", { name: "Sign out" })).toBeInTheDocument();
  });

  it("signing out clears the session", async () => {
    vi.spyOn(authService, "logout").mockResolvedValue(undefined);
    renderUserMenu(STUDENT);

    await userEvent.click(await screen.findByRole("button", { name: "Account menu (Parent/Guardian)" }));
    await userEvent.click(screen.getByRole("menuitem", { name: "Sign out" }));

    expect(await screen.findByText("Signed out.")).toBeInTheDocument();
  });
});
