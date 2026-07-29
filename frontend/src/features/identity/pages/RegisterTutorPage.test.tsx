import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RegisterTutorPage } from "@/features/identity/pages/RegisterTutorPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";

describe("RegisterTutorPage", () => {
  it("registers a Tutor and shows the returned id with a link to complete their profile", async () => {
    vi.spyOn(identityService, "registerTutor").mockResolvedValue({
      tutorId: "11111111-1111-1111-1111-111111111111",
      isApproved: false,
      isSuspended: false,
      isDiscoverable: false,
      hourlyRate: null,
      subject: null,
      language: null,
      location: null,
      offeredDurations: [],
    });

    renderWithProviders(<RegisterTutorPage />, { routePath: "/" });

    await userEvent.type(screen.getByLabelText("Email"), "tutor@example.com");
    await userEvent.type(screen.getByLabelText("Password"), "Password123!");
    await userEvent.click(screen.getByRole("button", { name: "Register as Tutor" }));

    expect(
      await screen.findByText("11111111-1111-1111-1111-111111111111"),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Complete your profile" })).toHaveAttribute(
      "href",
      "/identity/tutors/11111111-1111-1111-1111-111111111111/onboarding",
    );
  });

  it("shows an error state and lets the user retry when registration fails", async () => {
    vi.spyOn(identityService, "registerTutor").mockRejectedValue(new Error("Network Error"));

    renderWithProviders(<RegisterTutorPage />, { routePath: "/" });

    await userEvent.type(screen.getByLabelText("Email"), "tutor@example.com");
    await userEvent.type(screen.getByLabelText("Password"), "Password123!");
    await userEvent.click(screen.getByRole("button", { name: "Register as Tutor" }));

    expect(await screen.findByText("Network Error")).toBeInTheDocument();
  });
});
