import { useNavigate, useParams } from "react-router-dom";
import { Card, CardContent, Stack, Typography } from "@mui/material";
import { useParentGuardian } from "@/features/identity/hooks/useParentGuardianQueries";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { PageHeader } from "@/shared/components/PageHeader";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { paths } from "@/routes/paths";

/**
 * No "list every Parent/Guardian" capability exists at the API boundary
 * (only GET /parent-guardians/{id}) — this page is the honest substitute
 * for a directory: look one up by id, then view their detail.
 */
export function ParentGuardianDetailPage() {
  const { parentGuardianId } = useParams<{ parentGuardianId: string }>();
  const navigate = useNavigate();
  const parentGuardianQuery = useParentGuardian(parentGuardianId);

  return (
    <Stack spacing={3} maxWidth={560}>
      <PageHeader
        title="Parent/Guardian detail"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            There is no Parent/Guardian directory — enter an id to look one up.
          </Typography>
        }
      />

      {!parentGuardianId ? (
        <IdLookupForm
          label="Parent/Guardian id"
          onSubmit={(id) => {
            void navigate(paths.identity.parentGuardianDetail(id));
          }}
        />
      ) : (
        <Card variant="outlined">
          <CardContent>
            {parentGuardianQuery.isPending ? (
              <LoadingState label="Loading Parent/Guardian…" />
            ) : null}
            {parentGuardianQuery.isError ? (
              <ErrorState
                error={parentGuardianQuery.error}
                onRetry={() => void parentGuardianQuery.refetch()}
              />
            ) : null}
            {parentGuardianQuery.isSuccess ? (
              <Typography variant="body2" fontFamily="ui-monospace, monospace">
                {parentGuardianQuery.data.parentGuardianId}
              </Typography>
            ) : null}
          </CardContent>
        </Card>
      )}
    </Stack>
  );
}
