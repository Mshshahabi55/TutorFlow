import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { SessionDetailPage } from "@/features/scheduling/pages/SessionDetailPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { ConfirmDialogProvider } from "@/shared/context/ConfirmDialogProvider";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";

const SESSION_ID = "44444444-4444-4444-4444-444444444444";

const SCHEDULED_SESSION = {
  sessionId: SESSION_ID,
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

describe("SessionDetailPage", () => {
  it("shows an id-lookup form when no id is in the route", () => {
    renderWithProviders(<SessionDetailPage />);

    expect(screen.getByLabelText("Session id")).toBeInTheDocument();
  });

  it("navigates to the id-specific route, shows the session, and offers reschedule while Scheduled", async () => {
    vi.spyOn(schedulingService, "fetchSessionById").mockResolvedValue(SCHEDULED_SESSION);

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={queryClient}>
        <NotificationProvider>
          <ConfirmDialogProvider>
            <MemoryRouter initialEntries={["/scheduling/sessions"]}>
              <Routes>
                <Route path="/scheduling/sessions" element={<SessionDetailPage />} />
                <Route path="/scheduling/sessions/:sessionId" element={<SessionDetailPage />} />
              </Routes>
            </MemoryRouter>
          </ConfirmDialogProvider>
        </NotificationProvider>
      </QueryClientProvider>,
    );

    await userEvent.type(screen.getByLabelText("Session id"), SESSION_ID);
    await userEvent.click(screen.getByRole("button", { name: "Look up" }));

    expect(await screen.findByText("Scheduled")).toBeInTheDocument();
    expect(screen.getByLabelText("New start time (UTC, ISO 8601)")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Complete" })).toBeEnabled();
  });

  it("reschedules a session to a new UTC time", async () => {
    vi.spyOn(schedulingService, "fetchSessionById").mockResolvedValue(SCHEDULED_SESSION);
    const rescheduleSession = vi
      .spyOn(schedulingService, "rescheduleSession")
      .mockResolvedValue(undefined);

    renderWithProviders(<SessionDetailPage />, {
      initialEntries: [`/scheduling/sessions/${SESSION_ID}`],
      routePath: "/scheduling/sessions/:sessionId",
    });

    await screen.findByText("Scheduled");
    await userEvent.type(
      screen.getByLabelText("New start time (UTC, ISO 8601)"),
      "2026-08-02T14:00:00Z",
    );
    await userEvent.click(screen.getByRole("button", { name: "Reschedule" }));

    expect(rescheduleSession).toHaveBeenCalledWith(SESSION_ID, "2026-08-02T14:00:00Z");
    expect(await screen.findByText("Session rescheduled.")).toBeInTheDocument();
  });

  it("hides reschedule and disables actions for a Completed session", async () => {
    vi.spyOn(schedulingService, "fetchSessionById").mockResolvedValue({
      ...SCHEDULED_SESSION,
      status: SessionStatus.Completed,
    });

    renderWithProviders(<SessionDetailPage />, {
      initialEntries: [`/scheduling/sessions/${SESSION_ID}`],
      routePath: "/scheduling/sessions/:sessionId",
    });

    expect(await screen.findByText("Completed")).toBeInTheDocument();
    expect(screen.queryByLabelText("New start time (UTC, ISO 8601)")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Complete" })).toBeDisabled();
  });
});
