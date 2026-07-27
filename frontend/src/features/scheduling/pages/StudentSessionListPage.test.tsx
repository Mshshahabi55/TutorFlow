import { beforeEach, describe, expect, it, vi } from "vitest";
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
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("shows a friendly identity prompt, not a raw id field, when no id is known", () => {
    renderWithProviders(<StudentSessionListPage />);

    expect(screen.getByRole("heading", { name: "Let's find your lessons" })).toBeInTheDocument();
    expect(screen.getByLabelText("Student id")).toBeInTheDocument();
  });

  it("redirects straight to the remembered Student's schedule without re-asking", () => {
    window.localStorage.setItem("tutorflow.rememberedId.student", STUDENT_ID);
    vi.spyOn(schedulingService, "fetchStudentSchedule").mockResolvedValue([]);

    renderWithProviders(<StudentSessionListPage />);

    expect(screen.queryByLabelText("Student id")).not.toBeInTheDocument();
  });

  it("shows a skeleton layout while loading, not an abrupt spinner", () => {
    vi.spyOn(schedulingService, "fetchStudentSchedule").mockReturnValue(new Promise(() => {}));

    renderWithProviders(<StudentSessionListPage />, {
      initialEntries: [`/scheduling/students/${STUDENT_ID}/schedule`],
      routePath: "/scheduling/students/:studentId/schedule",
    });

    expect(screen.getAllByTestId("session-card-skeleton").length).toBeGreaterThan(0);
  });

  it("shows a professional empty state guiding the Student to find a tutor, when there are no sessions", async () => {
    vi.spyOn(schedulingService, "fetchStudentSchedule").mockResolvedValue([]);

    renderWithProviders(<StudentSessionListPage />, {
      initialEntries: [`/scheduling/students/${STUDENT_ID}/schedule`],
      routePath: "/scheduling/students/:studentId/schedule",
    });

    expect(await screen.findByText("No lessons yet")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Find Tutors/ })).toHaveAttribute(
      "href",
      "/discovery/tutors/search",
    );
  });

  it("offers a large Book another lesson CTA when there are existing sessions", async () => {
    vi.spyOn(schedulingService, "fetchStudentSchedule").mockResolvedValue([SESSION]);

    renderWithProviders(<StudentSessionListPage />, {
      initialEntries: [`/scheduling/students/${STUDENT_ID}/schedule`],
      routePath: "/scheduling/students/:studentId/schedule",
    });

    expect(await screen.findByRole("link", { name: /Book another lesson/ })).toHaveAttribute(
      "href",
      "/scheduling/sessions/book",
    );
  });

  it("highlights the sole Scheduled session as the Next Lesson, and navigates to its detail page on click, without triggering a row action", async () => {
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

    expect(await screen.findByRole("heading", { name: "Next Lesson" })).toBeInTheDocument();

    const info = await screen.findByText("Tutor: t1");
    await userEvent.click(info);

    expect(await screen.findByText("Session detail route reached")).toBeInTheDocument();
    expect(cancelSession).not.toHaveBeenCalled();
  });

  it("groups a second upcoming session under Upcoming Sessions, separate from the Next Lesson highlight", async () => {
    const laterSession = {
      ...SESSION,
      sessionId: "55555555-5555-5555-5555-555555555555",
      scheduledTimeUtc: "2026-08-05T14:00:00Z",
      endTimeUtc: "2026-08-05T15:00:00Z",
    };
    vi.spyOn(schedulingService, "fetchStudentSchedule").mockResolvedValue([laterSession, SESSION]);

    renderWithProviders(<StudentSessionListPage />, {
      initialEntries: [`/scheduling/students/${STUDENT_ID}/schedule`],
      routePath: "/scheduling/students/:studentId/schedule",
    });

    expect(await screen.findByRole("heading", { name: "Next Lesson" })).toBeInTheDocument();
    expect(await screen.findByRole("heading", { name: "Upcoming Sessions" })).toBeInTheDocument();
  });

  it("groups Completed and Cancelled/No-Show sessions separately under History", async () => {
    const completedSession = {
      ...SESSION,
      sessionId: "66666666-6666-6666-6666-666666666666",
      status: SessionStatus.Completed,
    };
    const cancelledSession = {
      ...SESSION,
      sessionId: "77777777-7777-7777-7777-777777777777",
      status: SessionStatus.Cancelled,
    };
    vi.spyOn(schedulingService, "fetchStudentSchedule").mockResolvedValue([
      completedSession,
      cancelledSession,
    ]);

    renderWithProviders(<StudentSessionListPage />, {
      initialEntries: [`/scheduling/students/${STUDENT_ID}/schedule`],
      routePath: "/scheduling/students/:studentId/schedule",
    });

    expect(await screen.findByRole("heading", { name: "History" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Completed" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Cancelled & No-Show" })).toBeInTheDocument();
  });
});
