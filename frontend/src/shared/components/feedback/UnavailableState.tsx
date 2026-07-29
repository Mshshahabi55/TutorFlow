import type { ReactNode } from "react";
import { Link as RouterLink } from "react-router-dom";
import { Button, Stack, Typography } from "@mui/material";

export interface UnavailableStateAction {
  label: string;
  to?: string;
  onClick?: () => void;
  variant?: "contained" | "outlined";
  icon?: ReactNode;
}

export interface UnavailableStateProps {
  title: string;
  description: string;
  actions: UnavailableStateAction[];
  /** Defaults to "h1" — a full-page replacement. Pass "h2" when nested inside a page that already has its own h1. */
  headingComponent?: "h1" | "h2";
}

/**
 * The shared "this entity could not be resolved" placeholder — the same
 * shape `SessionDetailPage`/`TutorDetailPage` each independently
 * established (a heading, a plain-language description, one or more
 * recovery actions, never the backend's own wording) — so every
 * entity-by-id lookup failure reads and behaves consistently instead of
 * each page inventing its own copy and layout.
 */
export function UnavailableState({
  title,
  description,
  actions,
  headingComponent = "h1",
}: UnavailableStateProps) {
  return (
    <Stack spacing={2} alignItems="center" textAlign="center" py={6} maxWidth={480} mx="auto">
      <Typography variant="h4" component={headingComponent}>
        {title}
      </Typography>
      <Typography variant="body1" color="text.secondary">
        {description}
      </Typography>
      <Stack direction="row" spacing={1.5} mt={1} flexWrap="wrap" justifyContent="center">
        {actions.map((action) =>
          action.to ? (
            <Button
              key={action.label}
              component={RouterLink}
              to={action.to}
              variant={action.variant ?? "outlined"}
              startIcon={action.icon}
            >
              {action.label}
            </Button>
          ) : (
            <Button
              key={action.label}
              variant={action.variant ?? "outlined"}
              onClick={action.onClick}
              startIcon={action.icon}
            >
              {action.label}
            </Button>
          ),
        )}
      </Stack>
    </Stack>
  );
}
