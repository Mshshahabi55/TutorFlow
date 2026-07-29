import { Link as RouterLink, useNavigate } from "react-router-dom";
import { Box, Button, Stack, Typography } from "@mui/material";
import LightbulbRoundedIcon from "@mui/icons-material/LightbulbRounded";
import EventAvailableRoundedIcon from "@mui/icons-material/EventAvailableRounded";
import { useTutor } from "@/features/identity/hooks/useTutorQueries";
import { useTutorSchedule } from "@/features/scheduling/hooks/useSessionQueries";
import { useTutorAvailabilitySlots } from "@/features/scheduling/hooks/useAvailabilitySlotQueries";
import { byScheduledTimeAscending, byScheduledTimeDescending } from "@/features/scheduling/utils/sessionSort";
import { deriveStudentRoster } from "@/features/scheduling/utils/studentRoster";
import { TutorSessionCard } from "@/features/scheduling/components/TutorSessionCard";
import { StudentRosterCard } from "@/features/scheduling/components/StudentRosterCard";
import { SessionCardSkeleton } from "@/features/scheduling/components/SessionCardSkeleton";
import { SessionStatusBreakdownChart } from "@/features/scheduling/components/SessionStatusBreakdownChart";
import { deriveStatusCounts } from "@/features/scheduling/utils/sessionStatusCounts";
import { AvailabilitySummaryCard } from "@/features/scheduling/components/AvailabilitySummaryCard";
import { AvailabilitySummaryCardSkeleton } from "@/features/scheduling/components/AvailabilitySummaryCardSkeleton";
import { ProfileCompletionCard } from "@/features/identity/components/ProfileCompletionCard";
import { RecentConversationsSection } from "@/features/communication/components/RecentConversationsSection";
import { deriveProfileCompletion } from "@/features/identity/utils/profileCompletion";
import { SectionCard } from "@/shared/components/SectionCard";
import { LearningPlanCard } from "@/features/learningPlans/components/LearningPlanCard";
import { TeachingDayCard } from "@/routes/dashboard/TeachingDayCard";
import { NextLessonHeroCard } from "@/routes/dashboard/NextLessonHeroCard";
import { PageHeader } from "@/shared/components/PageHeader";
import { IdentityGate } from "@/shared/components/IdentityGate";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { IdentityLookupErrorState } from "@/shared/components/feedback/IdentityLookupErrorState";
import { isTodayInTehran, todayInTehranLabel } from "@/shared/time/tehranTime";
import { ROLE_QUICK_ACTIONS } from "@/routes/dashboardRoleConfig";
import { SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";
import type { LearningPlanPreview } from "@/features/learningPlans/types";
import { paths } from "@/routes/paths";

const RECENT_ACTIVITY_LIMIT = 3;
const UPCOMING_PREVIEW_LIMIT = 5;
const AVAILABILITY_PREVIEW_LIMIT = 3;
const ATTENTION_LIMIT = 3;

interface TeachingOverviewProps {
  tutorId: string;
  onChooseAgain: () => void;
}

function TeachingSummaryStat({ label, value }: { label: string; value: number }) {
  return (
    <Box flex={1} minWidth={120}>
      <Typography variant="h4" component="p" fontWeight={700}>
        {value}
      </Typography>
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
    </Box>
  );
}

/**
 * RC5.0: "My Learning Plans" — no Learning Plan/Enrollment capability
 * exists in this API version yet (`docs/adr/ADR-021...`, Proposed, not
 * Accepted). `plans` is always empty today; the counts below are real
 * (zero, not fabricated) rather than invented numbers, and the section
 * lights up with real `LearningPlanCard`s the moment a real endpoint
 * exists — no structural change needed here.
 */
function MyLearningPlansSection() {
  const plans: LearningPlanPreview[] = [];
  const activeCount = plans.filter((plan) => plan.status === "Active").length;
  const draftCount = plans.filter((plan) => plan.status === "Draft").length;
  const archivedCount = plans.filter((plan) => plan.status === "Archived").length;
  const enrollmentCount = 0;

  return (
    <SectionCard title="My Learning Plans">
      <Stack spacing={2}>
        <Stack direction="row" flexWrap="wrap" gap={3}>
          <TeachingSummaryStat label="Active Plans" value={activeCount} />
          <TeachingSummaryStat label="Draft Plans" value={draftCount} />
          <TeachingSummaryStat label="Archived Plans" value={archivedCount} />
          <TeachingSummaryStat label="Enrollments" value={enrollmentCount} />
        </Stack>
        {plans.length === 0 ? (
          <EmptyState
            title="No learning plans yet"
            description="Structured, multi-week Learning Plans are coming soon — for now, Students book individual lessons with you directly."
          />
        ) : (
          <Stack direction="row" flexWrap="wrap" gap={2}>
            {plans.map((plan) => (
              <LearningPlanCard key={plan.learningPlanId} plan={plan} />
            ))}
          </Stack>
        )}
      </Stack>
    </SectionCard>
  );
}

/**
 * A real signed-in Tutor's own id resolves automatically (`IdentityGate`,
 * now backed by `useOwnId`) — only the dev-only "Acting as" preview (no
 * real session) is ever asked for it, once per device. Reuses `useTutor`,
 * `useTutorSchedule`, and
 * `useTutorAvailabilitySlots` — the same three hooks `TutorDetailPage`,
 * `TutorSessionListPage`, `TutorStudentsPage`, and `DeclareAvailabilityPage`
 * already fetch — React Query's cache means visiting more than one of
 * these pages in a session never re-requests the same data twice.
 */
function TeachingOverview({ tutorId, onChooseAgain }: TeachingOverviewProps) {
  const navigate = useNavigate();
  const tutorQuery = useTutor(tutorId);
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
  const nextLesson = sessions
    .filter((session) => session.status === SessionStatus.Scheduled)
    .sort(byScheduledTimeAscending)[0];

  // "Requiring attention": this Student's most recent lesson didn't go as
  // scheduled (Cancelled/No-Show) and nothing new is booked yet — a real,
  // honest signal derived entirely from statuses already in the Tutor's
  // own schedule, not a fabricated "at risk" score.
  const roster = deriveStudentRoster(sessions);
  const studentsRequiringAttention = roster
    .filter(
      (entry) =>
        entry.mostRecentPastSession &&
        (entry.mostRecentPastSession.status === SessionStatus.Cancelled ||
          entry.mostRecentPastSession.status === SessionStatus.NoShow) &&
        !entry.nextSession,
    )
    .sort(
      (a, b) =>
        Date.parse(b.mostRecentPastSession!.scheduledTimeUtc) -
        Date.parse(a.mostRecentPastSession!.scheduledTimeUtc),
    )
    .slice(0, ATTENTION_LIMIT);

  const openSlots = (slotsQuery.data ?? [])
    .filter((slot) => !slot.isConsumed)
    .sort((a, b) => Date.parse(a.startTimeUtc) - Date.parse(b.startTimeUtc));
  const openSlotsPreview = openSlots.slice(0, AVAILABILITY_PREVIEW_LIMIT);
  const todaySlots = (slotsQuery.data ?? []).filter((slot) => isTodayInTehran(slot.startTimeUtc));
  const bookedSlotCount = (slotsQuery.data ?? []).filter((slot) => slot.isConsumed).length;

  const hasAvailability = (slotsQuery.data ?? []).length > 0;
  const uniqueStudentCount = roster.length;

  return (
    <Stack spacing={3}>
      {scheduleQuery.isPending ? (
        <Stack spacing={2}>
          {Array.from({ length: 2 }, (_, index) => (
            <SessionCardSkeleton key={index} />
          ))}
        </Stack>
      ) : scheduleQuery.isError ? (
        <IdentityLookupErrorState
          error={scheduleQuery.error}
          onRetry={() => void scheduleQuery.refetch()}
          onChooseAgain={onChooseAgain}
        />
      ) : (
        <>
          <SectionCard title="Teaching Summary">
            <Stack direction="row" flexWrap="wrap" gap={3}>
              <TeachingSummaryStat label="Lessons today" value={todaySessions.length} />
              <TeachingSummaryStat label="Upcoming lessons" value={upcomingSessions.length} />
              <TeachingSummaryStat label="Students" value={uniqueStudentCount} />
              <TeachingSummaryStat label="Open time slots" value={openSlots.length} />
            </Stack>
          </SectionCard>

          {sessions.length > 0 ? (
            <SectionCard title="Lesson History">
              <SessionStatusBreakdownChart counts={deriveStatusCounts(sessions)} />
            </SectionCard>
          ) : null}

          <RecentConversationsSection />

          <MyLearningPlansSection />

          {nextLesson ? (
            <NextLessonHeroCard session={nextLesson} subject={tutorQuery.data?.subject ?? null} />
          ) : null}

          <TeachingDayCard todaySessions={todaySessions} onOpen={openSession} />

          <SectionCard title="Today's Availability">
            {todaySlots.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                No teaching time declared for today.
              </Typography>
            ) : (
              <Stack spacing={2}>
                {todaySlots.map((slot) => (
                  <AvailabilitySummaryCard key={slot.availabilitySlotId} slot={slot} />
                ))}
              </Stack>
            )}
          </SectionCard>

          <SectionCard
            title="Upcoming Lessons"
            action={
              <Button component={RouterLink} to={paths.scheduling.tutorSchedule(tutorId)} size="small">
                View all
              </Button>
            }
          >
            {upcomingSessions.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                No upcoming lessons beyond today.
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

          <SectionCard
            title="Students Requiring Attention"
            action={
              <Button component={RouterLink} to={paths.scheduling.tutorStudents} size="small">
                View all students
              </Button>
            }
          >
            {studentsRequiringAttention.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                No Students need a follow-up right now.
              </Typography>
            ) : (
              <Stack spacing={2}>
                {studentsRequiringAttention.map((entry) => (
                  <StudentRosterCard key={entry.studentId} entry={entry} />
                ))}
              </Stack>
            )}
          </SectionCard>
        </>
      )}

      <SectionCard
        title="Availability Overview"
        action={
          <Button component={RouterLink} to={paths.scheduling.declareAvailability} size="small">
            Manage Your Schedule
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
          <IdentityLookupErrorState
            error={slotsQuery.error}
            onRetry={() => void slotsQuery.refetch()}
            onChooseAgain={onChooseAgain}
          />
        ) : openSlots.length === 0 ? (
          <EmptyState
            title="You haven't added any teaching time"
            description="Add some open times so Students can book a lesson with you."
            action={
              <Button
                component={RouterLink}
                to={paths.scheduling.declareAvailability}
                variant="contained"
                size="small"
                startIcon={<EventAvailableRoundedIcon />}
              >
                Add Availability
              </Button>
            }
          />
        ) : (
          <Stack spacing={2}>
            <Typography variant="body2" color="text.secondary">
              {openSlots.length} open · {bookedSlotCount} booked
            </Typography>
            <Stack spacing={2}>
              {openSlotsPreview.map((slot) => (
                <AvailabilitySummaryCard key={slot.availabilitySlotId} slot={slot} />
              ))}
            </Stack>
          </Stack>
        )}
      </SectionCard>

      {tutorQuery.isSuccess ? (
        <ProfileCompletionCard
          completion={deriveProfileCompletion(tutorQuery.data, hasAvailability)}
          tutorId={tutorId}
        />
      ) : null}

      <SectionCard title="Teaching Tips">
        <Stack spacing={1} alignItems="flex-start">
          <LightbulbRoundedIcon color="disabled" fontSize="large" aria-hidden="true" />
          <Typography variant="body1" fontWeight={600}>
            Teaching tips coming soon
          </Typography>
          <Typography variant="body2" color="text.secondary">
            We&rsquo;re working on tips to help you grow your teaching business. Check back soon.
          </Typography>
        </Stack>
      </SectionCard>
    </Stack>
  );
}

/**
 * The Tutor's action-center landing experience. Quick actions need no
 * Tutor id and always render; everything else reuses `useTutor`,
 * `useTutorSchedule`, and `useTutorAvailabilitySlots` once a Tutor id is
 * provided.
 */
export function TutorDashboard() {
  return (
    <Stack spacing={3}>
      <PageHeader
        title="Welcome back"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            {todayInTehranLabel()} — manage today&rsquo;s teaching, see what&rsquo;s next, and check
            your availability.
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

      <IdentityGate
        kind="tutor"
        fieldLabel="Tutor id"
        title="Let's set up your dashboard"
        description="Enter your tutor id once — we'll remember it on this device so you'll see today's lessons, upcoming lessons, and your availability here every time."
      >
        {(tutorId, forget) => <TeachingOverview tutorId={tutorId} onChooseAgain={forget} />}
      </IdentityGate>
    </Stack>
  );
}
