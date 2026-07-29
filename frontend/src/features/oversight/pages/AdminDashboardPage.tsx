import { Link as RouterLink, useNavigate } from "react-router-dom";
import { Box, Button, Stack, Typography } from "@mui/material";
import { useQuery, type UseQueryResult } from "@tanstack/react-query";
import { usePendingTutors, useTutorDirectory } from "@/features/identity/hooks/useTutorQueries";
import { useAllSessions } from "@/features/oversight/hooks/useAllSessions";
import { useSessionStatusCounts } from "@/features/oversight/hooks/useSessionStatusCounts";
import { SessionStatusBreakdownChart } from "@/features/scheduling/components/SessionStatusBreakdownChart";
import { countsFromDto } from "@/features/scheduling/utils/sessionStatusCounts";
import { PendingTutorCard } from "@/features/identity/components/PendingTutorCard";
import { PendingTutorCardSkeleton } from "@/features/identity/components/PendingTutorCardSkeleton";
import { AdminSessionCard } from "@/features/oversight/components/AdminSessionCard";
import { AdminSessionCardSkeleton } from "@/features/oversight/components/AdminSessionCardSkeleton";
import { RecentConversationsSection } from "@/features/communication/components/RecentConversationsSection";
import { SectionCard } from "@/shared/components/SectionCard";
import { PageHeader } from "@/shared/components/PageHeader";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { fetchHealthStatus } from "@/services/api/healthService";
import { ROLE_QUICK_ACTIONS } from "@/routes/dashboardRoleConfig";
import { paths } from "@/routes/paths";
import type { SessionDto } from "@/services/api/dtos";

const PENDING_TUTORS_PREVIEW = 5;
const RECENT_SESSIONS_PREVIEW = 5;

interface MarketplaceStatProps<T> {
  title: string;
  query: UseQueryResult<T>;
  renderCount: (data: T) => number;
  /** Omitted for "Total sessions" — the "Recent Sessions" section right above already links to /oversight/sessions, so a second identically-labelled link here would be redundant/ambiguous. */
  linkTo?: string;
  linkLabel?: string;
}

/**
 * Every stat here composes an existing query hook from its own owning
 * module (Identity & Relationship for the Tutor count, Marketplace
 * Oversight for the Session count) — unchanged from before Phase 3 Step 8,
 * just restyled to sit inside "Marketplace Overview" instead of its own
 * separate outlined Card.
 */
function MarketplaceStat<T>({ title, query, renderCount, linkTo, linkLabel }: MarketplaceStatProps<T>) {
  return (
    <Box flex={1} minWidth={200}>
      <Typography variant="caption" color="text.secondary">
        {title}
      </Typography>
      {query.isPending ? <LoadingState label="Loading…" minHeight={72} /> : null}
      {query.isError ? (
        <ErrorState error={query.error} onRetry={() => void query.refetch()} />
      ) : null}
      {query.isSuccess ? (
        <Stack spacing={1}>
          <Typography variant="h4" component="p" fontWeight={700}>
            {renderCount(query.data)}
          </Typography>
          {linkTo && linkLabel ? (
            <Button component={RouterLink} to={linkTo} size="small" sx={{ alignSelf: "flex-start" }}>
              {linkLabel}
            </Button>
          ) : null}
        </Stack>
      ) : null}
    </Box>
  );
}

/**
 * Marketplace Oversight's own operations dashboard (`/oversight/dashboard`,
 * AdminStaff-only). Every widget still reads an existing capability — no
 * data is fabricated to fill this page. "Today's Sessions" was considered
 * and deliberately left out: `GET /sessions` has no date filter and no
 * documented chronological ordering, so a "today" view can't be built
 * honestly from it without assuming an ordering the API contract doesn't
 * state — "Recent Sessions" below shows the real first page instead,
 * labelled for what it actually is.
 */
export function AdminDashboardPage() {
  const navigate = useNavigate();
  const pendingTutorsQuery = usePendingTutors(1, PENDING_TUTORS_PREVIEW);
  const allSessionsQuery = useAllSessions(1, RECENT_SESSIONS_PREVIEW);
  const statusCountsQuery = useSessionStatusCounts();
  const tutorDirectoryQuery = useTutorDirectory();
  const healthQuery = useQuery({ queryKey: ["health"], queryFn: fetchHealthStatus });

  function openSession(session: SessionDto) {
    void navigate(paths.scheduling.sessionDetail(session.sessionId));
  }

  const quickActions = ROLE_QUICK_ACTIONS.AdminStaff.filter(
    (action) => action.to !== paths.oversight.adminDashboard,
  );

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Admin dashboard"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Every widget reads an existing capability — no data is fabricated to fill this page.
          </Typography>
        }
      />

      <SectionCard title="Quick actions">
        <Stack direction="row" flexWrap="wrap" gap={1.5}>
          {quickActions.map((action) => (
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

      <SectionCard
        title="Pending Tutor Approvals"
        action={
          <Button component={RouterLink} to={paths.identity.tutorPending} size="small">
            Review queue
          </Button>
        }
      >
        {pendingTutorsQuery.isPending ? (
          <Stack spacing={2}>
            <PendingTutorCardSkeleton />
            <PendingTutorCardSkeleton />
          </Stack>
        ) : pendingTutorsQuery.isError ? (
          <ErrorState
            error={pendingTutorsQuery.error}
            onRetry={() => void pendingTutorsQuery.refetch()}
          />
        ) : pendingTutorsQuery.data.totalCount === 0 ? (
          <EmptyState
            title="No Tutors are pending approval"
            description="Every registered Tutor has already been reviewed."
          />
        ) : (
          <Stack spacing={2}>
            <Typography variant="h4" component="p" fontWeight={700}>
              {pendingTutorsQuery.data.totalCount}
            </Typography>
            {pendingTutorsQuery.data.items.length > 0 ? (
              <Stack spacing={2}>
                {pendingTutorsQuery.data.items.map((tutor) => (
                  <PendingTutorCard key={tutor.tutorId} tutor={tutor} />
                ))}
              </Stack>
            ) : null}
          </Stack>
        )}
      </SectionCard>

      <SectionCard
        title="Recent Sessions"
        action={
          <Button component={RouterLink} to={paths.oversight.globalSessions} size="small">
            View all sessions
          </Button>
        }
      >
        {allSessionsQuery.isPending ? (
          <Stack spacing={2}>
            <AdminSessionCardSkeleton />
            <AdminSessionCardSkeleton />
          </Stack>
        ) : allSessionsQuery.isError ? (
          <ErrorState error={allSessionsQuery.error} onRetry={() => void allSessionsQuery.refetch()} />
        ) : allSessionsQuery.data.items.length === 0 ? (
          <EmptyState
            title="No sessions have been booked yet"
            description="Sessions will appear here as they're booked across the platform."
          />
        ) : (
          <Stack spacing={2}>
            {allSessionsQuery.data.items.map((session) => (
              <AdminSessionCard key={session.sessionId} session={session} onOpen={openSession} />
            ))}
          </Stack>
        )}
      </SectionCard>

      <RecentConversationsSection />

      <SectionCard title="Marketplace Overview">
        <Stack direction={{ xs: "column", sm: "row" }} spacing={3}>
          <MarketplaceStat
            title="Total sessions"
            query={allSessionsQuery}
            renderCount={(data) => data.totalCount}
          />
          <MarketplaceStat
            title="Discoverable Tutors"
            query={tutorDirectoryQuery}
            renderCount={(data) => data.length}
            linkTo={paths.identity.tutorDirectory}
            linkLabel="View directory"
          />
        </Stack>
      </SectionCard>

      <SectionCard title="Session Status Breakdown">
        {statusCountsQuery.isPending ? (
          <LoadingState label="Loading session counts…" />
        ) : statusCountsQuery.isError ? (
          <ErrorState error={statusCountsQuery.error} onRetry={() => void statusCountsQuery.refetch()} />
        ) : (
          <SessionStatusBreakdownChart counts={countsFromDto(statusCountsQuery.data)} />
        )}
      </SectionCard>

      <SectionCard title="Platform Health">
        {healthQuery.isPending ? <LoadingState label="Checking backend health…" /> : null}
        {healthQuery.isError ? (
          <ErrorState error={healthQuery.error} onRetry={() => void healthQuery.refetch()} />
        ) : null}
        {healthQuery.isSuccess ? (
          <Stack direction="row" alignItems="center" spacing={1.5}>
            <StatusPill
              label={healthQuery.data}
              tone={healthQuery.data === "Healthy" ? "success" : "critical"}
            />
            <Typography variant="body2" color="text.secondary">
              GET /health
            </Typography>
          </Stack>
        ) : null}
      </SectionCard>
    </Stack>
  );
}
