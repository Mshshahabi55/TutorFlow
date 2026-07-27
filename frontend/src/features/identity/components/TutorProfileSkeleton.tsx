import { Card, CardContent, Skeleton, Stack } from "@mui/material";

/** Mirrors `TutorProfileHero` + the profile's card sections exactly, so resolving the query never shifts the layout. */
export function TutorProfileSkeleton() {
  return (
    <Stack spacing={3} data-testid="tutor-profile-skeleton">
      <Stack direction={{ xs: "column", sm: "row" }} spacing={3} alignItems={{ xs: "flex-start", sm: "center" }}>
        <Skeleton variant="circular" width={88} height={88} />
        <Stack flex={1} spacing={1} minWidth={0} width="100%">
          <Skeleton variant="text" width="40%" height={40} />
          <Skeleton variant="text" width="30%" />
        </Stack>
        <Skeleton variant="rectangular" width={160} height={42} sx={{ borderRadius: 1 }} />
      </Stack>

      <Stack direction={{ xs: "column", md: "row" }} spacing={3} alignItems="stretch">
        <Stack flex={2} spacing={3}>
          <Card variant="outlined">
            <CardContent>
              <Skeleton variant="text" width="30%" height={32} />
              <Skeleton variant="text" width="90%" />
              <Skeleton variant="text" width="70%" />
            </CardContent>
          </Card>
          <Card variant="outlined">
            <CardContent>
              <Skeleton variant="text" width="30%" height={32} />
              <Skeleton variant="text" width="60%" />
            </CardContent>
          </Card>
        </Stack>
        <Stack flex={1} spacing={3}>
          <Card variant="outlined">
            <CardContent>
              <Skeleton variant="text" width="50%" height={32} />
              <Skeleton variant="text" width="80%" />
              <Skeleton variant="text" width="60%" />
            </CardContent>
          </Card>
        </Stack>
      </Stack>
    </Stack>
  );
}
