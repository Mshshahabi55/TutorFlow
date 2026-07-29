import { describe, expect, it } from "vitest";
import {
  addMonths,
  buildMonthMatrix,
  monthKeyFromDateKey,
  monthLabel,
  todayDateKey,
  todayMonthKey,
  WEEKDAY_LABELS,
} from "@/features/scheduling/utils/monthCalendar";

describe("monthKeyFromDateKey", () => {
  it("takes the year-month prefix of a date key", () => {
    expect(monthKeyFromDateKey("2026-08-15")).toBe("2026-08");
  });
});

describe("addMonths", () => {
  it("moves forward within a year", () => {
    expect(addMonths("2026-08", 1)).toBe("2026-09");
  });

  it("moves backward across a year boundary", () => {
    expect(addMonths("2026-01", -1)).toBe("2025-12");
  });

  it("moves forward across a year boundary", () => {
    expect(addMonths("2026-12", 1)).toBe("2027-01");
  });
});

describe("monthLabel", () => {
  it("formats a month key as a human-readable label", () => {
    expect(monthLabel("2026-08")).toBe("August 2026");
  });
});

describe("todayDateKey / todayMonthKey", () => {
  it("derives the month key from the date key for a fixed instant", () => {
    const now = new Date("2026-08-15T12:00:00Z");
    expect(todayDateKey(now)).toBe("2026-08-15");
    expect(todayMonthKey(now)).toBe("2026-08");
  });
});

describe("buildMonthMatrix", () => {
  it("returns full weeks of 7 days each", () => {
    const weeks = buildMonthMatrix("2026-08");
    for (const week of weeks) {
      expect(week).toHaveLength(7);
    }
  });

  it("includes every day of the target month exactly once, marked isCurrentMonth", () => {
    const weeks = buildMonthMatrix("2026-08");
    const currentMonthDays = weeks.flat().filter((day) => day.isCurrentMonth);
    expect(currentMonthDays).toHaveLength(31);
    expect(currentMonthDays[0].dateKey).toBe("2026-08-01");
    expect(currentMonthDays[currentMonthDays.length - 1].dateKey).toBe("2026-08-31");
  });

  it("pads leading/trailing days from adjacent months, marked not isCurrentMonth", () => {
    // August 1, 2026 is a Saturday, so the first week needs 6 leading July days.
    const weeks = buildMonthMatrix("2026-08");
    const firstWeek = weeks[0];
    expect(firstWeek[0].dateKey).toBe("2026-07-26");
    expect(firstWeek[0].isCurrentMonth).toBe(false);
    expect(firstWeek[6].dateKey).toBe("2026-08-01");
    expect(firstWeek[6].isCurrentMonth).toBe(true);
  });

  it("has exactly 7 weekday labels", () => {
    expect(WEEKDAY_LABELS).toHaveLength(7);
  });
});
