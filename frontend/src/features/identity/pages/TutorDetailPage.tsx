import { Link as RouterLink, useParams } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { useTutor } from "@/features/identity/hooks/useTutorQueries";
import { TutorApprovalActions } from "@/features/identity/components/TutorApprovalActions";
import { formatMinutesList } from "@/shared/utils/duration";
import { formatToman } from "@/shared/money/rial";
import { PageHeader } from "@/shared/components/PageHeader";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { useEffectiveRole } from "@/shared/hooks/useEffectiveRole";
import { paths } from "@/routes/paths";
import type { TutorDto } from "@/services/api/dtos";

/**
 * Phase 4.9 Task 4: Edit offering (Tutor-only, `ManageTutorOffering`) and
 * Approve/Suspend (Admin-only, `ApproveTutor`/`SuspendTutor`) previously
 * rendered unconditionally for every viewer — the literal "a Student sees
 * tutor Approve/Suspend, admin actions" live-browser finding this phase's
 * brief reported. Gated on the same `useEffectiveRole` signal NavSidebar and
 * the route guard already use, not a new one. A Student/Parent-Guardian
 * viewer gets no action here — booking itself is a separate, later phase
 * this one is a prerequisite for, not yet wired to this page.
 */
function TutorDetailActions({ tutor }: { tutor: TutorDto }) {
  const role = useEffectiveRole();

  if (role === "AdminStaff") {
    return <TutorApprovalActions tutor={tutor} />;
  }

  if (role === "Tutor") {
    return (
      <Button
        component={RouterLink}
        to={paths.identity.tutorEdit(tutor.tutorId)}
        variant="contained"
        sx={{ alignSelf: "flex-start" }}
      >
        Edit offering
      </Button>
    );
  }

  return null;
}

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
                  {tutorQuery.data.hourlyRate !== null
                    ? `${formatToman(tutorQuery.data.hourlyRate)} Toman`
                    : "Not set"}
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

              <TutorDetailActions tutor={tutorQuery.data} />
            </Stack>
          ) : null}
        </CardContent>
      </Card>
    </Stack>
  );
}
