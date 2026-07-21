import { describe, expect, it } from "vitest";
import {
  formatMinutesList,
  minutesToTimeSpan,
  parseMinutesList,
  timeSpanToMinutes,
} from "@/shared/utils/duration";

describe("minutesToTimeSpan", () => {
  it("formats sub-hour minutes", () => {
    expect(minutesToTimeSpan(30)).toBe("00:30:00");
  });

  it("formats whole hours", () => {
    expect(minutesToTimeSpan(60)).toBe("01:00:00");
  });

  it("formats hours and minutes", () => {
    expect(minutesToTimeSpan(90)).toBe("01:30:00");
  });
});

describe("timeSpanToMinutes", () => {
  it("parses an hh:mm:ss string back to minutes", () => {
    expect(timeSpanToMinutes("01:30:00")).toBe(90);
  });

  it("parses a sub-hour duration", () => {
    expect(timeSpanToMinutes("00:45:00")).toBe(45);
  });
});

describe("parseMinutesList", () => {
  it("parses a comma-separated list, trimming whitespace", () => {
    expect(parseMinutesList("30, 45,  60")).toEqual([30, 45, 60]);
  });

  it("ignores empty segments", () => {
    expect(parseMinutesList("30,,60")).toEqual([30, 60]);
  });
});

describe("formatMinutesList", () => {
  it("round-trips a list of TimeSpan strings to a comma-separated minutes string", () => {
    expect(formatMinutesList(["00:30:00", "01:00:00"])).toBe("30, 60");
  });
});
