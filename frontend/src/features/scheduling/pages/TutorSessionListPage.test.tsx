import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { TutorSessionListPage } from "@/features/scheduling/pages/TutorSessionListPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { ConfirmDialogProvider } from "@/shared/context/ConfirmDialogProvider";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";

const TUTOR_ID = "11111111-1111-1111-1111-111111111111";
const SESSION_ID = "44444444-4444-4444-4444-444444444444";

const SESSION = {
  sessionId: SESSION_ID,
  tutorId: TUTOR_ID,
  studentId: "st1",
  parentGuardianId: null,
  availabilitySlotId: "a1",
  scheduledTimeUtc: "2026-08-01T14:00:00Z",
  endTimeUtc: "2026-08-01T15:00:00Z",
  duration: "01:00:00",
  deliveryMode: DeliveryMode.Online,
  status: SessionStatus.Scheduled,
};

describe("TutorSessionListPage", () => {
  it("shows an id-lookup form when no id is in the route", () => {
    renderWithProviders(<TutorSessionListPage />);

    expect(screen.getByLabelText("Tutor id")).toBeInTheDocument();
  });

  it("shows an empty state when the Tutor has no sessions", async () => {
    vi.spyOn(schedulingService, "fetchTutorSchedule").mockResolvedValue([]);

    renderWithProviders(<TutorSessionListPage />, {
      initialEntries: [`/scheduling/tutors/${TUTOR_ID}/schedule`],
      routePath: "/scheduling/tutors/:tutorId/schedule",
    });

    expect(await screen.findByText("No sessions yet")).toBeInTheDocument();
  });

  it("clicking a row action does not also navigate the row to the detail page", async () => {
    vi.spyOn(schedulingService, "fetchTutorSchedule").mockResolvedValue([SESSION]);

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={queryClient}>
        <NotificationProvider>
          <ConfirmDialogProvider>
            <MemoryRouter initialEntries={[`/scheduling/tutors/${TUTOR_ID}/schedule`]}>
              <Routes>
                <Route
                  path="/scheduling/tutors/:tutorId/schedule"
                  element={<TutorSessionListPage />}
                />
                <Route
                  path="/scheduling/sessions/:sessionId"
                  element={<div>Session detail route reached</div>}
                />
              </Routes>
            </MemoryRouter>
          </ConfirmDialogProvider>
        </NotificationProvider>
      </QueryClientProvider>,
    );

    await userEvent.click(await screen.findByRole("button", { name: "Cancel" }));

    expect(screen.getByText("Cancel this session?")).toBeInTheDocument();
    expect(screen.queryByText("Session detail route reached")).not.toBeInTheDocument();
  });
});
