import { Card, CardContent, Skeleton, Stack } from "@mui/material";

/** Mirrors `SessionCard`'s layout exactly, so resolving the schedule never shifts the page. */
export function SessionCardSkeleton() {
  return (
    <Card variant="outlined" data-testid="session-card-skeleton">
      <CardContent>
        <Stack direction="row" justifyContent="space-between" alignItems="center" mb={1}>
          <Skeleton variant="text" width="45%" height={28} />
          <Skeleton variant="rounded" width={90} height={24} />
        </Stack>
        <Skeleton variant="text" width="30%" />
        <Skeleton variant="text" width="50%" />
      </CardContent>
    </Card>
  );
}
