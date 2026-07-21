import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RegisterParentGuardianPage } from "@/features/identity/pages/RegisterParentGuardianPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";

describe("RegisterParentGuardianPage", () => {
  it("registers a Parent/Guardian and shows the returned id", async () => {
    vi.spyOn(identityService, "registerParentGuardian").mockResolvedValue({
      parentGuardianId: "44444444-4444-4444-4444-444444444444",
    });

    renderWithProviders(<RegisterParentGuardianPage />);

    await userEvent.type(screen.getByLabelText("Email"), "parent@example.com");
    await userEvent.type(screen.getByLabelText("Password"), "Password123!");
    await userEvent.click(screen.getByRole("button", { name: "Register as Parent/Guardian" }));

    expect(await screen.findByText("44444444-4444-4444-4444-444444444444")).toBeInTheDocument();
  });

  it("shows an error state when registration fails", async () => {
    vi.spyOn(identityService, "registerParentGuardian").mockRejectedValue(
      new Error("Network Error"),
    );

    renderWithProviders(<RegisterParentGuardianPage />);

    await userEvent.type(screen.getByLabelText("Email"), "parent@example.com");
    await userEvent.type(screen.getByLabelText("Password"), "Password123!");
    await userEvent.click(screen.getByRole("button", { name: "Register as Parent/Guardian" }));

    expect(await screen.findByText("Network Error")).toBeInTheDocument();
  });
});
