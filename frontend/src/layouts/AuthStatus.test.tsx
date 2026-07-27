import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AuthStatus } from "@/layouts/AuthStatus";
import { AuthProvider } from "@/shared/context/AuthProvider";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { AuthHarness } from "@/test/AuthHarness";

function renderAuthStatus(authUser?: Parameters<typeof AuthHarness>[0]["user"]) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <NotificationProvider>
          <MemoryRouter>
            {authUser ? <AuthHarness user={authUser} /> : null}
            <AuthStatus />
          </MemoryRouter>
        </NotificationProvider>
      </AuthProvider>
    </QueryClientProvider>,
  );
}

describe("AuthStatus (Phase 4.9 Task 3)", () => {
  it("shows a Sign in link when signed out", () => {
    renderAuthStatus();

    expect(screen.getByRole("link", { name: "Sign in" })).toHaveAttribute("href", "/auth/login");
  });

  it("shows the signed-in identity (role + account id) and a Sign out action instead of Sign in", async () => {
    renderAuthStatus({
      token: "t",
      accountId: "11111111-1111-1111-1111-111111111111",
      role: "ParentGuardian",
      expiresAtUtc: "2999-01-01T00:00:00Z",
    });

    expect(await screen.findByText("Parent/Guardian")).toBeInTheDocument();
    expect(screen.getByText("11111111-1111-1111-1111-111111111111")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Sign out" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Sign in" })).not.toBeInTheDocument();
  });
});
