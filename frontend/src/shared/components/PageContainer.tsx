import { Box } from "@mui/material";
import type { ReactNode } from "react";

/**
 * Phase D4: extracted verbatim from `AppLayout`'s own inline content
 * wrapper (Phase D2) — same 1200px reading-width bound, same responsive
 * padding, no visual change. Its own component now so the shell (header,
 * sidebar) can be restyled around it without also touching this.
 */
const CONTENT_MAX_WIDTH = 1200;

export function PageContainer({ children }: { children: ReactNode }) {
  return (
    <Box
      sx={{
        maxWidth: CONTENT_MAX_WIDTH,
        mx: "auto",
        px: { xs: 2, sm: 3, md: 4 },
        py: { xs: 3, sm: 4 },
      }}
    >
      {children}
    </Box>
  );
}
