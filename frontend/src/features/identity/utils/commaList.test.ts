import { describe, expect, it } from "vitest";
import { formatCommaList, parseCommaList } from "@/features/identity/utils/commaList";

describe("parseCommaList", () => {
  it("splits, trims, and drops empty entries", () => {
    expect(parseCommaList("French, German,  , Spanish")).toEqual(["French", "German", "Spanish"]);
  });

  it("returns an empty array for a blank string", () => {
    expect(parseCommaList("")).toEqual([]);
  });
});

describe("formatCommaList", () => {
  it("joins with a comma and space", () => {
    expect(formatCommaList(["French", "German"])).toBe("French, German");
  });

  it("returns an empty string for an empty array", () => {
    expect(formatCommaList([])).toBe("");
  });
});
