/**
 * A minimal RFC 5545 .ics file for exactly one lesson — "Add to Calendar"
 * on a booked Session. Deliberately does not attempt recurrence, alarms,
 * attendees, or any other calendar concept beyond one VEVENT: this is a
 * one-off booking (`PRODUCT_REQUIREMENTS.md` SCH-4 — no recurring bookings
 * in v1), and calendar apps universally support a bare single-event .ics
 * with no external dependency needed to produce one.
 */
export interface SessionCalendarEvent {
  sessionId: string;
  startTimeUtc: string;
  endTimeUtc: string;
  summary: string;
  description: string;
}

function toIcsDateTimeUtc(isoUtc: string): string {
  // "2026-07-29T14:00:00.000Z" -> "20260729T140000Z" — strips separators
  // and sub-second precision, per RFC 5545's DATE-TIME form (UTC, trailing Z).
  return isoUtc.replace(/[-:]/g, "").replace(/\.\d{3}Z$/, "Z");
}

/**
 * Folds/escapes only what plain lesson text can plausibly contain (commas,
 * semicolons, newlines) — RFC 5545 §3.3.11's minimum required escaping, not
 * a full TEXT-value implementation (no line folding at 75 octets: every
 * value here is short enough it will never approach that limit).
 */
function escapeIcsText(value: string): string {
  return value.replace(/\\/g, "\\\\").replace(/[,;]/g, (match) => `\\${match}`).replace(/\r?\n/g, "\\n");
}

export function buildSessionIcs(event: SessionCalendarEvent): string {
  const lines = [
    "BEGIN:VCALENDAR",
    "VERSION:2.0",
    "PRODID:-//TutorFlow//Session//EN",
    "CALSCALE:GREGORIAN",
    "BEGIN:VEVENT",
    `UID:tutorflow-session-${event.sessionId}@tutorflow`,
    `DTSTAMP:${toIcsDateTimeUtc(new Date().toISOString())}`,
    `DTSTART:${toIcsDateTimeUtc(event.startTimeUtc)}`,
    `DTEND:${toIcsDateTimeUtc(event.endTimeUtc)}`,
    `SUMMARY:${escapeIcsText(event.summary)}`,
    `DESCRIPTION:${escapeIcsText(event.description)}`,
    "END:VEVENT",
    "END:VCALENDAR",
  ];
  // RFC 5545 §1: lines MUST be terminated by CRLF.
  return lines.join("\r\n") + "\r\n";
}

/** Triggers a browser download of `content` as `filename` — a plain Blob + synthetic anchor click, no library needed for something this small. */
export function downloadTextFile(filename: string, content: string, mimeType: string) {
  const blob = new Blob([content], { type: mimeType });
  const url = URL.createObjectURL(blob);
  try {
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = filename;
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
  } finally {
    URL.revokeObjectURL(url);
  }
}
