import { Card, CardContent, Skeleton, Stack } from "@mui/material";

/** Mirrors `ChildSummaryCard`'s layout exactly, so resolving the query never shifts the page. */
export function ChildSummaryCardSkeleton() {
  return (
    <Card variant="outlined" data-testid="child-summary-card-skeleton">
      <CardContent>
        <Stack direction="row" spacing={2} alignItems="center">
          <Skeleton variant="circular" width={48} height={48} />
          <Stack flex={1} spacing={0.5}>
            <Skeleton variant="text" width="60%" height={28} />
          </Stack>
        </Stack>
        <Skeleton variant="rounded" width={130} height={30} sx={{ mt: 2 }} />
      </CardContent>
    </Card>
  );
}
