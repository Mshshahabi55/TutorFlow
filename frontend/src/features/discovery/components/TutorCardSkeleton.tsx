import { Card, CardContent, Skeleton, Stack } from "@mui/material";
import { TUTOR_CARD_SX } from "@/features/discovery/components/TutorCard";

/** Matches `TutorCard`'s own layout exactly, so a resolved search never shifts the grid. */
export function TutorCardSkeleton() {
  return (
    <Card variant="outlined" sx={TUTOR_CARD_SX} data-testid="tutor-card-skeleton">
      <CardContent>
        <Stack direction="row" spacing={1.5} alignItems="flex-start">
          <Skeleton variant="circular" width={56} height={56} />
          <Stack spacing={0.5} flex={1}>
            <Skeleton variant="text" width="70%" height={24} />
            <Skeleton variant="text" width="50%" />
            <Skeleton variant="text" width="40%" />
          </Stack>
        </Stack>
        <Skeleton variant="text" width="30%" height={20} sx={{ mt: 1 }} />
        <Skeleton variant="text" width="35%" height={32} sx={{ mt: 1.5 }} />
        <Stack spacing={1} mt={1}>
          <Stack direction="row" spacing={0.75}>
            <Skeleton variant="rounded" width={70} height={24} />
            <Skeleton variant="rounded" width={90} height={24} />
          </Stack>
          <Skeleton variant="text" width="45%" />
          <Skeleton variant="rounded" width={140} height={24} />
        </Stack>
        <Skeleton variant="rectangular" height={36} sx={{ mt: 2, borderRadius: 1 }} />
      </CardContent>
    </Card>
  );
}
