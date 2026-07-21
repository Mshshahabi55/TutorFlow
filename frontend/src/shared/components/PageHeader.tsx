import { Box, Typography } from "@mui/material";
import type { ReactNode } from "react";

export interface PageHeaderProps {
  title: string;
  /** Whatever belongs under the title — a plain description, an id line, or nothing. Left as a node rather than a string so each page keeps its own subtitle styling (a description reads differently from an id). */
  subtitle?: ReactNode;
  /** An optional action rendered top-right (e.g. a primary "Register" link) — helps a page announce its own next step instead of relying solely on navigation. */
  action?: ReactNode;
}

/**
 * The one page-title block every page in the app was independently
 * re-typing (Product Polish phase: Design System audit found the exact
 * same `<Typography variant="h4" fontWeight={700} gutterBottom>` markup
 * duplicated across 20 pages). Standardizes the title's typography once,
 * here, instead of per page.
 */
export function PageHeader({ title, subtitle, action }: PageHeaderProps) {
  return (
    <Box display="flex" alignItems="flex-start" justifyContent="space-between" flexWrap="wrap" gap={2}>
      <Box minWidth={0}>
        <Typography variant="h4" gutterBottom={Boolean(subtitle)}>
          {title}
        </Typography>
        {subtitle}
      </Box>
      {action ? <Box flexShrink={0}>{action}</Box> : null}
    </Box>
  );
}
