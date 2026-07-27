import { beforeEach, describe, expect, it, vi } from "vitest";
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
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("shows a friendly identity prompt, not a raw id field, when no id is known", () => {
    renderWithProviders(<TutorSessionListPage />);

    expect(screen.getByRole("heading", { name: "Let's find your calendar" })).toBeInTheDocument();
    expect(screen.getByLabelText("Tutor id")).toBeInTheDocument();
  });

  it("redirects straight to the remembered Tutor's schedule without re-asking", () => {
    window.localStorage.setItem("tutorflow.rememberedId.tutor", TUTOR_ID);
    vi.spyOn(schedulingService, "fetchTutorSchedule").mockResolvedValue([]);

    renderWithProviders(<TutorSessionListPage />);

    expect(screen.queryByLabelText("Tutor id")).not.toBeInTheDocument();
  });

  it("shows a skeleton layout while loading, not an abrupt spinner", () => {
    vi.spyOn(schedulingService, "fetchTutorSchedule").mockReturnValue(new Promise(() => {}));

    renderWithProviders(<TutorSessionListPage />, {
      initialEntries: [`/scheduling/tutors/${TUTOR_ID}/schedule`],
      routePath: "/scheduling/tutors/:tutorId/schedule",
    });

    expect(screen.getAllByTestId("session-card-skeleton").length).toBeGreaterThan(0);
  });

  it("shows a professional empty state guiding the Tutor to declare availability, when there are no sessions", async () => {
    vi.spyOn(schedulingService, "fetchTutorSchedule").mockResolvedValue([]);

    renderWithProviders(<TutorSessionListPage />, {
      initialEntries: [`/scheduling/tutors/${TUTOR_ID}/schedule`],
      routePath: "/scheduling/tutors/:tutorId/schedule",
    });

    expect(await screen.findByText("No sessions yet")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Declare availability/ })).toHaveAttribute(
      "href",
      "/scheduling/availability/declare",
    );
  });

  it("groups sessions into Upcoming, Completed, and Cancelled & No-Show", async () => {
    const upcoming = SESSION;
    const completed = { ...SESSION, sessionId: "66666666-6666-6666-6666-666666666666", status: SessionStatus.Completed };
    const cancelled = { ...SESSION, sessionId: "77777777-7777-7777-7777-777777777777", status: SessionStatus.Cancelled };
    vi.spyOn(schedulingService, "fetchTutorSchedule").mockResolvedValue([upcoming, completed, cancelled]);

    renderWithProviders(<TutorSessionListPage />, {
      initialEntries: [`/scheduling/tutors/${TUTOR_ID}/schedule`],
      routePath: "/scheduling/tutors/:tutorId/schedule",
    });

    expect(await screen.findByRole("heading", { name: "Upcoming Sessions" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Completed" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Cancelled & No-Show" })).toBeInTheDocument();
    expect(screen.getAllByText("Student: st1")).toHaveLength(3);
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
