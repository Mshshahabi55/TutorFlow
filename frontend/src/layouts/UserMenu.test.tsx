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

  const result = render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <NotificationProvider>
          {authUser ? <AuthHarness user={authUser} /> : null}
          <UserMenu />
        </NotificationProvider>
      </AuthProvider>
    </QueryClientProvider>,
  );

  return { queryClient, ...result };
}

const STUDENT: AuthenticatedUser = {
  token: "t",
  accountId: "11111111-1111-1111-1111-111111111111",
  role: "ParentGuardian",
  expiresAtUtc: "2999-01-01T00:00:00Z",
  email: "parent@example.com",
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

  it("opening the menu shows the email, role label, and a sign out action — never the raw account id", async () => {
    renderUserMenu(STUDENT);

    await userEvent.click(await screen.findByRole("button", { name: "Account menu (Parent/Guardian)" }));

    expect(screen.getByText("Parent/Guardian")).toBeInTheDocument();
    expect(screen.getByText(STUDENT.email)).toBeInTheDocument();
    expect(screen.queryByText(STUDENT.accountId)).not.toBeInTheDocument();
    expect(screen.getByRole("menuitem", { name: "Sign out" })).toBeInTheDocument();
  });

  it("signing out clears the session", async () => {
    vi.spyOn(authService, "logout").mockResolvedValue(undefined);
    renderUserMenu(STUDENT);

    await userEvent.click(await screen.findByRole("button", { name: "Account menu (Parent/Guardian)" }));
    await userEvent.click(screen.getByRole("menuitem", { name: "Sign out" }));

    expect(await screen.findByText("Signed out.")).toBeInTheDocument();
  });

  // RC4.4: symmetric with useLogin's own fix — signing out must not leave a
  // previously-authenticated result behind for whoever/whatever uses this
  // browser tab next (a different account signing in, or dev-preview
  // browsing), since the QueryClient itself outlives the session.
  it("signing out clears the QueryClient cache", async () => {
    vi.spyOn(authService, "logout").mockResolvedValue(undefined);
    const { queryClient } = renderUserMenu(STUDENT);

    const key = ["communication", "conversations", "mine"];
    queryClient.setQueryData(key, [{ conversationId: "c1" }]);
    expect(queryClient.getQueryData(key)).toBeDefined();

    await userEvent.click(await screen.findByRole("button", { name: "Account menu (Parent/Guardian)" }));
    await userEvent.click(screen.getByRole("menuitem", { name: "Sign out" }));

    await screen.findByText("Signed out.");
    expect(queryClient.getQueryState(key)).toBeUndefined();
  });
});
