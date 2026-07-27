import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AppHeader } from "@/layouts/AppHeader";
import { AuthProvider } from "@/shared/context/AuthProvider";
import { ActorProvider } from "@/shared/context/ActorProvider";
import { ColorModeProvider } from "@/shared/context/ColorModeProvider";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { AuthHarness } from "@/test/AuthHarness";
import type { AuthenticatedUser } from "@/shared/context/AuthContext";

function renderHeader(isDesktop: boolean, authUser?: AuthenticatedUser) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const onOpenMobileNav = vi.fn();

  render(
    <QueryClientProvider client={queryClient}>
      <ColorModeProvider>
        <AuthProvider>
          <ActorProvider>
            <NotificationProvider>
              <MemoryRouter initialEntries={["/"]}>
                {authUser ? <AuthHarness user={authUser} /> : null}
                <AppHeader isDesktop={isDesktop} onOpenMobileNav={onOpenMobileNav} />
              </MemoryRouter>
            </NotificationProvider>
          </ActorProvider>
        </AuthProvider>
      </ColorModeProvider>
    </QueryClientProvider>,
  );

  return { onOpenMobileNav };
}

describe("AppHeader", () => {
  it("shows the hamburger only on a non-desktop viewport", () => {
    renderHeader(false);
    expect(screen.getByLabelText("Open navigation")).toBeInTheDocument();
  });

  it("hides the hamburger on a desktop viewport", () => {
    renderHeader(true);
    expect(screen.queryByLabelText("Open navigation")).not.toBeInTheDocument();
  });

  it("shows the breadcrumb, search, notifications, and theme toggle", () => {
    renderHeader(true);

    expect(screen.getByText("Home")).toBeInTheDocument();
    expect(screen.getByLabelText("Search")).toBeInTheDocument();
    expect(screen.getByLabelText("Notifications")).toBeInTheDocument();
    expect(screen.getByLabelText("Switch to dark mode")).toBeInTheDocument();
  });

  it("shows Sign in when signed out, and the account menu when signed in", async () => {
    renderHeader(true);
    expect(screen.getByRole("link", { name: "Sign in" })).toBeInTheDocument();

    renderHeader(true, {
      token: "t",
      accountId: "a1",
      role: "Student",
      expiresAtUtc: "2999-01-01T00:00:00Z",
    });
    expect(await screen.findByRole("button", { name: "Account menu (Student)" })).toBeInTheDocument();
  });
});
