import { Link as RouterLink, useNavigate } from "react-router-dom";
import { Button, Stack, Typography } from "@mui/material";
import { useTutorDirectory } from "@/features/identity/hooks/useTutorQueries";
import { formatMinutesList } from "@/shared/utils/duration";
import { formatToman } from "@/shared/money/rial";
import { DataTable, type DataTableColumn } from "@/shared/components/table/DataTable";
import { PageHeader } from "@/shared/components/PageHeader";
import { paths } from "@/routes/paths";
import type { TutorDto } from "@/services/api/dtos";

const columns: DataTableColumn<TutorDto>[] = [
  { key: "subject", header: "Subject", render: (row) => row.subject ?? "—" },
  { key: "language", header: "Language", render: (row) => row.language ?? "—" },
  { key: "location", header: "Location", render: (row) => row.location ?? "—" },
  {
    key: "hourlyRate",
    header: "Hourly rate (Toman)",
    align: "right",
    render: (row) => (row.hourlyRate !== null ? formatToman(row.hourlyRate) : "—"),
  },
  {
    key: "offeredDurations",
    header: "Durations (min)",
    render: (row) =>
      row.offeredDurations.length > 0 ? formatMinutesList(row.offeredDurations) : "—",
  },
];

/**
 * GET /tutors — every discoverable (approved and not suspended) Tutor, not
 * paginated (the endpoint itself returns a plain array), so DataTable is
 * used here without its pagination prop.
 */
export function TutorDirectoryPage() {
  const navigate = useNavigate();
  const directoryQuery = useTutorDirectory();

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Tutor directory"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Every currently discoverable Tutor. Select a row to view details.
          </Typography>
        }
        action={
          <Button component={RouterLink} to={paths.identity.tutorRegister} variant="outlined">
            Register as Tutor
          </Button>
        }
      />

      <DataTable
        columns={columns}
        rows={directoryQuery.data ?? []}
        getRowKey={(row) => row.tutorId}
        isLoading={directoryQuery.isPending}
        error={directoryQuery.isError ? directoryQuery.error : undefined}
        onRetry={() => void directoryQuery.refetch()}
        emptyState={{
          title: "No discoverable Tutors yet",
          description: "Tutors appear here once an Admin has approved them.",
        }}
        onRowClick={(row) => {
          void navigate(paths.identity.tutorDetail(row.tutorId));
        }}
      />
    </Stack>
  );
}
