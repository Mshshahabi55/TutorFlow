import { useNavigate, useParams } from "react-router-dom";
import type { MouseEvent } from "react";
import { Box, Stack } from "@mui/material";
import { useTutorSchedule } from "@/features/scheduling/hooks/useSessionQueries";
import { SessionActions } from "@/features/scheduling/components/SessionActions";
import { SESSION_STATUS_LABEL, SESSION_STATUS_TONE } from "@/features/scheduling/utils/sessionStatus";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { DataTable, type DataTableColumn } from "@/shared/components/table/DataTable";
import { PageHeader } from "@/shared/components/PageHeader";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import type { SessionDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

const columns: DataTableColumn<SessionDto>[] = [
  { key: "studentId", header: "Student id", render: (row) => row.studentId },
  {
    key: "scheduledTimeUtc",
    header: "Scheduled (Tehran)",
    render: (row) => toTehranDisplay(row.scheduledTimeUtc),
  },
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
    render: (row) => (
      <Box onClick={(event: MouseEvent) => event.stopPropagation()}>
        <SessionActions session={row} />
      </Box>
    ),
  },
];

/** GET /tutors/{id}/schedule — every Session for the Tutor, unpaginated (matches the endpoint's own shape). */
export function TutorSessionListPage() {
  const { tutorId } = useParams<{ tutorId: string }>();
  const navigate = useNavigate();
  const scheduleQuery = useTutorSchedule(tutorId);

  return (
    <Stack spacing={3}>
      <PageHeader title="Tutor sessions" />

      {!tutorId ? (
        <IdLookupForm
          label="Tutor id"
          onSubmit={(id) => {
            void navigate(paths.scheduling.tutorSchedule(id));
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
            description: "Sessions booked with this Tutor will appear here.",
          }}
          onRowClick={(row) => {
            void navigate(paths.scheduling.sessionDetail(row.sessionId));
          }}
        />
      )}
    </Stack>
  );
}
