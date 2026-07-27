import type { ReactNode } from "react";
import { Card, CardContent, Stack, Typography } from "@mui/material";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { useRememberedId } from "@/shared/hooks/useRememberedId";

export interface IdentityGateProps {
  kind: "tutor" | "student" | "parentGuardian";
  /** The id field's accessible label, e.g. "Student id" — kept, not hidden, since this is still a real GUID entry the one time it's needed. */
  fieldLabel: string;
  title: string;
  description: string;
  children: (id: string) => ReactNode;
}

/**
 * RC2: replaces a bare id-lookup form with a warm, explained, one-time
 * setup step. There is still no "my own id" resolution from an
 * authenticated Account (ADR-011 frozen, no backend change in scope) —
 * this doesn't remove that real GUID entry, it removes having to repeat
 * it. `useRememberedId` persists it locally the first time, so every
 * screen using this gate (My Lessons, Manage Your Schedule, My Students,
 * Profile, each workspace Dashboard) only ever asks once per browser.
 */
export function IdentityGate({ kind, fieldLabel, title, description, children }: IdentityGateProps) {
  const { id, remember } = useRememberedId(kind);

  if (id) {
    return <>{children(id)}</>;
  }

  return (
    <Card variant="outlined">
      <CardContent>
        <Stack spacing={2} alignItems="flex-start">
          <Typography variant="h5" component="h2" fontWeight={600}>
            {title}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            {description}
          </Typography>
          <IdLookupForm label={fieldLabel} onSubmit={remember} />
        </Stack>
      </CardContent>
    </Card>
  );
}
