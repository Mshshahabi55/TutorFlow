import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { NextLessonHeroCard } from "@/routes/dashboard/NextLessonHeroCard";
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

const NOW = Date.parse("2026-08-01T12:30:00Z");

function renderCard(session: SessionDto = SESSION, subject: string | null = "Mathematics") {
  return render(
    <MemoryRouter>
      <NextLessonHeroCard session={session} subject={subject} now={NOW} />
    </MemoryRouter>,
  );
}

describe("NextLessonHeroCard", () => {
  it("shows the subject, Student id, and a countdown to the lesson", () => {
    renderCard();

    expect(screen.getByText("Mathematics")).toBeInTheDocument();
    expect(screen.getByText(/Student: st1/)).toBeInTheDocument();
    expect(screen.getByText("in 1h 30min")).toBeInTheDocument();
  });

  it("falls back to a generic label when the Tutor has no subject set", () => {
    renderCard(SESSION, null);

    expect(screen.getByText("Lesson")).toBeInTheDocument();
  });

  it("shows 'Starting now' once the scheduled time has arrived", () => {
    renderCard(SESSION, "Mathematics");
    render(
      <MemoryRouter>
        <NextLessonHeroCard
          session={SESSION}
          subject="Mathematics"
          now={Date.parse("2026-08-01T14:05:00Z")}
        />
      </MemoryRouter>,
    );

    expect(screen.getByText("Starting now")).toBeInTheDocument();
  });

  it("links Open Lesson to the Session's real detail page", () => {
    renderCard();

    expect(screen.getByRole("link", { name: "Open Lesson" })).toHaveAttribute(
      "href",
      "/scheduling/sessions/44444444-4444-4444-4444-444444444444",
    );
  });

  it("disables the Join/Prepare placeholder action", () => {
    renderCard();

    expect(screen.getByRole("button", { name: "Join / Prepare" })).toBeDisabled();
  });
});
