import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { TutorApprovalActions } from "@/features/identity/components/TutorApprovalActions";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";
import type { TutorDto } from "@/services/api/dtos";

const PENDING_TUTOR: TutorDto = {
  tutorId: "11111111-1111-1111-1111-111111111111",
  isApproved: false,
  isSuspended: false,
  isDiscoverable: false,
  hourlyRate: null,
  subject: null,
  language: null,
  location: null,
  offeredDurations: [],
};

describe("TutorApprovalActions", () => {
  it("approves a Tutor only after the confirm dialog is accepted", async () => {
    const approveTutor = vi.spyOn(identityService, "approveTutor").mockResolvedValue(undefined);

    renderWithProviders(<TutorApprovalActions tutor={PENDING_TUTOR} />);

    await userEvent.click(screen.getByRole("button", { name: "Approve" }));
    expect(approveTutor).not.toHaveBeenCalled();

    await userEvent.click(screen.getByRole("button", { name: "Approve Tutor" }));

    expect(await screen.findByText("Tutor approved.")).toBeInTheDocument();
    expect(approveTutor).toHaveBeenCalledWith(PENDING_TUTOR.tutorId);
  });

  it("disables Approve once already approved, and Suspend once already suspended", () => {
    renderWithProviders(
      <TutorApprovalActions tutor={{ ...PENDING_TUTOR, isApproved: true, isSuspended: true }} />,
    );

    expect(screen.getByRole("button", { name: "Approve" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Suspend" })).toBeDisabled();
  });
});
