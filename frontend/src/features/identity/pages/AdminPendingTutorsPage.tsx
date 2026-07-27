import { Stack, TablePagination, Typography } from "@mui/material";
import { usePendingTutors } from "@/features/identity/hooks/useTutorQueries";
import { PendingTutorCard } from "@/features/identity/components/PendingTutorCard";
import { PendingTutorCardSkeleton } from "@/features/identity/components/PendingTutorCardSkeleton";
import { PageHeader } from "@/shared/components/PageHeader";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { usePagination } from "@/shared/hooks/usePagination";

const SKELETON_COUNT = 4;

/**
 * GET /tutors/pending — every Tutor awaiting Admin review, paginated. Same
 * query, route, and moderation actions as before (Phase 3 Step 8 is
 * presentation-only) — cards replace the plain table, nothing else. There
 * is no filter parameter on this endpoint to improve the presentation of
 * (only page/pageSize), so none was added.
 */
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

      {pendingQuery.isPending ? (
        <Stack spacing={2}>
          {Array.from({ length: SKELETON_COUNT }, (_, index) => (
            <PendingTutorCardSkeleton key={index} />
          ))}
        </Stack>
      ) : pendingQuery.isError ? (
        <ErrorState error={pendingQuery.error} onRetry={() => void pendingQuery.refetch()} />
      ) : pendingQuery.data.items.length === 0 ? (
        <EmptyState
          title="No Tutors are pending approval"
          description="Every registered Tutor has already been reviewed."
        />
      ) : (
        <>
          <Stack spacing={2}>
            {pendingQuery.data.items.map((tutor) => (
              <PendingTutorCard key={tutor.tutorId} tutor={tutor} />
            ))}
          </Stack>
          <TablePagination
            component="div"
            count={pendingQuery.data.totalCount}
            page={page - 1}
            rowsPerPage={pageSize}
            rowsPerPageOptions={[10, 20, 50]}
            onPageChange={(_event, newPage) => setPage(newPage + 1)}
            onRowsPerPageChange={(event) => {
              setPageSize(Number(event.target.value));
              setPage(1);
            }}
          />
        </>
      )}
    </Stack>
  );
}
