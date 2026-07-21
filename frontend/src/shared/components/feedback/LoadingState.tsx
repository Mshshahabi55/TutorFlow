import { Box, CircularProgress, Typography } from "@mui/material";

export interface LoadingStateProps {
  label?: string;
  minHeight?: number | string;
}

/** A consistent loading placeholder for any query-backed view. */
export function LoadingState({ label = "Loading…", minHeight = 160 }: LoadingStateProps) {
  return (
    <Box
      role="status"
      aria-live="polite"
      display="flex"
      flexDirection="column"
      alignItems="center"
      justifyContent="center"
      gap={1.5}
      minHeight={minHeight}
      width="100%"
      py={4}
    >
      <CircularProgress size={32} />
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
    </Box>
  );
}
