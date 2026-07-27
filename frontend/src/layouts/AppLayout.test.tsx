import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ThemeProvider } from "@mui/material/styles";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { theme } from "@/app/theme";
import { AppLayout } from "@/layouts/AppLayout";
import { ActorProvider } from "@/shared/context/ActorProvider";
import { AuthProvider } from "@/shared/context/AuthProvider";
import { ColorModeProvider } from "@/shared/context/ColorModeProvider";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { ConfirmDialogProvider } from "@/shared/context/ConfirmDialogProvider";
import { AuthHarness } from "@/test/AuthHarness";
import type { AuthenticatedUser } from "@/shared/context/AuthContext";

function mockViewport(matches: boolean) {
  window.matchMedia = vi.fn((query: string): MediaQueryList => ({
    matches,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }));
}

function renderShell(authUser?: AuthenticatedUser) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(
    <ColorModeProvider>
      <ThemeProvider theme={theme}>
        <QueryClientProvider client={queryClient}>
          <AuthProvider>
            <ActorProvider>
              <NotificationProvider>
                <ConfirmDialogProvider>
                  <MemoryRouter initialEntries={["/"]}>
                    {authUser ? <AuthHarness user={authUser} /> : null}
                    <Routes>
                      <Route element={<AppLayout />}>
                        <Route path="/" element={<div>Page content</div>} />
                      </Route>
                    </Routes>
                  </MemoryRouter>
                </ConfirmDialogProvider>
              </NotificationProvider>
            </ActorProvider>
          </AuthProvider>
        </QueryClientProvider>
      </ThemeProvider>
    </ColorModeProvider>,
  );
}

describe("AppLayout", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("renders the routed page content", () => {
    mockViewport(true);
    renderShell();

    expect(screen.getByText("Page content")).toBeInTheDocument();
  });

  it("hides the hamburger button on a desktop-sized viewport", () => {
    mockViewport(true);
    renderShell();

    expect(screen.queryByLabelText("Open navigation")).not.toBeInTheDocument();
  });

  it("shows a hamburger button on a mobile-sized viewport that opens the navigation drawer", async () => {
    mockViewport(false);
    renderShell();

    const hamburger = screen.getByLabelText("Open navigation");

    await userEvent.click(hamburger);

    expect(await screen.findByRole("link", { name: "Dashboard" })).toBeInTheDocument();
  });

  // Phase 4.9 Task 2: the dev-only RoleSwitcher must never appear once a
  // real session is signed in (a control that visibly claims to change role
  // but can no longer do anything would only confuse a signed-in user).
  it("shows the dev-only RoleSwitcher when signed out, and hides it once signed in", async () => {
    mockViewport(true);
    renderShell();

    expect(await screen.findByLabelText("Acting as (dev only)")).toBeInTheDocument();
  });

  it("hides the dev-only RoleSwitcher for a real signed-in session", async () => {
    mockViewport(true);
    renderShell({ token: "t", accountId: "a1", role: "Student", expiresAtUtc: "2999-01-01T00:00:00Z" });

    await screen.findByText("Page content");
    expect(screen.queryByLabelText("Acting as (dev only)")).not.toBeInTheDocument();
  });
});
