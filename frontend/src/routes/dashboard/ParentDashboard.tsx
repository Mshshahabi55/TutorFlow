import { useState } from "react";
import { Link as RouterLink, useNavigate } from "react-router-dom";
import { Button, Stack, Typography } from "@mui/material";
import { useRelationshipsForAccount } from "@/features/identity/hooks/useRelationshipQueries";
import { ChildSummaryCard } from "@/features/identity/components/ChildSummaryCard";
import { ChildSummaryCardSkeleton } from "@/features/identity/components/ChildSummaryCardSkeleton";
import { useStudentSchedule } from "@/features/scheduling/hooks/useSessionQueries";
import { SessionCard } from "@/features/scheduling/components/SessionCard";
import { SessionCardSkeleton } from "@/features/scheduling/components/SessionCardSkeleton";
import { byScheduledTimeAscending, byScheduledTimeDescending } from "@/features/scheduling/utils/sessionSort";
import { SectionCard } from "@/shared/components/SectionCard";
import { PageHeader } from "@/shared/components/PageHeader";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { ROLE_QUICK_ACTIONS } from "@/routes/dashboardRoleConfig";
import { RelationshipStatus, SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

const RECENT_ACTIVITY_LIMIT = 3;

/**
 * Reuses `useStudentSchedule` (the same hook `StudentSessionListPage`
 * already uses) for exactly one confirmed child — fetching a schedule per
 * child for a Parent with several children would be an N+1 pattern this
 * phase must avoid, so this only renders when there is exactly one
 * confirmed Relationship. With more than one, each child's own "View
 * sessions" link (on their `ChildSummaryCard`) is the way to see their
 * schedule instead of aggregating on the dashboard.
 */
function SingleChildSchedule({ studentId }: { studentId: string }) {
  const navigate = useNavigate();
  const scheduleQuery = useStudentSchedule(studentId);

  function openSession(session: SessionDto) {
    void navigate(paths.scheduling.sessionDetail(session.sessionId));
  }

  if (scheduleQuery.isPending) {
    return (
      <SectionCard title="Upcoming Sessions">
        <Stack spacing={2}>
          <SessionCardSkeleton />
          <SessionCardSkeleton />
        </Stack>
      </SectionCard>
    );
  }

  if (scheduleQuery.isError) {
    return <ErrorState error={scheduleQuery.error} onRetry={() => void scheduleQuery.refetch()} />;
  }

  const sessions = scheduleQuery.data;
  const upcoming = sessions
    .filter((session) => session.status === SessionStatus.Scheduled)
    .sort(byScheduledTimeAscending);
  const recentActivity = sessions
    .filter((session) => session.status !== SessionStatus.Scheduled)
    .sort(byScheduledTimeDescending)
    .slice(0, RECENT_ACTIVITY_LIMIT);

  return (
    <Stack spacing={3}>
      <SectionCard
        title="Upcoming Sessions"
        action={
          <Button component={RouterLink} to={paths.scheduling.studentSchedule(studentId)} size="small">
            View all
          </Button>
        }
      >
        {upcoming.length === 0 ? (
          <EmptyState
            title="No upcoming sessions yet"
            description="Book a session with a tutor to see it here."
            action={
              <Button component={RouterLink} to={paths.scheduling.bookSession} variant="contained">
                Book a session
              </Button>
            }
          />
        ) : (
          <Stack spacing={2}>
            {upcoming.map((session) => (
              <SessionCard key={session.sessionId} session={session} onOpen={openSession} />
            ))}
          </Stack>
        )}
      </SectionCard>

      {recentActivity.length > 0 ? (
        <SectionCard title="Recent Activity">
          <Stack spacing={2}>
            {recentActivity.map((session) => (
              <SessionCard key={session.sessionId} session={session} onOpen={openSession} />
            ))}
          </Stack>
        </SectionCard>
      ) : null}
    </Stack>
  );
}

/**
 * Reuses `useRelationshipsForAccount` (the same hook/endpoint
 * `RelationshipsPage` already uses — "works for either a Parent/Guardian
 * id or a Student id, both are Accounts") to derive the Children Overview
 * directly from `RelationshipDto` — no per-child Student fetch, so this
 * section alone is a single query regardless of how many children exist.
 */
function FamilyOverview({ accountId }: { accountId: string }) {
  const relationshipsQuery = useRelationshipsForAccount(accountId);

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
          title="No children linked yet"
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

  return (
    <Stack spacing={3}>
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

      {confirmedChildren.length === 1 ? (
        <SingleChildSchedule studentId={confirmedChildren[0].studentId} />
      ) : confirmedChildren.length > 1 ? (
        <SectionCard title="Upcoming Sessions">
          <Typography variant="body2" color="text.secondary">
            You have more than one child — view each child&rsquo;s sessions from their card above.
          </Typography>
        </SectionCard>
      ) : null}
    </Stack>
  );
}

/**
 * The Parent/Guardian's landing experience. Quick actions need no id and
 * always render; the family overview reuses `useRelationshipsForAccount`
 * once a Parent/Guardian id is provided — the same `IdLookupForm` pattern
 * already used identically across the app (there is still no "my own id"
 * resolution from an authenticated Account — ADR-011 remains frozen).
 */
export function ParentDashboard() {
  const [accountId, setAccountId] = useState<string | undefined>(undefined);

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Welcome back"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Manage your family&rsquo;s tutoring — see your children and their upcoming sessions at
            a glance.
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

      {accountId ? (
        <FamilyOverview accountId={accountId} />
      ) : (
        <SectionCard title="Your family">
          <Stack spacing={2} alignItems="flex-start">
            <Typography variant="body2" color="text.secondary">
              Enter your Parent/Guardian id to see your children and their upcoming sessions.
            </Typography>
            <IdLookupForm label="Parent/Guardian id" onSubmit={setAccountId} />
          </Stack>
        </SectionCard>
      )}
    </Stack>
  );
}
