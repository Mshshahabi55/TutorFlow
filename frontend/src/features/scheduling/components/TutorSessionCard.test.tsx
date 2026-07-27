import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { ConfirmDialogProvider } from "@/shared/context/ConfirmDialogProvider";
import { TutorSessionCard } from "@/features/scheduling/components/TutorSessionCard";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

const SESSION: SessionDto = {
  sessionId: "44444444-4444-4444-4444-444444444444",
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

function renderCard(session: SessionDto, onOpen: (session: SessionDto) => void) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <NotificationProvider>
        <ConfirmDialogProvider>
          <TutorSessionCard session={session} onOpen={onOpen} />
        </ConfirmDialogProvider>
      </NotificationProvider>
    </QueryClientProvider>,
  );
}

describe("TutorSessionCard", () => {
  it("shows the Student id instead of the Tutor id, since the Tutor is the constant viewer here", () => {
    renderCard(SESSION, () => {});

    expect(screen.getByText("Student: st1")).toBeInTheDocument();
    expect(screen.queryByText(/Tutor: /)).not.toBeInTheDocument();
  });

  it("calls onOpen when the informational content is clicked", async () => {
    const onOpen = vi.fn();
    renderCard(SESSION, onOpen);

    await userEvent.click(screen.getByText("Student: st1"));

    expect(onOpen).toHaveBeenCalledWith(SESSION);
  });

  it("does not trigger onOpen when a SessionActions button is clicked", async () => {
    const onOpen = vi.fn();
    const cancelSession = vi.spyOn(schedulingService, "cancelSession");
    renderCard(SESSION, onOpen);

    await userEvent.click(screen.getByRole("button", { name: "Complete" }));

    expect(onOpen).not.toHaveBeenCalled();
    expect(cancelSession).not.toHaveBeenCalled();
  });

  it("shows an explicit Open Lesson primary action that also calls onOpen", async () => {
    const onOpen = vi.fn();
    renderCard(SESSION, onOpen);

    await userEvent.click(screen.getByRole("button", { name: "Open Lesson" }));

    expect(onOpen).toHaveBeenCalledWith(SESSION);
  });
});
