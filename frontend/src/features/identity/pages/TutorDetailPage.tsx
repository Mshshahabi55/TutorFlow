import { Link as RouterLink, useParams } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { useTutor } from "@/features/identity/hooks/useTutorQueries";
import { TutorApprovalActions } from "@/features/identity/components/TutorApprovalActions";
import { formatMinutesList } from "@/shared/utils/duration";
import { PageHeader } from "@/shared/components/PageHeader";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { paths } from "@/routes/paths";

export function TutorDetailPage() {
  const { tutorId } = useParams<{ tutorId: string }>();
  const tutorQuery = useTutor(tutorId);

  return (
    <Stack spacing={3} maxWidth={640}>
      <PageHeader
        title="Tutor detail"
        subtitle={
          <Typography variant="body2" color="text.secondary" fontFamily="ui-monospace, monospace">
            {tutorId}
          </Typography>
        }
      />

      <Card variant="outlined">
        <CardContent>
          {tutorQuery.isPending ? <LoadingState label="Loading Tutor…" /> : null}
          {tutorQuery.isError ? (
            <ErrorState error={tutorQuery.error} onRetry={() => void tutorQuery.refetch()} />
          ) : null}
          {tutorQuery.isSuccess ? (
            <Stack spacing={2}>
              <Stack direction="row" spacing={1} flexWrap="wrap">
                <StatusPill
                  label={tutorQuery.data.isApproved ? "Approved" : "Pending approval"}
                  tone={tutorQuery.data.isApproved ? "success" : "warning"}
                />
                <StatusPill
                  label={tutorQuery.data.isSuspended ? "Suspended" : "Not suspended"}
                  tone={tutorQuery.data.isSuspended ? "critical" : "neutral"}
                />
                <StatusPill
                  label={tutorQuery.data.isDiscoverable ? "Discoverable" : "Not discoverable"}
                  tone={tutorQuery.data.isDiscoverable ? "info" : "neutral"}
                />
              </Stack>

              <Stack spacing={1}>
                <Typography variant="body2">
                  <b>Hourly rate:</b>{" "}
                  {tutorQuery.data.hourlyRate !== null ? tutorQuery.data.hourlyRate : "Not set"}
                </Typography>
                <Typography variant="body2">
                  <b>Subject:</b> {tutorQuery.data.subject ?? "Not set"}
                </Typography>
                <Typography variant="body2">
                  <b>Language:</b> {tutorQuery.data.language ?? "Not set"}
                </Typography>
                <Typography variant="body2">
                  <b>Location:</b> {tutorQuery.data.location ?? "Not set"}
                </Typography>
                <Typography variant="body2">
                  <b>Offered durations (minutes):</b>{" "}
                  {tutorQuery.data.offeredDurations.length > 0
                    ? formatMinutesList(tutorQuery.data.offeredDurations)
                    : "Not set"}
                </Typography>
              </Stack>

              <Button
                component={RouterLink}
                to={paths.identity.tutorEdit(tutorQuery.data.tutorId)}
                variant="contained"
                sx={{ alignSelf: "flex-start" }}
              >
                Edit offering
              </Button>

              <TutorApprovalActions tutor={tutorQuery.data} />
            </Stack>
          ) : null}
        </CardContent>
      </Card>
    </Stack>
  );
}
