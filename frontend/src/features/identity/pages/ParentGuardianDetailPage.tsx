import { Link as RouterLink, useNavigate, useParams } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { useParentGuardian } from "@/features/identity/hooks/useParentGuardianQueries";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { PageHeader } from "@/shared/components/PageHeader";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
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
              <Stack spacing={2} alignItems="flex-start">
                <Typography variant="subtitle1" fontWeight={600}>
                  Parent/Guardian unavailable
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  This Parent/Guardian profile is unavailable. Double-check the id, or try again.
                </Typography>
                <Stack direction="row" spacing={1.5} flexWrap="wrap">
                  <Button variant="outlined" onClick={() => void parentGuardianQuery.refetch()}>
                    Try again
                  </Button>
                  <Button component={RouterLink} to={paths.home} variant="contained">
                    Back to Dashboard
                  </Button>
                </Stack>
              </Stack>
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
