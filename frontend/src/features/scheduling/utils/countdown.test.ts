import { describe, expect, it } from "vitest";
import { formatCountdown } from "@/features/scheduling/utils/countdown";

const NOW = Date.parse("2026-08-01T12:30:00Z");

describe("formatCountdown", () => {
  it("shows hours and minutes when both are non-zero", () => {
    expect(formatCountdown("2026-08-01T14:00:00Z", NOW)).toBe("in 1h 30min");
  });

  it("shows only minutes when under an hour away", () => {
    expect(formatCountdown("2026-08-01T12:45:00Z", NOW)).toBe("in 15 min");
  });

  it("shows only hours when exactly on the hour", () => {
    expect(formatCountdown("2026-08-01T14:30:00Z", NOW)).toBe("in 2h");
  });

  it("shows 'Starting now' once the scheduled time has arrived", () => {
    expect(formatCountdown("2026-08-01T12:30:00Z", NOW)).toBe("Starting now");
  });

  it("shows 'Starting now' for a time already in the past", () => {
    expect(formatCountdown("2026-08-01T10:00:00Z", NOW)).toBe("Starting now");
  });
});
