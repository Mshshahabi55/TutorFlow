import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { StudentSessionListPage } from "@/features/scheduling/pages/StudentSessionListPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { ConfirmDialogProvider } from "@/shared/context/ConfirmDialogProvider";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";

const STUDENT_ID = "33333333-3333-3333-3333-333333333333";
const SESSION_ID = "44444444-4444-4444-4444-444444444444";

const SESSION = {
  sessionId: SESSION_ID,
  tutorId: "t1",
  studentId: STUDENT_ID,
  parentGuardianId: null,
  availabilitySlotId: "a1",
  scheduledTimeUtc: "2026-08-01T14:00:00Z",
  endTimeUtc: "2026-08-01T15:00:00Z",
  duration: "01:00:00",
  deliveryMode: DeliveryMode.Online,
  status: SessionStatus.Scheduled,
};

describe("StudentSessionListPage", () => {
  it("shows an id-lookup form when no id is in the route", () => {
    renderWithProviders(<StudentSessionListPage />);

    expect(screen.getByLabelText("Student id")).toBeInTheDocument();
  });

  it("shows an empty state when the Student has no sessions", async () => {
    vi.spyOn(schedulingService, "fetchStudentSchedule").mockResolvedValue([]);

    renderWithProviders(<StudentSessionListPage />, {
      initialEntries: [`/scheduling/students/${STUDENT_ID}/schedule`],
      routePath: "/scheduling/students/:studentId/schedule",
    });

    expect(await screen.findByText("No sessions yet")).toBeInTheDocument();
  });

  it("lists sessions and navigates to the detail page on row click, without triggering a row action", async () => {
    vi.spyOn(schedulingService, "fetchStudentSchedule").mockResolvedValue([SESSION]);
    const cancelSession = vi.spyOn(schedulingService, "cancelSession");

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={queryClient}>
        <NotificationProvider>
          <ConfirmDialogProvider>
            <MemoryRouter initialEntries={[`/scheduling/students/${STUDENT_ID}/schedule`]}>
              <Routes>
                <Route
                  path="/scheduling/students/:studentId/schedule"
                  element={<StudentSessionListPage />}
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

    const cell = await screen.findByText("t1");
    await userEvent.click(cell);

    expect(await screen.findByText("Session detail route reached")).toBeInTheDocument();
    expect(cancelSession).not.toHaveBeenCalled();
  });
});
