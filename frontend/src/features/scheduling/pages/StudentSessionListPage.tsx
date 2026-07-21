import { useNavigate, useParams } from "react-router-dom";
import type { MouseEvent } from "react";
import { Box, Stack } from "@mui/material";
import { useStudentSchedule } from "@/features/scheduling/hooks/useSessionQueries";
import { SessionActions } from "@/features/scheduling/components/SessionActions";
import { SESSION_STATUS_LABEL, SESSION_STATUS_TONE } from "@/features/scheduling/utils/sessionStatus";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { DataTable, type DataTableColumn } from "@/shared/components/table/DataTable";
import { PageHeader } from "@/shared/components/PageHeader";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import type { SessionDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

const columns: DataTableColumn<SessionDto>[] = [
  { key: "tutorId", header: "Tutor id", render: (row) => row.tutorId },
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

/** GET /students/{id}/schedule — every Session for the Student, unpaginated (matches the endpoint's own shape). */
export function StudentSessionListPage() {
  const { studentId } = useParams<{ studentId: string }>();
  const navigate = useNavigate();
  const scheduleQuery = useStudentSchedule(studentId);

  return (
    <Stack spacing={3}>
      <PageHeader title="Student sessions" />

      {!studentId ? (
        <IdLookupForm
          label="Student id"
          onSubmit={(id) => {
            void navigate(paths.scheduling.studentSchedule(id));
          }}
        />
      ) : (
        <DataTable
          columns={columns}
          rows={scheduleQuery.data ?? []}
          getRowKey={(row) => row.sessionId}
          isLoading={scheduleQuery.isPending}
          error={scheduleQuery.isError ? scheduleQuery.error : undefined}
          onRetry={() => void scheduleQuery.refetch()}
          emptyState={{
            title: "No sessions yet",
            description: "Sessions booked for this Student will appear here.",
          }}
          onRowClick={(row) => {
            void navigate(paths.scheduling.sessionDetail(row.sessionId));
          }}
        />
      )}
    </Stack>
  );
}
