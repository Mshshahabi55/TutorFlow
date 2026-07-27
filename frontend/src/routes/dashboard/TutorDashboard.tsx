import { useState } from "react";
import { Link as RouterLink, useNavigate } from "react-router-dom";
import { Button, Stack, Typography } from "@mui/material";
import { useTutorSchedule } from "@/features/scheduling/hooks/useSessionQueries";
import { useTutorAvailabilitySlots } from "@/features/scheduling/hooks/useAvailabilitySlotQueries";
import { byScheduledTimeAscending, byScheduledTimeDescending } from "@/features/scheduling/utils/sessionSort";
import { TutorSessionCard } from "@/features/scheduling/components/TutorSessionCard";
import { SessionCardSkeleton } from "@/features/scheduling/components/SessionCardSkeleton";
import { AvailabilitySummaryCard } from "@/features/scheduling/components/AvailabilitySummaryCard";
import { AvailabilitySummaryCardSkeleton } from "@/features/scheduling/components/AvailabilitySummaryCardSkeleton";
import { SectionCard } from "@/shared/components/SectionCard";
import { TeachingDayCard } from "@/routes/dashboard/TeachingDayCard";
import { PageHeader } from "@/shared/components/PageHeader";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { isTodayInTehran } from "@/shared/time/tehranTime";
import { ROLE_QUICK_ACTIONS } from "@/routes/dashboardRoleConfig";
import { SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

const RECENT_ACTIVITY_LIMIT = 3;
const UPCOMING_PREVIEW_LIMIT = 5;
const AVAILABILITY_PREVIEW_LIMIT = 3;

interface TeachingOverviewProps {
  tutorId: string;
}

/**
 * There is no "my own Tutor id" resolution from an authenticated Account
 * anywhere in this app (same structural gap the Student Dashboard
 * documented — ADR-011 remains frozen), so the overview below only
 * populates once a Tutor id is entered, reusing the exact `IdLookupForm`
 * pattern already used identically by `TutorSessionListPage`,
 * `SessionDetailPage`, `AvailabilitySlotDetailPage`, and
 * `DeclareAvailabilityPage` — not a new workflow, the same one applied to
 * one more page.
 */
function TeachingOverview({ tutorId }: TeachingOverviewProps) {
  const navigate = useNavigate();
  const scheduleQuery = useTutorSchedule(tutorId);
  const slotsQuery = useTutorAvailabilitySlots(tutorId);

  function openSession(session: SessionDto) {
    void navigate(paths.scheduling.sessionDetail(session.sessionId));
  }

  const sessions = scheduleQuery.data ?? [];
  const todaySessions = sessions.filter(
    (session) => session.status === SessionStatus.Scheduled && isTodayInTehran(session.scheduledTimeUtc),
  );
  const upcomingSessions = sessions
    .filter(
      (session) => session.status === SessionStatus.Scheduled && !isTodayInTehran(session.scheduledTimeUtc),
    )
    .sort(byScheduledTimeAscending)
    .slice(0, UPCOMING_PREVIEW_LIMIT);
  const recentActivity = sessions
    .filter((session) => session.status !== SessionStatus.Scheduled)
    .sort(byScheduledTimeDescending)
    .slice(0, RECENT_ACTIVITY_LIMIT);

  const openSlots = (slotsQuery.data ?? [])
    .filter((slot) => !slot.isConsumed)
    .sort((a, b) => Date.parse(a.startTimeUtc) - Date.parse(b.startTimeUtc))
    .slice(0, AVAILABILITY_PREVIEW_LIMIT);

  return (
    <Stack spacing={3}>
      {scheduleQuery.isPending ? (
        <Stack spacing={2}>
          {Array.from({ length: 2 }, (_, index) => (
            <SessionCardSkeleton key={index} />
          ))}
        </Stack>
      ) : scheduleQuery.isError ? (
        <ErrorState error={scheduleQuery.error} onRetry={() => void scheduleQuery.refetch()} />
      ) : (
        <>
          <TeachingDayCard todaySessions={todaySessions} onOpen={openSession} />

          <SectionCard
            title="Upcoming Sessions"
            action={
              <Button component={RouterLink} to={paths.scheduling.tutorSchedule(tutorId)} size="small">
                View all
              </Button>
            }
          >
            {upcomingSessions.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                No upcoming sessions beyond today.
              </Typography>
            ) : (
              <Stack spacing={2}>
                {upcomingSessions.map((session) => (
                  <TutorSessionCard key={session.sessionId} session={session} onOpen={openSession} />
                ))}
              </Stack>
            )}
          </SectionCard>

          <SectionCard
            title="Recent Activity"
            action={
              <Button component={RouterLink} to={paths.scheduling.tutorSchedule(tutorId)} size="small">
                View all
              </Button>
            }
          >
            {recentActivity.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                Nothing completed or cancelled yet.
              </Typography>
            ) : (
              <Stack spacing={2}>
                {recentActivity.map((session) => (
                  <TutorSessionCard key={session.sessionId} session={session} onOpen={openSession} />
                ))}
              </Stack>
            )}
          </SectionCard>
        </>
      )}

      <SectionCard
        title="Availability Summary"
        action={
          <Button component={RouterLink} to={paths.scheduling.declareAvailability} size="small">
            Manage availability
          </Button>
        }
      >
        {slotsQuery.isPending ? (
          <Stack spacing={2}>
            {Array.from({ length: 2 }, (_, index) => (
              <AvailabilitySummaryCardSkeleton key={index} />
            ))}
          </Stack>
        ) : slotsQuery.isError ? (
          <ErrorState error={slotsQuery.error} onRetry={() => void slotsQuery.refetch()} />
        ) : openSlots.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            No open Availability Slots right now.
          </Typography>
        ) : (
          <Stack spacing={2}>
            {openSlots.map((slot) => (
              <AvailabilitySummaryCard key={slot.availabilitySlotId} slot={slot} />
            ))}
          </Stack>
        )}
      </SectionCard>
    </Stack>
  );
}

/**
 * The Tutor's landing experience. Quick actions need no Tutor id and
 * always render; the teaching overview (Today's Sessions, Upcoming
 * Sessions, Recent Activity, Availability Summary) reuses `useTutorSchedule`
 * and `useTutorAvailabilitySlots` — the same hooks `TutorSessionListPage`
 * and `DeclareAvailabilityPage` already use — once a Tutor id is provided.
 */
export function TutorDashboard() {
  const [tutorId, setTutorId] = useState<string | undefined>(undefined);

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Welcome back"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Manage today&rsquo;s teaching, see what&rsquo;s next, and check your availability.
          </Typography>
        }
      />

      <SectionCard title="Quick actions">
        <Stack direction="row" flexWrap="wrap" gap={1.5}>
          {ROLE_QUICK_ACTIONS.Tutor.map((action) => (
            <Button
              key={action.to}
              component={RouterLink}
              to={action.to}
              variant="outlined"
              startIcon={action.icon}
            >
              {action.label}
            </Button>
          ))}
        </Stack>
      </SectionCard>

      {tutorId ? (
        <TeachingOverview tutorId={tutorId} />
      ) : (
        <SectionCard title="Your teaching overview">
          <Stack spacing={2} alignItems="flex-start">
            <Typography variant="body2" color="text.secondary">
              Enter your Tutor id to see today&rsquo;s sessions, upcoming sessions, recent activity,
              and your availability at a glance.
            </Typography>
            <IdLookupForm label="Tutor id" onSubmit={setTutorId} />
          </Stack>
        </SectionCard>
      )}
    </Stack>
  );
}
