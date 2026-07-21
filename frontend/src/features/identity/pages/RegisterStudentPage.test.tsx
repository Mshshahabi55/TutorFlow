import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RegisterStudentPage } from "@/features/identity/pages/RegisterStudentPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";

describe("RegisterStudentPage", () => {
  it("registers a minor Student and shows the returned id", async () => {
    const registerStudent = vi
      .spyOn(identityService, "registerStudent")
      .mockResolvedValue({ studentId: "22222222-2222-2222-2222-222222222222", isMinor: true });

    renderWithProviders(<RegisterStudentPage />);

    await userEvent.type(screen.getByLabelText("Email"), "student@example.com");
    await userEvent.type(screen.getByLabelText("Password"), "Password123!");
    await userEvent.click(screen.getByLabelText("This Student is a minor"));
    await userEvent.click(screen.getByRole("button", { name: "Register as Student" }));

    expect(registerStudent).toHaveBeenCalledWith("student@example.com", "Password123!", true);
    expect(await screen.findByText("22222222-2222-2222-2222-222222222222")).toBeInTheDocument();
  });

  it("registers an adult Student by default (checkbox unchecked)", async () => {
    const registerStudent = vi
      .spyOn(identityService, "registerStudent")
      .mockResolvedValue({ studentId: "33333333-3333-3333-3333-333333333333", isMinor: false });

    renderWithProviders(<RegisterStudentPage />);

    await userEvent.type(screen.getByLabelText("Email"), "adult-student@example.com");
    await userEvent.type(screen.getByLabelText("Password"), "Password123!");
    await userEvent.click(screen.getByRole("button", { name: "Register as Student" }));

    expect(registerStudent).toHaveBeenCalledWith("adult-student@example.com", "Password123!", false);
    expect(await screen.findByText("33333333-3333-3333-3333-333333333333")).toBeInTheDocument();
  });
});
