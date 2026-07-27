import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { TutorStudentsPage } from "@/features/scheduling/pages/TutorStudentsPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";

const TUTOR_ID = "11111111-1111-1111-1111-111111111111";

function session(overrides: { sessionId: string; studentId: string; status: SessionStatus }) {
  return {
    sessionId: overrides.sessionId,
    tutorId: TUTOR_ID,
    studentId: overrides.studentId,
    parentGuardianId: null,
    availabilitySlotId: "a1",
    scheduledTimeUtc: "2026-08-01T14:00:00Z",
    endTimeUtc: "2026-08-01T15:00:00Z",
    duration: "01:00:00",
    deliveryMode: DeliveryMode.Online,
    status: overrides.status,
  };
}

describe("TutorStudentsPage", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("shows a friendly identity prompt when no Tutor id is known", () => {
    renderWithProviders(<TutorStudentsPage />);

    expect(screen.getByRole("heading", { name: "Let's find your students" })).toBeInTheDocument();
    expect(screen.getByLabelText("Tutor id")).toBeInTheDocument();
  });

  it("shows an empty state guiding toward Availability, when there are no sessions", async () => {
    window.localStorage.setItem("tutorflow.rememberedId.tutor", TUTOR_ID);
    vi.spyOn(schedulingService, "fetchTutorSchedule").mockResolvedValue([]);

    renderWithProviders(<TutorStudentsPage />);

    expect(await screen.findByText("No students yet")).toBeInTheDocument();
  });

  it("groups sessions by Student, deduplicating repeat bookings from the same Student", async () => {
    window.localStorage.setItem("tutorflow.rememberedId.tutor", TUTOR_ID);
    vi.spyOn(schedulingService, "fetchTutorSchedule").mockResolvedValue([
      session({ sessionId: "s1", studentId: "st-1", status: SessionStatus.Scheduled }),
      session({ sessionId: "s2", studentId: "st-1", status: SessionStatus.Completed }),
      session({ sessionId: "s3", studentId: "st-2", status: SessionStatus.Completed }),
    ]);

    renderWithProviders(<TutorStudentsPage />);

    expect(await screen.findByText("st-1")).toBeInTheDocument();
    expect(screen.getByText("st-2")).toBeInTheDocument();
    expect(screen.getByText("2 lessons together")).toBeInTheDocument();
    expect(screen.getByText("1 lesson together")).toBeInTheDocument();
    expect(screen.getByText("1 upcoming")).toBeInTheDocument();
  });
});
