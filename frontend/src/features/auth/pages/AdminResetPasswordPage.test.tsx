import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AdminResetPasswordPage } from "@/features/auth/pages/AdminResetPasswordPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as authService from "@/features/auth/api/authService";

describe("AdminResetPasswordPage", () => {
  it("resets the password for a valid account id and shows a confirmation", async () => {
    const resetPassword = vi.spyOn(authService, "resetPassword").mockResolvedValue(undefined);

    renderWithProviders(<AdminResetPasswordPage />);

    await userEvent.type(
      screen.getByLabelText("Account id"),
      "11111111-1111-1111-1111-111111111111",
    );
    await userEvent.type(screen.getByLabelText("New password"), "New-Password-123!");
    await userEvent.click(screen.getByRole("button", { name: "Reset password" }));

    expect(resetPassword).toHaveBeenCalledWith(
      "11111111-1111-1111-1111-111111111111",
      "New-Password-123!",
    );
    expect(
      await screen.findByText(/every existing session for this account was signed out/i),
    ).toBeInTheDocument();
  });

  it("rejects a malformed account id instead of submitting", async () => {
    const resetPassword = vi.spyOn(authService, "resetPassword");

    renderWithProviders(<AdminResetPasswordPage />);

    await userEvent.type(screen.getByLabelText("Account id"), "not-a-guid");
    await userEvent.type(screen.getByLabelText("New password"), "New-Password-123!");
    await userEvent.click(screen.getByRole("button", { name: "Reset password" }));

    expect(await screen.findByText(/enter a valid id/i)).toBeInTheDocument();
    expect(resetPassword).not.toHaveBeenCalled();
  });

  it("shows an error state when the reset fails", async () => {
    vi.spyOn(authService, "resetPassword").mockRejectedValue(new Error("Account was not found."));

    renderWithProviders(<AdminResetPasswordPage />);

    await userEvent.type(
      screen.getByLabelText("Account id"),
      "11111111-1111-1111-1111-111111111111",
    );
    await userEvent.type(screen.getByLabelText("New password"), "New-Password-123!");
    await userEvent.click(screen.getByRole("button", { name: "Reset password" }));

    expect(await screen.findByText("Account was not found.")).toBeInTheDocument();
  });
});
