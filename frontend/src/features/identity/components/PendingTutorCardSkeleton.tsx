import { Card, CardContent, Skeleton, Stack } from "@mui/material";

/** Mirrors `PendingTutorCard`'s layout exactly, so resolving the query never shifts the page. */
export function PendingTutorCardSkeleton() {
  return (
    <Card variant="outlined" data-testid="pending-tutor-card-skeleton">
      <CardContent>
        <Stack direction="row" justifyContent="space-between" alignItems="center" mb={1}>
          <Skeleton variant="text" width="40%" height={28} />
          <Skeleton variant="rounded" width={100} height={24} />
        </Stack>
        <Skeleton variant="text" width="35%" />
        <Skeleton variant="text" width="30%" />
        <Skeleton variant="rounded" width={160} height={32} sx={{ mt: 1 }} />
      </CardContent>
    </Card>
  );
}
