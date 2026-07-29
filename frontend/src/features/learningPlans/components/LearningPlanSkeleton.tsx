import { Card, CardContent, Skeleton, Stack } from "@mui/material";
import { LEARNING_PLAN_CARD_SX } from "@/features/learningPlans/components/LearningPlanCard";

/** Matches `LearningPlanCard`'s own layout exactly, so a resolved plan list never shifts the grid — same convention as `TutorCardSkeleton`. */
export function LearningPlanSkeleton() {
  return (
    <Card variant="outlined" sx={LEARNING_PLAN_CARD_SX} data-testid="learning-plan-card-skeleton">
      <CardContent>
        <Stack direction="row" justifyContent="space-between">
          <Skeleton variant="text" width="60%" height={28} />
          <Skeleton variant="rounded" width={56} height={24} />
        </Stack>
        <Skeleton variant="rounded" width={80} height={24} sx={{ mt: 1 }} />
        <Stack spacing={0.5} mt={1.5}>
          <Skeleton variant="text" width="90%" />
          <Skeleton variant="text" width="70%" />
        </Stack>
        <Stack spacing={0.75} mt={1.5}>
          <Skeleton variant="text" width="50%" />
          <Skeleton variant="text" width="45%" />
        </Stack>
        <Skeleton variant="text" width="35%" height={28} sx={{ mt: 1.5 }} />
        <Skeleton variant="rectangular" height={36} sx={{ mt: 2, borderRadius: 1 }} />
      </CardContent>
    </Card>
  );
}
