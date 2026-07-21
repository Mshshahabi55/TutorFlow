import { useNavigate } from "react-router-dom";
import type { MouseEvent } from "react";
import { Box, Stack, Typography } from "@mui/material";
import { useAllSessions } from "@/features/oversight/hooks/useAllSessions";
import { SessionActions } from "@/features/scheduling/components/SessionActions";
import { SESSION_STATUS_LABEL, SESSION_STATUS_TONE } from "@/features/scheduling/utils/sessionStatus";
import { DataTable, type DataTableColumn } from "@/shared/components/table/DataTable";
import { PageHeader } from "@/shared/components/PageHeader";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { usePagination } from "@/shared/hooks/usePagination";
import { paths } from "@/routes/paths";
import type { SessionDto } from "@/services/api/dtos";

const columns: DataTableColumn<SessionDto>[] = [
  { key: "tutorId", header: "Tutor id", render: (row) => row.tutorId },
  { key: "studentId", header: "Student id", render: (row) => row.studentId },
  { key: "scheduledTimeUtc", header: "Scheduled (UTC)", render: (row) => row.scheduledTimeUtc },
  {
    key: "status",
    header: "Status",
    render: (row) => (
      <StatusPill
        label={SESSION_STATUS_LABEL[row.status]}
        tone={SESSION_STATUS_TONE[row.status]}
      />
    ),
  },
  {
    key: "actions",
    header: "",
    align: "right",
    // Stops the click from bubbling to the row's own onRowClick navigation
    // — otherwise clicking Complete/No-Show/Cancel would also navigate away.
    render: (row) => (
      <Box onClick={(event: MouseEvent) => event.stopPropagation()}>
        <SessionActions session={row} />
      </Box>
    ),
  },
];

/**
 * GET /sessions — every Session across the platform, paginated (ADM-3:
 * "view all schedules"). The only capability the Marketplace Oversight
 * module owns; Session inspection reuses Scheduling & Booking's own
 * SessionDetailPage rather than a duplicate Admin-specific detail view.
 */
export function GlobalSessionListPage() {
  const navigate = useNavigate();
  const { page, pageSize, setPage, setPageSize } = usePagination();
  const sessionsQuery = useAllSessions(page, pageSize);

  return (
    <Stack spacing={3}>
      <PageHeader
        title="All sessions"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Every Session booked across the platform. Select a row to inspect it.
          </Typography>
        }
      />

      <DataTable
        columns={columns}
        rows={sessionsQuery.data?.items ?? []}
        getRowKey={(row) => row.sessionId}
        isLoading={sessionsQuery.isPending}
        error={sessionsQuery.isError ? sessionsQuery.error : undefined}
        onRetry={() => void sessionsQuery.refetch()}
        emptyState={{
          title: "No sessions have been booked yet",
          description: "Sessions will appear here as they're booked across the platform.",
        }}
        pagination={{
          page,
          pageSize,
          totalCount: sessionsQuery.data?.totalCount ?? 0,
          onPageChange: setPage,
          onPageSizeChange: setPageSize,
        }}
        onRowClick={(row) => {
          void navigate(paths.scheduling.sessionDetail(row.sessionId));
        }}
      />
    </Stack>
  );
}
