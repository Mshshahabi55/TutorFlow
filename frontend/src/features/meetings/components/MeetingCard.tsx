import { Alert, Button, Skeleton, Stack, Typography } from "@mui/material";
import VideocamRoundedIcon from "@mui/icons-material/VideocamRounded";
import { useMeetingBySession } from "@/features/meetings/hooks/useMeetingQueries";
import { useStartMeeting } from "@/features/meetings/hooks/useMeetingMutations";
import { useNow } from "@/features/meetings/hooks/useNow";
import { deriveMeetingTiming } from "@/features/meetings/utils/meetingTiming";
import { ProviderBadge } from "@/features/meetings/components/ProviderBadge";
import { SectionCard } from "@/shared/components/SectionCard";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { ApiRequestError } from "@/services/api/ApiRequestError";
import { isNotFoundError } from "@/services/api/errorClassification";
import { useEffectiveRole } from "@/shared/hooks/useEffectiveRole";
import { DeliveryMode, MeetingStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

const PROVIDER_NOT_CONFIGURED_CODE = "CreateMeetingCommand.ProviderNotConfigured";

function isProviderNotConfiguredError(error: unknown): boolean {
  return error instanceof ApiRequestError && error.code === PROVIDER_NOT_CONFIGURED_CODE;
}

export interface MeetingCardProps {
  session: SessionDto;
}

/**
 * RC5.3 (docs/adr/ADR-023-...): only rendered for an Online Session — an
 * In-Person Session has nothing for this card to show (SCH-2).
 */
export function MeetingCard({ session }: MeetingCardProps) {
  if (session.deliveryMode !== DeliveryMode.Online) {
    return null;
  }

  return (
    <SectionCard title="Online Lesson">
      <MeetingCardContent session={session} />
    </SectionCard>
  );
}

function MeetingCardContent({ session }: { session: SessionDto }) {
  const role = useEffectiveRole();
  const meetingQuery = useMeetingBySession(session.sessionId);
  const startMeeting = useStartMeeting(session.sessionId);
  const now = useNow();

  if (meetingQuery.isLoading) {
    return (
      <Stack spacing={1}>
        <Skeleton variant="text" width="40%" height={32} />
        <Skeleton variant="rounded" width={160} height={36} />
      </Stack>
    );
  }

  if (meetingQuery.isError && !isNotFoundError(meetingQuery.error)) {
    return <ErrorState error={meetingQuery.error} onRetry={() => void meetingQuery.refetch()} />;
  }

  const meeting = meetingQuery.data;

  if (!meeting) {
    if (role !== "Tutor") {
      return (
        <EmptyState
          title="Lesson hasn't started yet"
          description="Your tutor hasn't started the online meeting yet. Check back closer to your lesson time."
        />
      );
    }

    return (
      <Stack spacing={2} alignItems="flex-start">
        <Typography variant="body2" color="text.secondary">
          No online meeting has been started for this lesson yet.
        </Typography>
        {startMeeting.isError && isProviderNotConfiguredError(startMeeting.error) ? (
          <Alert severity="warning" sx={{ width: "100%" }}>
            Provider not configured — ask an administrator to finish setup before starting this lesson.
          </Alert>
        ) : startMeeting.isError ? (
          <Alert severity="error" sx={{ width: "100%" }}>
            {startMeeting.error instanceof Error ? startMeeting.error.message : "Couldn't start the meeting."}
          </Alert>
        ) : null}
        <Button
          variant="contained"
          startIcon={<VideocamRoundedIcon />}
          onClick={() => startMeeting.mutate()}
          disabled={startMeeting.isPending}
        >
          Start Lesson
        </Button>
      </Stack>
    );
  }

  const isCancelled = meeting.status === MeetingStatus.Cancelled;
  const timing = deriveMeetingTiming(meeting.startsAtUtc, meeting.endsAtUtc, now);

  return (
    <Stack spacing={2}>
      <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}>
        <ProviderBadge provider={meeting.provider} />
        <StatusPill
          label={isCancelled ? "Cancelled" : timing.label}
          tone={isCancelled ? "critical" : timing.phase === "live" ? "success" : "info"}
        />
      </Stack>

      {isCancelled ? (
        <Typography variant="body2" color="text.secondary">
          This lesson&rsquo;s online meeting was cancelled.
        </Typography>
      ) : timing.phase === "ended" ? (
        // deriveMeetingTiming's own doc comment: "derives the Join/Start
        // button's live state" — this is that state actually being read,
        // matching JoinLessonBanner's own "hide once ended" behavior
        // rather than leaving a join link clickable indefinitely.
        <Typography variant="body2" color="text.secondary">
          This lesson has ended.
        </Typography>
      ) : role === "AdminStaff" ? null : (
        <Button
          component="a"
          href={role === "Tutor" ? (meeting.hostUrl ?? meeting.joinUrl) : meeting.joinUrl}
          target="_blank"
          rel="noopener noreferrer"
          variant="contained"
          startIcon={<VideocamRoundedIcon />}
          sx={{ alignSelf: "flex-start" }}
        >
          {role === "Tutor" ? "Start Lesson" : role === "ParentGuardian" ? "View Meeting" : "Join Lesson"}
        </Button>
      )}
    </Stack>
  );
}
