import { Stack, Typography } from "@mui/material";
import { usePendingTutors } from "@/features/identity/hooks/useTutorQueries";
import { TutorApprovalActions } from "@/features/identity/components/TutorApprovalActions";
import { DataTable, type DataTableColumn } from "@/shared/components/table/DataTable";
import { PageHeader } from "@/shared/components/PageHeader";
import { usePagination } from "@/shared/hooks/usePagination";
import type { TutorDto } from "@/services/api/dtos";

const columns: DataTableColumn<TutorDto>[] = [
  { key: "tutorId", header: "Tutor id", render: (row) => row.tutorId },
  { key: "subject", header: "Subject", render: (row) => row.subject ?? "—" },
  {
    key: "actions",
    header: "",
    align: "right",
    render: (row) => <TutorApprovalActions tutor={row} />,
  },
];

export function AdminPendingTutorsPage() {
  const { page, pageSize, setPage, setPageSize } = usePagination();
  const pendingQuery = usePendingTutors(page, pageSize);

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Pending Tutor approvals"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Tutors awaiting Admin review. Approving makes a Tutor discoverable; suspending removes
            discoverability and bookability.
          </Typography>
        }
      />

      <DataTable
        columns={columns}
        rows={pendingQuery.data?.items ?? []}
        getRowKey={(row) => row.tutorId}
        isLoading={pendingQuery.isPending}
        error={pendingQuery.isError ? pendingQuery.error : undefined}
        onRetry={() => void pendingQuery.refetch()}
        emptyState={{
          title: "No Tutors are pending approval",
          description: "Every registered Tutor has already been reviewed.",
        }}
        pagination={{
          page,
          pageSize,
          totalCount: pendingQuery.data?.totalCount ?? 0,
          onPageChange: setPage,
          onPageSizeChange: setPageSize,
        }}
      />
    </Stack>
  );
}
