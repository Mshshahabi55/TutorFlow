import { describe, expect, it } from "vitest";
import {
  MAX_RIAL,
  MAX_TOMAN,
  formatToman,
  isValidTomanAmount,
  rialToToman,
  tomanToRial,
} from "@/shared/money/rial";

describe("tomanToRial / rialToToman — exact round-tripping", () => {
  it.each([0, 1, 45_000, 500_000, 1_234_567])(
    "round-trips %i Toman through Rial and back exactly",
    (toman) => {
      const rial = tomanToRial(toman);
      expect(rial).toBe(toman * 10);
      expect(rialToToman(rial)).toBe(toman);
    },
  );

  it("round-trips the largest value the schema allows", () => {
    const rial = tomanToRial(MAX_TOMAN);
    expect(rial).toBe(MAX_RIAL - (MAX_RIAL % 10));
    expect(rial).toBeLessThanOrEqual(MAX_RIAL);
    expect(rialToToman(rial)).toBe(MAX_TOMAN);
  });
});

describe("zero", () => {
  it("tomanToRial(0) is 0", () => {
    expect(tomanToRial(0)).toBe(0);
  });

  it("rialToToman(0) is 0", () => {
    expect(rialToToman(0)).toBe(0);
  });

  it("formatToman(0) is \"0\"", () => {
    expect(formatToman(0)).toBe("0");
  });
});

describe("formatToman — thousand-separator formatting", () => {
  it("formats a small amount with no separator", () => {
    expect(formatToman(450)).toBe("45");
  });

  it("formats a six-figure Toman amount with thousand separators", () => {
    expect(formatToman(500_000)).toBe("50,000");
  });

  it("formats a seven-figure Toman amount with thousand separators", () => {
    expect(formatToman(12_345_670)).toBe("1,234,567");
  });

  it("formats the largest value the schema allows with thousand separators", () => {
    const maxRialAsToman = tomanToRial(MAX_TOMAN);
    expect(formatToman(maxRialAsToman)).toBe(MAX_TOMAN.toLocaleString("en-US"));
  });
});

describe("tomanToRial validation", () => {
  it("rejects a negative amount", () => {
    expect(() => tomanToRial(-1)).toThrow();
  });

  it("rejects a fractional amount", () => {
    expect(() => tomanToRial(1.5)).toThrow();
  });

  it("rejects an amount exceeding MAX_TOMAN", () => {
    expect(() => tomanToRial(MAX_TOMAN + 1)).toThrow();
  });
});

describe("rialToToman validation", () => {
  it("rejects a negative amount", () => {
    expect(() => rialToToman(-10)).toThrow();
  });

  it("rejects a fractional amount", () => {
    expect(() => rialToToman(10.5)).toThrow();
  });

  it("rejects an amount exceeding MAX_RIAL", () => {
    expect(() => rialToToman(MAX_RIAL + 1)).toThrow();
  });

  // The path this phase's absolute rule requires flagging rather than
  // silently resolving: a Rial amount not evenly divisible by 10 cannot be
  // expressed as a whole Toman value. This can only happen via a raw API
  // write bypassing the Toman-only UI (every write this product makes goes
  // through tomanToRial, always a multiple of 10) — reported explicitly in
  // docs/phases/PHASE-04-REPORT.md Section 4, not silently rounded here.
  it("throws rather than rounding when the Rial amount is not divisible by 10", () => {
    expect(() => rialToToman(45)).toThrow(/not evenly divisible/);
  });

  it("throws for the true schema maximum (999,999,999,999), which is not divisible by 10", () => {
    expect(MAX_RIAL % 10).not.toBe(0);
    expect(() => rialToToman(MAX_RIAL)).toThrow(/not evenly divisible/);
  });
});

describe("isValidTomanAmount", () => {
  it("accepts a positive whole number", () => {
    expect(isValidTomanAmount("50000")).toBe(true);
  });

  it("accepts the largest value the schema allows", () => {
    expect(isValidTomanAmount(String(MAX_TOMAN))).toBe(true);
  });

  it("rejects zero (an hourly rate must be positive)", () => {
    expect(isValidTomanAmount("0")).toBe(false);
  });

  it("rejects a negative number", () => {
    expect(isValidTomanAmount("-5")).toBe(false);
  });

  it("rejects a fractional number", () => {
    expect(isValidTomanAmount("50.5")).toBe(false);
  });

  it("rejects a value exceeding MAX_TOMAN", () => {
    expect(isValidTomanAmount(String(MAX_TOMAN + 1))).toBe(false);
  });

  it("rejects non-numeric input", () => {
    expect(isValidTomanAmount("not-a-number")).toBe(false);
  });

  it("rejects an empty string", () => {
    expect(isValidTomanAmount("")).toBe(false);
  });
});
