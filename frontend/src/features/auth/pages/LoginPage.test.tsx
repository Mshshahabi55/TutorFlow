import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { LoginPage } from "@/features/auth/pages/LoginPage";
import { AuthProvider } from "@/shared/context/AuthProvider";
import { ActorProvider } from "@/shared/context/ActorProvider";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { ConfirmDialogProvider } from "@/shared/context/ConfirmDialogProvider";
import * as authService from "@/features/auth/api/authService";

function renderLoginPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <ActorProvider>
          <NotificationProvider>
            <ConfirmDialogProvider>
              <MemoryRouter initialEntries={["/auth/login"]}>
                <Routes>
                  <Route path="/auth/login" element={<LoginPage />} />
                  <Route path="/" element={<div>Dashboard route reached</div>} />
                </Routes>
              </MemoryRouter>
            </ConfirmDialogProvider>
          </NotificationProvider>
        </ActorProvider>
      </AuthProvider>
    </QueryClientProvider>,
  );
}

describe("LoginPage", () => {
  it("signs in with the entered credentials and navigates home", async () => {
    const login = vi.spyOn(authService, "login").mockResolvedValue({
      token: "raw-token",
      accountId: "11111111-1111-1111-1111-111111111111",
      role: "Tutor",
      expiresAtUtc: "2026-07-20T18:00:00Z",
    });

    renderLoginPage();

    await userEvent.type(screen.getByLabelText("Email"), "tutor@example.com");
    await userEvent.type(screen.getByLabelText("Password"), "Password123!");
    await userEvent.click(screen.getByRole("button", { name: "Sign in" }));

    expect(login).toHaveBeenCalledWith("tutor@example.com", "Password123!");
    expect(await screen.findByText("Dashboard route reached")).toBeInTheDocument();
  });

  it("shows an error state instead of navigating when credentials are rejected", async () => {
    vi.spyOn(authService, "login").mockRejectedValue(new Error("Invalid email or password."));

    renderLoginPage();

    await userEvent.type(screen.getByLabelText("Email"), "tutor@example.com");
    await userEvent.type(screen.getByLabelText("Password"), "WrongPassword1");
    await userEvent.click(screen.getByRole("button", { name: "Sign in" }));

    expect(await screen.findByText("Invalid email or password.")).toBeInTheDocument();
    expect(screen.queryByText("Dashboard route reached")).not.toBeInTheDocument();
  });

  it("rejects a malformed email instead of submitting", async () => {
    const login = vi.spyOn(authService, "login");

    renderLoginPage();

    await userEvent.type(screen.getByLabelText("Email"), "not-an-email");
    await userEvent.type(screen.getByLabelText("Password"), "Password123!");
    await userEvent.click(screen.getByRole("button", { name: "Sign in" }));

    expect(await screen.findByText(/enter a valid email address/i)).toBeInTheDocument();
    expect(login).not.toHaveBeenCalled();
  });
});
