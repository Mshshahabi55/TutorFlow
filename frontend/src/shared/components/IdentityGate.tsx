import type { ReactNode } from "react";
import { Card, CardContent, Stack, Typography } from "@mui/material";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { UnavailableState } from "@/shared/components/feedback/UnavailableState";
import { useOwnId, type OwnIdKind } from "@/shared/hooks/useOwnId";
import { paths } from "@/routes/paths";

export interface IdentityGateProps {
  kind: OwnIdKind;
  /** The id field's accessible label, e.g. "Student id" — only ever shown to the dev-only "Acting as" preview, which has no real Account to resolve against; a real signed-in user never sees it. */
  fieldLabel: string;
  title: string;
  description: string;
  children: (id: string, forget: () => void) => ReactNode;
}

/**
 * A real signed-in Account IS the Tutor/Student/Parent-Guardian record
 * (`useOwnId`, `docs/adr/ADR-017-authentication-mechanism-decision.md`), so
 * this never asks a signed-in user for an id — it resolves automatically.
 * The id-entry form below only ever renders for the dev-only "Acting as"
 * preview (no real session to resolve against); every screen using this
 * gate (My Lessons, Manage Your Schedule, My Students, Profile, each
 * workspace Dashboard) is unaffected either way.
 */
export function IdentityGate({ kind, fieldLabel, title, description, children }: IdentityGateProps) {
  const { id, isRoleMismatch, remember, forget } = useOwnId(kind);

  if (id) {
    return <>{children(id, forget)}</>;
  }

  if (isRoleMismatch) {
    return (
      <UnavailableState
        title="We couldn't load your profile"
        description="Your signed-in account doesn't match this view."
        actions={[{ label: "Go to Home", to: paths.home, variant: "contained" }]}
        headingComponent="h2"
      />
    );
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
