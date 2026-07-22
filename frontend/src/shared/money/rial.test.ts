import { describe, expect, it } from "vitest";
import {
  MAX_RIAL,
  MAX_TOMAN,
  formatToman,
  isValidTomanAmount,
  rialToToman,
  toTomanInputValue,
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
    // MAX_RIAL is itself a multiple of 10 (ADR-019 Addendum 1 / Phase
    // 4.5), so this is now exact with no remainder to account for.
    const rial = tomanToRial(MAX_TOMAN);
    expect(rial).toBe(MAX_RIAL);
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
    expect(formatToman(MAX_RIAL)).toBe(MAX_TOMAN.toLocaleString("en-US"));
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

  // As of Phase 4.5 (ADR-019 Addendum 1), HourlyRate.Of enforces
  // divisibility by 10 in Domain, so no conforming write can produce a
  // non-divisible Rial amount here — a value like 45 can only be legacy
  // data written before that invariant existed. rialToToman is the strict
  // primitive (a programmer-error guard, not UI-facing): it still throws
  // rather than rounding one. See "safe display functions" below for the
  // page-facing functions, which must never throw on this same input.
  it("throws rather than rounding when the Rial amount is not divisible by 10", () => {
    expect(() => rialToToman(45)).toThrow(/not evenly divisible/);
  });
});

describe("safe display functions never throw on a legacy non-divisible amount", () => {
  // 45 Rial predates the Phase 4.5 Domain invariant and has no exact
  // Toman representation (4.5) — formatToman/toTomanInputValue must
  // degrade (round) rather than crash any page that renders it, since a
  // Tutor's whole profile page must not become permanently unviewable
  // over a display nicety. The stored Rial amount itself is never altered.
  it("formatToman rounds instead of throwing", () => {
    expect(() => formatToman(45)).not.toThrow();
    expect(formatToman(45)).toBe("5"); // 4.5 rounds to 5
  });

  it("toTomanInputValue rounds instead of throwing", () => {
    expect(() => toTomanInputValue(45)).not.toThrow();
    expect(toTomanInputValue(45)).toBe("5");
  });

  it("rounds down when the fractional Toman part is below .5", () => {
    expect(formatToman(44)).toBe("4"); // 4.4 rounds to 4
  });

  it("still throws for a genuinely invalid amount (negative/out-of-range), not just non-divisible", () => {
    expect(() => formatToman(-10)).toThrow();
    expect(() => formatToman(MAX_RIAL + 10)).toThrow();
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
