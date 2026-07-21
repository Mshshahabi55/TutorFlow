import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SessionActions } from "@/features/scheduling/components/SessionActions";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

const SCHEDULED_SESSION: SessionDto = {
  sessionId: "11111111-1111-1111-1111-111111111111",
  tutorId: "t1",
  studentId: "st1",
  parentGuardianId: null,
  availabilitySlotId: "a1",
  scheduledTimeUtc: "2026-08-01T14:00:00Z",
  endTimeUtc: "2026-08-01T15:00:00Z",
  duration: "01:00:00",
  deliveryMode: DeliveryMode.Online,
  status: SessionStatus.Scheduled,
};

describe("SessionActions", () => {
  it("completes a session only after the confirm dialog is accepted", async () => {
    const completeSession = vi
      .spyOn(schedulingService, "completeSession")
      .mockResolvedValue(undefined);

    renderWithProviders(<SessionActions session={SCHEDULED_SESSION} />);

    await userEvent.click(screen.getByRole("button", { name: "Complete" }));
    expect(completeSession).not.toHaveBeenCalled();

    await userEvent.click(screen.getByRole("button", { name: "Mark completed" }));

    expect(await screen.findByText("Session marked completed.")).toBeInTheDocument();
    expect(completeSession).toHaveBeenCalledWith(SCHEDULED_SESSION.sessionId);
  });

  it("cancels a session only after the confirm dialog is accepted", async () => {
    const cancelSession = vi.spyOn(schedulingService, "cancelSession").mockResolvedValue(undefined);

    renderWithProviders(<SessionActions session={SCHEDULED_SESSION} />);

    await userEvent.click(screen.getByRole("button", { name: "Cancel" }));
    await userEvent.click(screen.getByRole("button", { name: "Cancel session" }));

    expect(await screen.findByText("Session cancelled.")).toBeInTheDocument();
    expect(cancelSession).toHaveBeenCalledWith(SCHEDULED_SESSION.sessionId);
  });

  it("marks a session No-Show only after the confirm dialog is accepted", async () => {
    const markNoShow = vi
      .spyOn(schedulingService, "markSessionNoShow")
      .mockResolvedValue(undefined);

    renderWithProviders(<SessionActions session={SCHEDULED_SESSION} />);

    await userEvent.click(screen.getByRole("button", { name: "No-Show" }));
    await userEvent.click(screen.getByRole("button", { name: "Mark No-Show" }));

    expect(await screen.findByText("Session marked No-Show.")).toBeInTheDocument();
    expect(markNoShow).toHaveBeenCalledWith(SCHEDULED_SESSION.sessionId);
  });

  it("disables every action for a Session that is not Scheduled", () => {
    renderWithProviders(
      <SessionActions session={{ ...SCHEDULED_SESSION, status: SessionStatus.Completed }} />,
    );

    expect(screen.getByRole("button", { name: "Complete" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "No-Show" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Cancel" })).toBeDisabled();
  });
});
