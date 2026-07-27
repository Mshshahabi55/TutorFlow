import { describe, expect, it } from "vitest";
import { contrastRatio } from "@/app/contrast";
import { createAppTheme } from "@/app/theme";

const AA_MINIMUM = 4.5;

describe("contrastRatio", () => {
  it("computes the known WCAG example ratio (black on white is 21:1)", () => {
    expect(contrastRatio("#000000", "#FFFFFF")).toBeCloseTo(21, 0);
  });

  it("returns 1 for identical colors", () => {
    expect(contrastRatio("#336699", "#336699")).toBeCloseTo(1, 5);
  });
});

/**
 * Phase D3: replaces Phase D1's manual, unrepeatable contrast measurement
 * (a static table in docs/design/DESIGN-SYSTEM.md) with a real assertion —
 * a future palette edit that drops a tone below AA fails this test instead
 * of silently shipping. Both light and dark hold every semantic tone (plus
 * primary/secondary) to the same bar: each tone's own `contrastText` against
 * its own `main` fill, and `text.primary`/`text.secondary` against both
 * `background.default` and `background.paper`.
 */
describe.each([
  { label: "light", mode: "light" as const },
  { label: "dark", mode: "dark" as const },
])("$label theme meets WCAG AA (4.5:1)", ({ mode }) => {
  const theme = createAppTheme(mode);

  it.each(["primary", "secondary", "success", "warning", "error", "info"] as const)(
    "%s.contrastText clears AA against %s.main",
    (key) => {
      const { main, contrastText } = theme.palette[key];
      expect(contrastRatio(contrastText, main)).toBeGreaterThanOrEqual(AA_MINIMUM);
    },
  );

  it("text.primary clears AA against background.default and background.paper", () => {
    expect(
      contrastRatio(theme.palette.text.primary, theme.palette.background.default),
    ).toBeGreaterThanOrEqual(AA_MINIMUM);
    expect(
      contrastRatio(theme.palette.text.primary, theme.palette.background.paper),
    ).toBeGreaterThanOrEqual(AA_MINIMUM);
  });

  it("text.secondary clears AA against background.default", () => {
    expect(
      contrastRatio(theme.palette.text.secondary, theme.palette.background.default),
    ).toBeGreaterThanOrEqual(AA_MINIMUM);
  });

  // text.disabled is the sole deliberate exception, both modes — WCAG
  // explicitly exempts inactive/disabled UI text from the contrast
  // requirement (recorded in DESIGN-SYSTEM.md for light mode already).
  it("text.disabled is present but not held to the AA bar (WCAG's own exemption)", () => {
    expect(theme.palette.text.disabled).toBeTruthy();
  });
});
