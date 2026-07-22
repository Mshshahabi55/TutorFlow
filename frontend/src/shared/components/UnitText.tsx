import { Typography } from "@mui/material";
import type { ReactNode } from "react";

export interface UnitTextProps {
  /** The number/date/time itself — the thing the unit qualifies. */
  value: ReactNode;
  /** The unit name, e.g. "Tehran" or "Toman" — rendered, never inferred. */
  unit: string;
}

/**
 * The one typographic treatment for every unit-bearing value in the app —
 * a Tehran time or a Toman amount. Phase D1 (Design System Foundation)
 * audit found six different ad hoc renderings of "(Tehran)"/"(Toman)"
 * across pages (a table header suffix, a bold inline label, a parenthetical
 * after the value, a field label) with no shared component behind any of
 * them. This is that shared component, ready for pages to adopt from D2
 * onward — introducing it does not itself change any page (Phase D1 does
 * not restyle pages).
 */
export function UnitText({ value, unit }: UnitTextProps) {
  return (
    <Typography component="span" sx={{ fontVariantNumeric: "tabular-nums" }}>
      {value}
      <Typography component="span" variant="caption" color="text.secondary" sx={{ ml: 0.5 }}>
        {unit}
      </Typography>
    </Typography>
  );
}
