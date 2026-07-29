import { describe, expect, it } from "vitest";
import { buildSessionIcs } from "@/shared/utils/ics";

describe("buildSessionIcs", () => {
  it("produces a valid single-VEVENT calendar with CRLF line endings", () => {
    const ics = buildSessionIcs({
      sessionId: "11111111-1111-1111-1111-111111111111",
      startTimeUtc: "2026-08-01T10:00:00.000Z",
      endTimeUtc: "2026-08-01T11:00:00.000Z",
      summary: "Lesson with Jane Doe",
      description: "Online lesson booked via TutorFlow.",
    });

    expect(ics).toContain("BEGIN:VCALENDAR\r\n");
    expect(ics).toContain("BEGIN:VEVENT\r\n");
    expect(ics).toContain("DTSTART:20260801T100000Z\r\n");
    expect(ics).toContain("DTEND:20260801T110000Z\r\n");
    expect(ics).toContain("SUMMARY:Lesson with Jane Doe\r\n");
    expect(ics).toContain("UID:tutorflow-session-11111111-1111-1111-1111-111111111111@tutorflow\r\n");
    expect(ics).toContain("END:VEVENT\r\n");
    expect(ics).toContain("END:VCALENDAR\r\n");
  });

  it("escapes commas, semicolons, backslashes, and newlines in text fields", () => {
    const ics = buildSessionIcs({
      sessionId: "a",
      startTimeUtc: "2026-08-01T10:00:00.000Z",
      endTimeUtc: "2026-08-01T11:00:00.000Z",
      summary: "Math, Physics; Chemistry",
      description: "Line one\nLine two \\ backslash",
    });

    expect(ics).toContain("SUMMARY:Math\\, Physics\\; Chemistry\r\n");
    expect(ics).toContain("DESCRIPTION:Line one\\nLine two \\\\ backslash\r\n");
  });
});
