import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { PlanBadge } from "@/features/learningPlans/components/PlanBadge";
import { PlanFeatureList } from "@/features/learningPlans/components/PlanFeatureList";
import { formatToman } from "@/shared/money/rial";
import type { LearningPlanPreview } from "@/features/learningPlans/types";

/** Shared by `LearningPlanCard` and `LearningPlanSkeleton` so a loading grid never shifts once real cards arrive — same convention as `TUTOR_CARD_SX`. */
export const LEARNING_PLAN_CARD_SX = { flex: "1 1 280px", minWidth: 280, maxWidth: 360 } as const;

export interface LearningPlanCardProps {
  plan: LearningPlanPreview;
  /** Omitted wherever no real enrollment capability exists yet — the button still renders (so the card's layout is final), but disabled rather than wired to a fake action. */
  onEnroll?: (plan: LearningPlanPreview) => void;
}

/**
 * The one Learning Plan card used everywhere a plan is shown (Tutor Profile,
 * Tutor Dashboard, and any future plan browse/detail view) — Title,
 * duration/status badges, description, `PlanFeatureList`, price, and a
 * single primary "Enroll" action. No page in this app renders one with real
 * data today (no Learning Plan backend exists — see `features/learningPlans/types.ts`);
 * it's demonstrated with sample data only in the dev-only Style Guide.
 */
export function LearningPlanCard({ plan, onEnroll }: LearningPlanCardProps) {
  return (
    <Card variant="outlined" sx={LEARNING_PLAN_CARD_SX}>
      <CardContent sx={{ display: "flex", flexDirection: "column", height: "100%" }}>
        <Stack spacing={1.5} flex={1}>
          <Stack direction="row" justifyContent="space-between" alignItems="flex-start" flexWrap="wrap" gap={1}>
            <Typography variant="subtitle1" component="h4" fontWeight={600}>
              {plan.title}
            </Typography>
            {plan.status ? <PlanBadge label={plan.status} /> : null}
          </Stack>

          <Stack direction="row" spacing={1} flexWrap="wrap">
            <PlanBadge label={`${plan.durationDays} Days`} tone="info" />
          </Stack>

          <Typography variant="body2" color="text.secondary">
            {plan.description}
          </Typography>

          <PlanFeatureList
            features={[
              `${plan.sessionsPerWeek} ${plan.sessionsPerWeek === 1 ? "lesson" : "lessons"} / week`,
              `${plan.totalSessions} total ${plan.totalSessions === 1 ? "lesson" : "lessons"}`,
            ]}
          />

          <Typography variant="subtitle1" fontWeight={700}>
            {formatToman(plan.price)} Toman
          </Typography>
        </Stack>

        <Button
          variant="contained"
          fullWidth
          disabled={!onEnroll}
          onClick={() => onEnroll?.(plan)}
          sx={{ mt: 2 }}
        >
          Enroll
        </Button>
      </CardContent>
    </Card>
  );
}
