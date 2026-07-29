import { Box, Card, CardContent, Skeleton, Stack } from "@mui/material";

/**
 * Mirrors `TutorProfileHero` + the profile's actual current section count
 * (Phase 9: About, Certificates & Experience, Teaching Style, Availability,
 * Learning Plans, Reviews, FAQ — 6 nav entries including "Similar tutors")
 * so resolving the query never shifts the layout as badly as the earlier,
 * now-stale 4-card version did. The 5th left-column card is shaped like a
 * small grid rather than plain text lines, standing in for the Availability
 * section's `AvailabilityPreviewCalendar`.
 */
export function TutorProfileSkeleton() {
  return (
    <Stack spacing={3} data-testid="tutor-profile-skeleton">
      <Stack direction={{ xs: "column", sm: "row" }} spacing={3} alignItems={{ xs: "flex-start", sm: "center" }}>
        <Skeleton variant="circular" width={96} height={96} />
        <Stack flex={1} spacing={1} minWidth={0} width="100%">
          <Skeleton variant="text" width="40%" height={40} />
          <Skeleton variant="text" width="30%" />
          <Skeleton variant="text" width="25%" />
          <Skeleton variant="text" width="45%" />
        </Stack>
        <Skeleton variant="rectangular" width={160} height={42} sx={{ borderRadius: 1 }} />
      </Stack>

      <Stack direction="row" spacing={3} flexWrap="wrap">
        {Array.from({ length: 6 }, (_, index) => (
          <Skeleton key={index} variant="text" width={70} />
        ))}
      </Stack>

      <Stack direction={{ xs: "column", md: "row" }} spacing={3} alignItems="stretch">
        <Stack flex={2} spacing={3}>
          {Array.from({ length: 4 }, (_, index) => (
            <Card key={index} variant="outlined">
              <CardContent>
                <Skeleton variant="text" width="30%" height={32} />
                <Skeleton variant="text" width="90%" />
                <Skeleton variant="text" width="70%" />
              </CardContent>
            </Card>
          ))}
          <Card variant="outlined">
            <CardContent>
              <Skeleton variant="text" width="30%" height={32} sx={{ mb: 1 }} />
              <Box display="grid" gridTemplateColumns="repeat(7, 1fr)" gap={0.5}>
                {Array.from({ length: 21 }, (_, index) => (
                  <Skeleton key={index} variant="rounded" height={32} />
                ))}
              </Box>
            </CardContent>
          </Card>
          <Card variant="outlined">
            <CardContent>
              <Skeleton variant="text" width="30%" height={32} />
              <Skeleton variant="text" width="90%" />
              <Skeleton variant="text" width="70%" />
            </CardContent>
          </Card>
        </Stack>
        <Stack flex={1} spacing={3}>
          <Card variant="outlined">
            <CardContent>
              <Skeleton variant="text" width="50%" height={32} />
              <Skeleton variant="text" width="40%" />
              <Skeleton variant="text" width="60%" />
              <Skeleton variant="rectangular" height={40} sx={{ mt: 1, borderRadius: 1 }} />
            </CardContent>
          </Card>
        </Stack>
      </Stack>
    </Stack>
  );
}
