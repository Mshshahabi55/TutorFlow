import { Card, CardContent, Skeleton, Stack } from "@mui/material";

/** Mirrors the resolved layout (hero + Tutor Summary/Session Summary cards) so resolving the query never shifts the page. */
export function SessionDetailSkeleton() {
  return (
    <Stack spacing={3} data-testid="session-detail-skeleton">
      <Card variant="outlined">
        <CardContent>
          <Skeleton variant="text" width="30%" height={32} />
          <Skeleton variant="text" width="50%" />
          <Stack direction="row" spacing={2} alignItems="center" mt={2}>
            <Skeleton variant="circular" width={40} height={40} />
            <Skeleton variant="text" width={120} />
            <Skeleton variant="circular" width={40} height={40} />
            <Skeleton variant="text" width={120} />
          </Stack>
        </CardContent>
      </Card>

      <Stack direction={{ xs: "column", md: "row" }} spacing={3}>
        <Card variant="outlined" sx={{ flex: 2 }}>
          <CardContent>
            <Skeleton variant="text" width="25%" height={32} />
            <Skeleton variant="text" width="60%" />
            <Skeleton variant="rectangular" height={64} sx={{ mt: 2, borderRadius: 1 }} />
          </CardContent>
        </Card>
        <Card variant="outlined" sx={{ flex: 1 }}>
          <CardContent>
            <Skeleton variant="text" width="40%" height={32} />
            <Skeleton variant="text" width="80%" />
            <Skeleton variant="text" width="60%" />
          </CardContent>
        </Card>
      </Stack>
    </Stack>
  );
}
