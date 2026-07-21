import { Link as RouterLink } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import type { UseQueryResult } from "@tanstack/react-query";
import { usePendingTutors, useTutorDirectory } from "@/features/identity/hooks/useTutorQueries";
import { useAllSessions } from "@/features/oversight/hooks/useAllSessions";
import { PageHeader } from "@/shared/components/PageHeader";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { paths } from "@/routes/paths";

interface WidgetCardProps<T> {
  title: string;
  query: UseQueryResult<T>;
  renderCount: (data: T) => number;
  linkTo: string;
  linkLabel: string;
}

/**
 * Every widget composes an existing query hook from its own owning module
 * (Identity & Relationship for Tutor counts, Marketplace Oversight for the
 * Session count) — Oversight is a cross-cutting consumer of both by design
 * (ARCHITECTURE.md §4), not a duplicate of either module's own data-fetching.
 */
function WidgetCard<T>({ title, query, renderCount, linkTo, linkLabel }: WidgetCardProps<T>) {
  return (
    <Card variant="outlined" sx={{ minWidth: 220, flex: 1 }}>
      <CardContent>
        <Typography variant="subtitle2" color="text.secondary" gutterBottom>
          {title}
        </Typography>
        {query.isPending ? <LoadingState label="Loading…" minHeight={80} /> : null}
        {query.isError ? (
          <ErrorState error={query.error} onRetry={() => void query.refetch()} />
        ) : null}
        {query.isSuccess ? (
          <Stack spacing={1.5}>
            <Typography variant="h3">{renderCount(query.data)}</Typography>
            <Button component={RouterLink} to={linkTo} size="small" sx={{ alignSelf: "flex-start" }}>
              {linkLabel}
            </Button>
          </Stack>
        ) : null}
      </CardContent>
    </Card>
  );
}

export function AdminDashboardPage() {
  const pendingTutorsQuery = usePendingTutors(1, 1);
  const allSessionsQuery = useAllSessions(1, 1);
  const tutorDirectoryQuery = useTutorDirectory();

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

      <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
        <WidgetCard
          title="Pending Tutor approvals"
          query={pendingTutorsQuery}
          renderCount={(data) => data.totalCount}
          linkTo={paths.identity.tutorPending}
          linkLabel="Review queue"
        />
        <WidgetCard
          title="Total sessions"
          query={allSessionsQuery}
          renderCount={(data) => data.totalCount}
          linkTo={paths.oversight.globalSessions}
          linkLabel="View all sessions"
        />
        <WidgetCard
          title="Discoverable Tutors"
          query={tutorDirectoryQuery}
          renderCount={(data) => data.length}
          linkTo={paths.identity.tutorDirectory}
          linkLabel="View directory"
        />
      </Stack>
    </Stack>
  );
}
