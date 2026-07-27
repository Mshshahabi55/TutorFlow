import { describe, expect, it } from "vitest";
import {
  fromTehranInput,
  isValidTehranLocalInput,
  tehranDateKey,
  tehranDateLabel,
  todayInTehranLabel,
  toTehranDisplay,
  toTehranInputValue,
} from "@/shared/time/tehranTime";

describe("fromTehranInput / toTehranInputValue — the +03:30 half-hour offset", () => {
  it("converts a Tehran local input to the correct UTC instant (subtracts 3h30m)", () => {
    expect(fromTehranInput("2026-08-01T17:30")).toBe("2026-08-01T14:00:00.000Z");
  });

  it("converts a UTC instant back to the correct Tehran local input (adds 3h30m)", () => {
    expect(toTehranInputValue("2026-08-01T14:00:00Z")).toBe("2026-08-01T17:30");
  });

  it("handles a non-half-hour-aligned time correctly in both directions", () => {
    expect(fromTehranInput("2026-08-01T09:05")).toBe("2026-08-01T05:35:00.000Z");
    expect(toTehranInputValue("2026-08-01T05:35:00Z")).toBe("2026-08-01T09:05");
  });
});

describe("date-boundary crossing", () => {
  it("01:00 Tehran on Aug 2 is 21:30 UTC on Aug 1 (Tehran -> UTC)", () => {
    expect(fromTehranInput("2026-08-02T01:00")).toBe("2026-08-01T21:30:00.000Z");
  });

  it("21:30 UTC on Aug 1 displays as 01:00 Tehran on Aug 2 (UTC -> Tehran)", () => {
    expect(toTehranInputValue("2026-08-01T21:30:00Z")).toBe("2026-08-02T01:00");
  });

  it("also crosses the date boundary going the other way (Tehran late night -> UTC next-day-ish is not the case, but early Tehran morning -> prior UTC day is)", () => {
    // 00:15 Tehran on Jan 1 is 20:45 UTC on Dec 31 — a year boundary too.
    expect(fromTehranInput("2026-01-01T00:15")).toBe("2025-12-31T20:45:00.000Z");
  });
});

describe("round-tripping", () => {
  const instants = [
    "2026-08-01T14:00:00.000Z",
    "2026-01-01T00:00:00.000Z",
    "2026-12-31T23:59:00.000Z",
    "2026-03-08T02:00:00.000Z", // a US DST "spring forward" instant, see below
  ];

  it.each(instants)("fromTehranInput(toTehranInputValue(%s)) returns the original instant", (iso) => {
    const roundTripped = fromTehranInput(toTehranInputValue(iso));
    expect(Date.parse(roundTripped)).toBe(Date.parse(iso));
  });
});

describe("no inherited DST", () => {
  it("keeps exactly a 3h30m offset across a US DST 'spring forward' transition", () => {
    // 2026-03-08 07:00 UTC is the instant US Eastern time springs forward.
    // Tehran has no DST, so the offset either side of it must stay 3:30.
    expect(toTehranInputValue("2026-03-08T06:00:00Z")).toBe("2026-03-08T09:30");
    expect(toTehranInputValue("2026-03-08T08:00:00Z")).toBe("2026-03-08T11:30");
  });

  it("keeps exactly a 3h30m offset across a US DST 'fall back' transition", () => {
    // 2026-11-01 06:00 UTC is the instant US Eastern time falls back.
    expect(toTehranInputValue("2026-11-01T05:00:00Z")).toBe("2026-11-01T08:30");
    expect(toTehranInputValue("2026-11-01T07:00:00Z")).toBe("2026-11-01T10:30");
  });

  it("keeps exactly a 3h30m offset across Iran's own former DST transition dates (now abolished)", () => {
    // Pre-2022, Iran itself observed DST around this date. Post-abolition,
    // there must be no transition here at all.
    expect(fromTehranInput("2026-03-22T00:00")).toBe("2026-03-21T20:30:00.000Z");
    expect(fromTehranInput("2026-09-22T00:00")).toBe("2026-09-21T20:30:00.000Z");
  });
});

describe("toTehranDisplay", () => {
  it("formats a UTC instant as a human-readable Tehran-local string", () => {
    expect(toTehranDisplay("2026-08-01T14:00:00Z")).toBe("Aug 01, 2026, 17:30");
  });

  it("formats a date-boundary-crossing instant on the correct (Tehran) day", () => {
    expect(toTehranDisplay("2026-08-01T21:30:00Z")).toBe("Aug 02, 2026, 01:00");
  });
});

describe("isValidTehranLocalInput", () => {
  it("accepts a well-formed datetime-local value", () => {
    expect(isValidTehranLocalInput("2026-08-01T17:30")).toBe(true);
  });

  it("accepts a well-formed value that also includes seconds", () => {
    expect(isValidTehranLocalInput("2026-08-01T17:30:15")).toBe(true);
  });

  it("rejects an empty string", () => {
    expect(isValidTehranLocalInput("")).toBe(false);
  });

  it("rejects a malformed string", () => {
    expect(isValidTehranLocalInput("not-a-date")).toBe(false);
  });

  it("rejects a calendrically impossible date", () => {
    expect(isValidTehranLocalInput("2026-02-30T10:00")).toBe(false);
  });

  it("rejects an out-of-range time", () => {
    expect(isValidTehranLocalInput("2026-08-01T25:00")).toBe(false);
  });
});

describe("fromTehranInput error handling", () => {
  it("throws on a malformed local date/time", () => {
    expect(() => fromTehranInput("not-a-date")).toThrow();
  });
});

describe("tehranDateKey / tehranDateLabel", () => {
  it("groups an instant by its Tehran-local calendar date, not its UTC date", () => {
    // 2026-08-02T01:00:00Z is 2026-08-02T04:30 Tehran — same UTC day, same Tehran day here.
    expect(tehranDateKey("2026-08-02T01:00:00Z")).toBe("2026-08-02");
    // 2026-08-01T21:30:00Z is 2026-08-02T01:00 Tehran — a day later in Tehran than in UTC.
    expect(tehranDateKey("2026-08-01T21:30:00Z")).toBe("2026-08-02");
  });

  it("formats a date key as a short human-readable label", () => {
    expect(tehranDateLabel("2026-08-01")).toBe("Sat, Aug 01");
  });
});

describe("todayInTehranLabel", () => {
  it("formats the given instant as a full Tehran-local date label", () => {
    // 2026-08-01T21:30:00Z is 2026-08-02T01:00 Tehran — a day later than the UTC date.
    expect(todayInTehranLabel(new Date("2026-08-01T21:30:00Z"))).toBe("Sunday, August 2, 2026");
  });
});
