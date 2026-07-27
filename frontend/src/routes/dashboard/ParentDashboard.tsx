import { Link as RouterLink, useNavigate } from "react-router-dom";
import { Box, Button, Stack, Typography } from "@mui/material";
import HelpOutlineRoundedIcon from "@mui/icons-material/HelpOutlineRounded";
import MenuBookRoundedIcon from "@mui/icons-material/MenuBookRounded";
import { useRelationshipsForAccount } from "@/features/identity/hooks/useRelationshipQueries";
import { ChildSummaryCard } from "@/features/identity/components/ChildSummaryCard";
import { ChildSummaryCardSkeleton } from "@/features/identity/components/ChildSummaryCardSkeleton";
import { useStudentSchedules } from "@/features/scheduling/hooks/useSessionQueries";
import { aggregateFamilySchedule } from "@/features/scheduling/utils/familySchedule";
import { byScheduledTimeAscending, byScheduledTimeDescending } from "@/features/scheduling/utils/sessionSort";
import { AdminSessionCard } from "@/features/oversight/components/AdminSessionCard";
import { SessionCardSkeleton } from "@/features/scheduling/components/SessionCardSkeleton";
import { NextFamilyLessonHeroCard } from "@/routes/dashboard/NextFamilyLessonHeroCard";
import { RecommendedTutors } from "@/routes/dashboard/RecommendedTutors";
import { SectionCard } from "@/shared/components/SectionCard";
import { PageHeader } from "@/shared/components/PageHeader";
import { IdentityGate } from "@/shared/components/IdentityGate";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { isTodayInTehran } from "@/shared/time/tehranTime";
import { ROLE_QUICK_ACTIONS } from "@/routes/dashboardRoleConfig";
import { RelationshipStatus, SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

const RECENT_ACTIVITY_LIMIT = 3;
const UPCOMING_PREVIEW_LIMIT = 5;

function FamilySummaryStat({ label, value }: { label: string; value: number }) {
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
 * Reuses `useRelationshipsForAccount` (the same hook/endpoint
 * `RelationshipsPage` already uses) for the family roster, then
 * `useStudentSchedules` (bounded by how many Confirmed children this
 * family actually has — see that hook's own doc comment on why this isn't
 * the N+1 pattern a previous phase avoided) for the family-wide Today's
 * Lessons/Next Lesson/Upcoming/Recent Activity. `AdminSessionCard` is
 * reused as-is for the family-wide lists — it already shows both Tutor
 * and Student, exactly what a Parent needs to tell their children's
 * lessons apart, and duplicating that shape into a new component would be
 * exactly the kind of "duplicated component" this phase must avoid.
 */
function FamilyOverview({ accountId }: { accountId: string }) {
  const navigate = useNavigate();
  const relationshipsQuery = useRelationshipsForAccount(accountId);
  const confirmedStudentIds = (relationshipsQuery.data ?? [])
    .filter((relationship) => relationship.status === RelationshipStatus.Confirmed)
    .map((relationship) => relationship.studentId);
  // Called unconditionally, before any early return below, per the Rules of
  // Hooks — an empty array (relationships not loaded yet) is a valid input
  // and resolves to an empty result set immediately.
  const scheduleResults = useStudentSchedules(confirmedStudentIds);
  const family = aggregateFamilySchedule(scheduleResults);

  function openSession(session: SessionDto) {
    void navigate(paths.scheduling.sessionDetail(session.sessionId));
  }

  if (relationshipsQuery.isPending) {
    return (
      <SectionCard title="Children Overview">
        <Stack spacing={2}>
          <ChildSummaryCardSkeleton />
          <ChildSummaryCardSkeleton />
        </Stack>
      </SectionCard>
    );
  }

  if (relationshipsQuery.isError) {
    return <ErrorState error={relationshipsQuery.error} onRetry={() => void relationshipsQuery.refetch()} />;
  }

  const relationships = relationshipsQuery.data;

  if (relationships.length === 0) {
    return (
      <SectionCard title="Children Overview">
        <EmptyState
          title="You haven't added a child yet"
          description="Invite a Relationship with a Student to start managing their tutoring."
          action={
            <Button component={RouterLink} to={paths.identity.relationships} variant="contained">
              Invite a Relationship
            </Button>
          }
        />
      </SectionCard>
    );
  }

  const confirmedChildren = relationships.filter(
    (relationship) => relationship.status === RelationshipStatus.Confirmed,
  );

  const todaySessions = family.sessions.filter(
    (session) => session.status === SessionStatus.Scheduled && isTodayInTehran(session.scheduledTimeUtc),
  );
  const upcoming = family.sessions
    .filter((session) => session.status === SessionStatus.Scheduled)
    .sort(byScheduledTimeAscending);
  const nextLesson = upcoming[0];
  const upcomingBeyondToday = upcoming
    .filter((session) => !isTodayInTehran(session.scheduledTimeUtc))
    .slice(0, UPCOMING_PREVIEW_LIMIT);
  const recentActivity = family.sessions
    .filter((session) => session.status !== SessionStatus.Scheduled)
    .sort(byScheduledTimeDescending)
    .slice(0, RECENT_ACTIVITY_LIMIT);

  return (
    <Stack spacing={3}>
      <SectionCard title="Family Summary">
        <Stack direction="row" flexWrap="wrap" gap={3}>
          <FamilySummaryStat label="Children" value={confirmedChildren.length} />
          <FamilySummaryStat label="Lessons today" value={todaySessions.length} />
          <FamilySummaryStat label="Upcoming lessons" value={upcoming.length} />
        </Stack>
      </SectionCard>

      {family.isPending ? (
        <Stack spacing={2}>
          <SessionCardSkeleton />
          <SessionCardSkeleton />
        </Stack>
      ) : family.isError ? (
        <ErrorState error={family.error} title="Some lessons could not be loaded" />
      ) : (
        <>
          {nextLesson ? <NextFamilyLessonHeroCard session={nextLesson} /> : null}

          <SectionCard title="Today's Lessons">
            {todaySessions.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                No lessons today.
              </Typography>
            ) : (
              <Stack spacing={2}>
                {todaySessions.map((session) => (
                  <AdminSessionCard key={session.sessionId} session={session} onOpen={openSession} />
                ))}
              </Stack>
            )}
          </SectionCard>
        </>
      )}

      <SectionCard
        title="Children Overview"
        action={
          <Button component={RouterLink} to={paths.identity.relationships} size="small">
            Manage Relationships
          </Button>
        }
      >
        <Stack spacing={2}>
          {relationships.map((relationship) => (
            <ChildSummaryCard key={relationship.relationshipId} relationship={relationship} />
          ))}
        </Stack>
      </SectionCard>

      <SectionCard title="Upcoming Lessons">
        {upcomingBeyondToday.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            No upcoming lessons beyond today.
          </Typography>
        ) : (
          <Stack spacing={2}>
            {upcomingBeyondToday.map((session) => (
              <AdminSessionCard key={session.sessionId} session={session} onOpen={openSession} />
            ))}
          </Stack>
        )}
      </SectionCard>

      <SectionCard title="Recent Activity">
        {recentActivity.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            Nothing completed or cancelled yet.
          </Typography>
        ) : (
          <Stack spacing={2}>
            {recentActivity.map((session) => (
              <AdminSessionCard key={session.sessionId} session={session} onOpen={openSession} />
            ))}
          </Stack>
        )}
      </SectionCard>

      <SectionCard
        title="Recommended Tutors"
        action={
          <Button component={RouterLink} to={paths.discovery.tutorSearch} size="small">
            Browse all tutors
          </Button>
        }
      >
        <RecommendedTutors />
      </SectionCard>
    </Stack>
  );
}

/**
 * The Parent/Guardian's family command-center landing experience. Quick
 * actions need no id and always render; the family overview reuses
 * `useRelationshipsForAccount` once a Parent/Guardian id is known — via
 * the shared `useRememberedId`/`IdentityGate` pattern (there is still no
 * "my own id" resolution from an authenticated Account — ADR-011 remains
 * frozen), so it's asked for once per device rather than on every visit.
 */
export function ParentDashboard() {
  return (
    <Stack spacing={3}>
      <PageHeader
        title="Welcome back"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Manage your family&rsquo;s tutoring — see your children and their lessons at a glance.
          </Typography>
        }
      />

      <SectionCard title="Quick actions">
        <Stack direction="row" flexWrap="wrap" gap={1.5}>
          {ROLE_QUICK_ACTIONS.ParentGuardian.map((action) => (
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
        kind="parentGuardian"
        fieldLabel="Parent/Guardian id"
        title="Let's set up your family view"
        description="Enter your Parent/Guardian id once — we'll remember it on this device so you'll see your children and their lessons here every time."
      >
        {(accountId) => <FamilyOverview accountId={accountId} />}
      </IdentityGate>

      <SectionCard title="Learning Tips">
        <Stack spacing={1} alignItems="flex-start">
          <MenuBookRoundedIcon color="disabled" fontSize="large" aria-hidden="true" />
          <Typography variant="body1" fontWeight={600}>
            Learning tips coming soon
          </Typography>
          <Typography variant="body2" color="text.secondary">
            We&rsquo;re working on tips to help you support your child&rsquo;s learning. Check back
            soon.
          </Typography>
        </Stack>
      </SectionCard>

      <SectionCard title="Support">
        <Stack spacing={1} alignItems="flex-start">
          <HelpOutlineRoundedIcon color="disabled" fontSize="large" aria-hidden="true" />
          <Typography variant="body1" fontWeight={600}>
            Support resources coming soon
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Need help with your family&rsquo;s account? Support options will be available here soon.
          </Typography>
        </Stack>
      </SectionCard>
    </Stack>
  );
}
