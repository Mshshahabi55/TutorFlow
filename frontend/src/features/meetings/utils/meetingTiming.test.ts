import { describe, expect, it } from "vitest";
import { deriveMeetingTiming } from "@/features/meetings/utils/meetingTiming";

describe("deriveMeetingTiming", () => {
  it("shows a countdown before the meeting starts", () => {
    const now = new Date("2026-08-01T12:00:00Z");
    const result = deriveMeetingTiming("2026-08-01T14:15:00Z", "2026-08-01T15:15:00Z", now);

    expect(result.phase).toBe("upcoming");
    expect(result.label).toBe("Starts in 2h 15m");
  });

  it("rounds a sub-minute countdown up to 1m rather than showing 0m", () => {
    const now = new Date("2026-08-01T13:59:50Z");
    const result = deriveMeetingTiming("2026-08-01T14:00:00Z", "2026-08-01T15:00:00Z", now);

    expect(result.label).toBe("Starts in 1m");
  });

  it("shows Live now once the start time has passed but the meeting has not ended", () => {
    const now = new Date("2026-08-01T14:30:00Z");
    const result = deriveMeetingTiming("2026-08-01T14:00:00Z", "2026-08-01T15:00:00Z", now);

    expect(result.phase).toBe("live");
    expect(result.label).toBe("Live now");
  });

  it("shows Ended once the end time has passed", () => {
    const now = new Date("2026-08-01T15:30:00Z");
    const result = deriveMeetingTiming("2026-08-01T14:00:00Z", "2026-08-01T15:00:00Z", now);

    expect(result.phase).toBe("ended");
    expect(result.label).toBe("Ended");
  });
});
