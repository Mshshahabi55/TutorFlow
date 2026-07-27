import { Card, CardContent, Skeleton, Stack } from "@mui/material";

/** Mirrors `AvailabilitySummaryCard`'s layout exactly, so resolving the query never shifts the page. */
export function AvailabilitySummaryCardSkeleton() {
  return (
    <Card variant="outlined" data-testid="availability-summary-card-skeleton">
      <CardContent>
        <Stack direction="row" justifyContent="space-between" alignItems="center" mb={1}>
          <Skeleton variant="text" width="45%" height={28} />
          <Skeleton variant="rounded" width={70} height={24} />
        </Stack>
        <Skeleton variant="text" width="35%" />
        <Skeleton variant="rounded" width={100} height={30} sx={{ mt: 1 }} />
      </CardContent>
    </Card>
  );
}
