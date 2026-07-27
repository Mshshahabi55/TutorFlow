import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { StudentRosterCard } from "@/features/scheduling/components/StudentRosterCard";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { ConfirmDialogProvider } from "@/shared/context/ConfirmDialogProvider";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";
import type { StudentRosterEntry } from "@/features/scheduling/utils/studentRoster";

const NEXT_SESSION: SessionDto = {
  sessionId: "s-next",
  tutorId: "t1",
  studentId: "st-1",
  parentGuardianId: null,
  availabilitySlotId: "a1",
  scheduledTimeUtc: "2026-08-05T14:00:00Z",
  endTimeUtc: "2026-08-05T15:00:00Z",
  duration: "01:00:00",
  deliveryMode: DeliveryMode.Online,
  status: SessionStatus.Scheduled,
};

const PAST_SESSION: SessionDto = {
  ...NEXT_SESSION,
  sessionId: "s-past",
  scheduledTimeUtc: "2026-07-01T14:00:00Z",
  status: SessionStatus.Completed,
};

function entry(overrides: Partial<StudentRosterEntry> = {}): StudentRosterEntry {
  return {
    studentId: "st-1",
    totalSessions: 2,
    upcomingSessions: 1,
    nextSession: NEXT_SESSION,
    mostRecentPastSession: PAST_SESSION,
    sessions: [NEXT_SESSION, PAST_SESSION],
    ...overrides,
  };
}

function renderCard(props: Partial<StudentRosterEntry> = {}) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <NotificationProvider>
        <ConfirmDialogProvider>
          <MemoryRouter>
            <StudentRosterCard entry={entry(props)} />
          </MemoryRouter>
        </ConfirmDialogProvider>
      </NotificationProvider>
    </QueryClientProvider>,
  );
}

describe("StudentRosterCard", () => {
  it("shows the Student id and total lesson count", () => {
    renderCard({ totalSessions: 3 });

    expect(screen.getByText("st-1")).toBeInTheDocument();
    expect(screen.getByText("3 lessons together")).toBeInTheDocument();
  });

  it("uses singular 'lesson' for exactly one total session", () => {
    renderCard({ totalSessions: 1 });

    expect(screen.getByText("1 lesson together")).toBeInTheDocument();
  });

  it("shows an upcoming-count pill only when there is at least one upcoming session", () => {
    renderCard({ upcomingSessions: 2 });
    expect(screen.getByText("2 upcoming")).toBeInTheDocument();
  });

  it("shows no upcoming-count pill when there are no upcoming sessions", () => {
    renderCard({ upcomingSessions: 0, nextSession: undefined });
    expect(screen.queryByText(/upcoming/)).not.toBeInTheDocument();
  });

  it("shows the next and last lesson times", () => {
    renderCard();

    expect(screen.getByText(toTehranDisplay(NEXT_SESSION.scheduledTimeUtc))).toBeInTheDocument();
    expect(screen.getByText(toTehranDisplay(PAST_SESSION.scheduledTimeUtc))).toBeInTheDocument();
  });

  it("shows an honest placeholder when there is no next or last lesson", () => {
    renderCard({ nextSession: undefined, mostRecentPastSession: undefined });

    expect(screen.getByText("None scheduled")).toBeInTheDocument();
    expect(screen.getByText("No lessons yet")).toBeInTheDocument();
  });

  it("toggles a History section listing every session, without a broken link to a Student profile", async () => {
    renderCard();

    expect(screen.queryByRole("button", { name: "Open Lesson" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /view profile/i })).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "View History" }));

    expect(screen.getAllByRole("button", { name: "Open Lesson" }).length).toBeGreaterThan(0);

    await userEvent.click(screen.getByRole("button", { name: "Hide History" }));

    expect(screen.queryByRole("button", { name: "Open Lesson" })).not.toBeInTheDocument();
  });

  it("links Add Availability to the real Manage Your Schedule route", () => {
    renderCard();

    expect(screen.getByRole("link", { name: "Add Availability" })).toHaveAttribute(
      "href",
      "/scheduling/availability/declare",
    );
  });

  it("disables the Message placeholder action", () => {
    renderCard();

    expect(screen.getByRole("button", { name: "Message" })).toBeDisabled();
  });
});
