import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AddToCalendarButton } from "@/features/scheduling/components/AddToCalendarButton";
import * as ics from "@/shared/utils/ics";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

const SESSION: SessionDto = {
  sessionId: "11111111-1111-1111-1111-111111111111",
  tutorId: "tutor-1",
  studentId: "student-1",
  parentGuardianId: null,
  availabilitySlotId: "slot-1",
  scheduledTimeUtc: "2026-08-01T10:00:00.000Z",
  endTimeUtc: "2026-08-01T11:00:00.000Z",
  duration: "01:00:00",
  deliveryMode: DeliveryMode.Online,
  status: SessionStatus.Scheduled,
};

describe("AddToCalendarButton", () => {
  it("builds and downloads an .ics file naming the Tutor when clicked", async () => {
    const user = userEvent.setup();
    const downloadSpy = vi.spyOn(ics, "downloadTextFile").mockImplementation(() => {});

    render(<AddToCalendarButton session={SESSION} tutorName="Jane Doe" />);
    await user.click(screen.getByRole("button", { name: "Add to Calendar" }));

    expect(downloadSpy).toHaveBeenCalledTimes(1);
    const [filename, content, mimeType] = downloadSpy.mock.calls[0];
    expect(filename).toBe("tutorflow-lesson-11111111.ics");
    expect(content).toContain("SUMMARY:Lesson with Jane Doe");
    expect(mimeType).toBe("text/calendar");
  });

  it("falls back to a generic label when no Tutor name is known", async () => {
    const user = userEvent.setup();
    const downloadSpy = vi.spyOn(ics, "downloadTextFile").mockImplementation(() => {});

    render(<AddToCalendarButton session={SESSION} />);
    await user.click(screen.getByRole("button", { name: "Add to Calendar" }));

    const [, content] = downloadSpy.mock.calls[0];
    expect(content).toContain("SUMMARY:Lesson with your Tutor");
  });

  it("describes an in-person lesson differently from an online one", async () => {
    const user = userEvent.setup();
    const downloadSpy = vi.spyOn(ics, "downloadTextFile").mockImplementation(() => {});

    render(<AddToCalendarButton session={{ ...SESSION, deliveryMode: DeliveryMode.InPerson }} />);
    await user.click(screen.getByRole("button", { name: "Add to Calendar" }));

    const [, content] = downloadSpy.mock.calls[0];
    expect(content).toContain("In-person lesson");
  });
});
