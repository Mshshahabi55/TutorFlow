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
import * as identityService from "@/features/identity/api/identityService";
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

const TUTOR = {
  tutorId: "t1",
  isApproved: true,
  isSuspended: false,
  isDiscoverable: true,
  hourlyRate: 500_000,
  subject: "Mathematics",
  language: "English",
  location: "Remote",
  offeredDurations: ["01:00:00"],
};

// One open slot besides the Session's own current slot ("a1", excluded by
// the reschedule picker) — the target every reschedule test below selects.
const OPEN_SLOTS = [
  {
    availabilitySlotId: "a1",
    tutorId: "t1",
    startTimeUtc: "2026-08-01T14:00:00Z",
    endTimeUtc: "2026-08-01T15:00:00Z",
    duration: "01:00:00",
    deliveryMode: DeliveryMode.Online,
    isConsumed: true,
  },
  {
    availabilitySlotId: "a2",
    tutorId: "t1",
    startTimeUtc: "2026-08-02T14:00:00Z",
    endTimeUtc: "2026-08-02T15:00:00Z",
    duration: "01:00:00",
    deliveryMode: DeliveryMode.Online,
    isConsumed: false,
  },
];

const STUDENT = { studentId: "st1", isMinor: false };

function mockTutor() {
  vi.spyOn(identityService, "fetchTutorById").mockResolvedValue(TUTOR);
  vi.spyOn(identityService, "fetchStudentById").mockResolvedValue(STUDENT);
}

describe("SessionDetailPage", () => {
  it("shows an id-lookup form when no id is in the route", () => {
    renderWithProviders(<SessionDetailPage />);

    expect(screen.getByLabelText("Session id")).toBeInTheDocument();
  });

  it("shows a friendly not-found panel, never the raw backend error, when the Session fails to load", async () => {
    vi.spyOn(schedulingService, "fetchSessionById").mockRejectedValue(new Error("404 Not Found"));

    renderWithProviders(<SessionDetailPage />, {
      initialEntries: [`/scheduling/sessions/${SESSION_ID}`],
      routePath: "/scheduling/sessions/:sessionId",
    });

    expect(
      await screen.findByRole("heading", { name: "We couldn’t load this lesson" }),
    ).toBeInTheDocument();
    expect(screen.queryByText("404 Not Found")).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Go Home" })).toHaveAttribute("href", "/");
  });

  it("shows a session detail skeleton while loading, not an abrupt spinner", () => {
    vi.spyOn(schedulingService, "fetchSessionById").mockReturnValue(new Promise(() => {}));

    renderWithProviders(<SessionDetailPage />, {
      initialEntries: [`/scheduling/sessions/${SESSION_ID}`],
      routePath: "/scheduling/sessions/:sessionId",
    });

    expect(screen.getByTestId("session-detail-skeleton")).toBeInTheDocument();
  });

  it("navigates to the id-specific route, shows the session and Tutor Summary, and offers reschedule while Scheduled", async () => {
    mockTutor();
    vi.spyOn(schedulingService, "fetchSessionById").mockResolvedValue(SCHEDULED_SESSION);
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue(OPEN_SLOTS);

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
    expect(await screen.findByText("Mathematics")).toBeInTheDocument();
    expect(await screen.findByLabelText("New Availability Slot")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Complete" })).toBeEnabled();
  });

  it("reschedules a session onto a different, open Availability Slot for the same Tutor", async () => {
    mockTutor();
    vi.spyOn(schedulingService, "fetchSessionById").mockResolvedValue(SCHEDULED_SESSION);
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue(OPEN_SLOTS);
    const rescheduleSession = vi
      .spyOn(schedulingService, "rescheduleSession")
      .mockResolvedValue(undefined);

    renderWithProviders(<SessionDetailPage />, {
      initialEntries: [`/scheduling/sessions/${SESSION_ID}`],
      routePath: "/scheduling/sessions/:sessionId",
    });

    await screen.findByText("Scheduled");
    await userEvent.click(await screen.findByLabelText("New Availability Slot"));
    await userEvent.click(await screen.findByRole("option", { name: /Aug 02, 2026/ }));
    await userEvent.click(screen.getByRole("button", { name: "Reschedule" }));

    expect(rescheduleSession).toHaveBeenCalledWith(SESSION_ID, "a2");
    expect(await screen.findByText("Session rescheduled.")).toBeInTheDocument();
  });

  it("does not offer the Session's own current slot as a reschedule target", async () => {
    mockTutor();
    vi.spyOn(schedulingService, "fetchSessionById").mockResolvedValue(SCHEDULED_SESSION);
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue(OPEN_SLOTS);

    renderWithProviders(<SessionDetailPage />, {
      initialEntries: [`/scheduling/sessions/${SESSION_ID}`],
      routePath: "/scheduling/sessions/:sessionId",
    });

    await screen.findByText("Scheduled");
    await userEvent.click(await screen.findByLabelText("New Availability Slot"));

    expect(screen.queryByRole("option", { name: /Aug 01, 2026/ })).not.toBeInTheDocument();
    expect(await screen.findByRole("option", { name: /Aug 02, 2026/ })).toBeInTheDocument();
  });

  it("hides reschedule and disables actions for a Completed session", async () => {
    mockTutor();
    vi.spyOn(schedulingService, "fetchSessionById").mockResolvedValue({
      ...SCHEDULED_SESSION,
      status: SessionStatus.Completed,
    });

    renderWithProviders(<SessionDetailPage />, {
      initialEntries: [`/scheduling/sessions/${SESSION_ID}`],
      routePath: "/scheduling/sessions/:sessionId",
    });

    expect(await screen.findByText("Completed")).toBeInTheDocument();
    expect(screen.queryByLabelText("New Availability Slot")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Complete" })).toBeDisabled();
  });

  it("shows a Session Summary card with the Session's own real date, duration, and platform", async () => {
    mockTutor();
    vi.spyOn(schedulingService, "fetchSessionById").mockResolvedValue(SCHEDULED_SESSION);
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue(OPEN_SLOTS);

    renderWithProviders(<SessionDetailPage />, {
      initialEntries: [`/scheduling/sessions/${SESSION_ID}`],
      routePath: "/scheduling/sessions/:sessionId",
    });

    expect(await screen.findByRole("heading", { name: "Session Summary" })).toBeInTheDocument();
    expect(screen.getByText("60 minutes")).toBeInTheDocument();
    expect(screen.getByText("Online")).toBeInTheDocument();
  });

  it("shows a Student Summary card with the Student's real id and minor status", async () => {
    mockTutor();
    vi.spyOn(identityService, "fetchStudentById").mockResolvedValue({ studentId: "st1", isMinor: true });
    vi.spyOn(schedulingService, "fetchSessionById").mockResolvedValue(SCHEDULED_SESSION);
    vi.spyOn(schedulingService, "fetchTutorAvailabilitySlots").mockResolvedValue(OPEN_SLOTS);

    renderWithProviders(<SessionDetailPage />, {
      initialEntries: [`/scheduling/sessions/${SESSION_ID}`],
      routePath: "/scheduling/sessions/:sessionId",
    });

    expect(await screen.findByText("st1")).toBeInTheDocument();
    expect(screen.getByText("Minor")).toBeInTheDocument();
  });
});
