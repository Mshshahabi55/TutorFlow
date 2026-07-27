import { Card, CardContent, Skeleton, Stack } from "@mui/material";

/** Mirrors the resolved layout (Tutor Summary + Availability grid) so resolving the query never shifts the page. */
export function BookingPageSkeleton() {
  return (
    <Stack spacing={3} data-testid="booking-page-skeleton">
      <Card variant="outlined">
        <CardContent>
          <Skeleton variant="text" width="20%" />
          <Stack direction="row" spacing={2} alignItems="center" mt={0.5}>
            <Skeleton variant="circular" width={56} height={56} />
            <Stack flex={1} spacing={0.5}>
              <Skeleton variant="text" width="40%" height={28} />
              <Skeleton variant="text" width="30%" />
            </Stack>
            <Skeleton variant="text" width={90} />
          </Stack>
        </CardContent>
      </Card>

      <Card variant="outlined">
        <CardContent>
          <Skeleton variant="text" width="25%" height={32} />
          <Stack direction="row" flexWrap="wrap" gap={2} mt={1}>
            {Array.from({ length: 3 }, (_, index) => (
              <Skeleton key={index} variant="rectangular" width={220} height={96} sx={{ borderRadius: 1 }} />
            ))}
          </Stack>
        </CardContent>
      </Card>
    </Stack>
  );
}
