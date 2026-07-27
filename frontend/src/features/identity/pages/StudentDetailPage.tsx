import { useNavigate, useParams } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { useStudent } from "@/features/identity/hooks/useStudentQueries";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { PageHeader } from "@/shared/components/PageHeader";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { paths } from "@/routes/paths";

/**
 * No "list every Student" capability exists at the API boundary (only
 * GET /students/{id}) — this page is the honest substitute for a directory:
 * look a Student up by id, then view their detail.
 */
export function StudentDetailPage() {
  const { studentId } = useParams<{ studentId: string }>();
  const navigate = useNavigate();
  const studentQuery = useStudent(studentId);

  return (
    <Stack spacing={3} maxWidth={560}>
      <PageHeader
        title="Student detail"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            There is no Student directory — enter a Student id to look one up.
          </Typography>
        }
      />

      {!studentId ? (
        <IdLookupForm
          label="Student id"
          onSubmit={(id) => {
            void navigate(paths.identity.studentDetail(id));
          }}
        />
      ) : (
        <Card variant="outlined">
          <CardContent>
            {studentQuery.isPending ? <LoadingState label="Loading Student…" /> : null}
            {studentQuery.isError ? (
              <Stack spacing={2} alignItems="flex-start">
                <Typography variant="subtitle1" fontWeight={600}>
                  We couldn&rsquo;t find that student
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Double-check the id, or try again.
                </Typography>
                <Button variant="outlined" onClick={() => void studentQuery.refetch()}>
                  Try again
                </Button>
              </Stack>
            ) : null}
            {studentQuery.isSuccess ? (
              <Stack spacing={1.5}>
                <Typography variant="body2" fontFamily="ui-monospace, monospace">
                  {studentQuery.data.studentId}
                </Typography>
                <StatusPill
                  label={studentQuery.data.isMinor ? "Minor" : "Adult"}
                  tone={studentQuery.data.isMinor ? "warning" : "neutral"}
                />
              </Stack>
            ) : null}
          </CardContent>
        </Card>
      )}
    </Stack>
  );
}
