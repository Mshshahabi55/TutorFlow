import { Box, Typography } from "@mui/material";
import type { ReactNode } from "react";

export interface EmptyStateProps {
  title: string;
  description?: string;
  action?: ReactNode;
}

/** A consistent "nothing here yet" placeholder for any list-backed view. */
export function EmptyState({ title, description, action }: EmptyStateProps) {
  return (
    <Box
      display="flex"
      flexDirection="column"
      alignItems="center"
      justifyContent="center"
      gap={1}
      textAlign="center"
      py={6}
      px={2}
    >
      <Typography variant="subtitle1" fontWeight={600}>
        {title}
      </Typography>
      {description ? (
        <Typography variant="body2" color="text.secondary" maxWidth={420}>
          {description}
        </Typography>
      ) : null}
      {action}
    </Box>
  );
}
