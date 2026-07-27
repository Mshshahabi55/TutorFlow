import { Card, CardContent, Skeleton, Stack } from "@mui/material";
import { TUTOR_CARD_SX } from "@/features/discovery/components/TutorCard";

/** Matches `TutorCard`'s own layout exactly, so a resolved search never shifts the grid. */
export function TutorCardSkeleton() {
  return (
    <Card variant="outlined" sx={TUTOR_CARD_SX} data-testid="tutor-card-skeleton">
      <CardContent>
        <Stack direction="row" spacing={1.5} alignItems="center">
          <Skeleton variant="circular" width={48} height={48} />
          <Stack spacing={0.5} flex={1}>
            <Skeleton variant="text" width="70%" height={24} />
            <Skeleton variant="text" width="50%" />
          </Stack>
        </Stack>
        <Stack spacing={1} mt={1.5}>
          <Skeleton variant="text" width="40%" />
          <Skeleton variant="text" width="55%" />
          <Skeleton variant="text" width="45%" />
        </Stack>
        <Skeleton variant="rectangular" height={36} sx={{ mt: 2, borderRadius: 1 }} />
      </CardContent>
    </Card>
  );
}
