import { Box, Typography, alpha } from "@mui/material";
import InboxRounded from "@mui/icons-material/InboxRounded";
import type { ReactNode } from "react";

export interface EmptyStateProps {
  title: string;
  description?: string;
  action?: ReactNode;
  /** A small icon rendered inside the illustration circle. Defaults to a generic inbox glyph — no per-context icon exists for most of this component's 20+ call sites, so a single friendly default reads better than nothing. */
  icon?: ReactNode;
}

/** A consistent "nothing here yet" placeholder for any list-backed view. */
export function EmptyState({ title, description, action, icon }: EmptyStateProps) {
  return (
    <Box
      display="flex"
      flexDirection="column"
      alignItems="center"
      justifyContent="center"
      gap={2}
      textAlign="center"
      py={8}
      px={3}
    >
      <Box
        sx={{
          width: 88,
          height: 88,
          borderRadius: "50%",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          bgcolor: (t) => alpha(t.palette.primary.main, t.palette.mode === "dark" ? 0.18 : 0.1),
          color: "primary.main",
          "& .MuiSvgIcon-root": { fontSize: 40 },
        }}
        aria-hidden="true"
      >
        {icon ?? <InboxRounded fontSize="inherit" />}
      </Box>
      <Typography variant="h5">{title}</Typography>
      {description ? (
        <Typography variant="body1" color="text.secondary" maxWidth={440}>
          {description}
        </Typography>
      ) : null}
      {action ? <Box mt={1}>{action}</Box> : null}
    </Box>
  );
}
