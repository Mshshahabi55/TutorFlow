import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AdminPendingTutorsPage } from "@/features/identity/pages/AdminPendingTutorsPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";

const PENDING_TUTOR = {
  tutorId: "11111111-1111-1111-1111-111111111111",
  isApproved: false,
  isSuspended: false,
  isDiscoverable: false,
  hourlyRate: null,
  subject: "Mathematics",
  language: null,
  location: null,
  offeredDurations: [],
};

describe("AdminPendingTutorsPage", () => {
  it("shows a skeleton layout while loading, not an abrupt spinner", () => {
    vi.spyOn(identityService, "fetchPendingTutors").mockReturnValue(new Promise(() => {}));

    renderWithProviders(<AdminPendingTutorsPage />);

    expect(screen.getAllByTestId("pending-tutor-card-skeleton").length).toBeGreaterThan(0);
  });

  it("shows each pending Tutor's real subject and Pending approval status", async () => {
    vi.spyOn(identityService, "fetchPendingTutors").mockResolvedValue({
      items: [PENDING_TUTOR],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });

    renderWithProviders(<AdminPendingTutorsPage />);

    expect(await screen.findByText("Mathematics")).toBeInTheDocument();
    expect(screen.getByText("Pending approval")).toBeInTheDocument();
  });

  it("shows an empty state when there is nothing pending", async () => {
    vi.spyOn(identityService, "fetchPendingTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });

    renderWithProviders(<AdminPendingTutorsPage />);

    expect(await screen.findByText("No Tutors are pending approval")).toBeInTheDocument();
  });

  it("approves a Tutor only after the confirm dialog is accepted", async () => {
    vi.spyOn(identityService, "fetchPendingTutors").mockResolvedValue({
      items: [PENDING_TUTOR],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });
    const approveTutor = vi.spyOn(identityService, "approveTutor").mockResolvedValue(undefined);

    renderWithProviders(<AdminPendingTutorsPage />);

    await userEvent.click(await screen.findByRole("button", { name: "Approve" }));
    expect(screen.getByText("Approve this Tutor?")).toBeInTheDocument();
    expect(approveTutor).not.toHaveBeenCalled();

    await userEvent.click(screen.getByRole("button", { name: "Approve Tutor" }));

    expect(await screen.findByText("Tutor approved.")).toBeInTheDocument();
    expect(approveTutor).toHaveBeenCalledWith(PENDING_TUTOR.tutorId);
  });

  it("cancelling the suspend confirmation does not call suspendTutor", async () => {
    vi.spyOn(identityService, "fetchPendingTutors").mockResolvedValue({
      items: [PENDING_TUTOR],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });
    const suspendTutor = vi.spyOn(identityService, "suspendTutor");

    renderWithProviders(<AdminPendingTutorsPage />);

    await userEvent.click(await screen.findByRole("button", { name: "Suspend" }));
    await userEvent.click(screen.getByRole("button", { name: "Cancel" }));

    expect(suspendTutor).not.toHaveBeenCalled();
  });
});
