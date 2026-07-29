import { Stack, Typography } from "@mui/material";
import CheckCircleOutlineRoundedIcon from "@mui/icons-material/CheckCircleOutlineRounded";

export interface PlanFeatureListProps {
  features: string[];
}

/** A short bullet list of plan facts (e.g. "2 lessons / week", "8 total lessons") — the one list treatment `LearningPlanCard` and any future plan-detail view share. */
export function PlanFeatureList({ features }: PlanFeatureListProps) {
  return (
    <Stack component="ul" spacing={0.75} sx={{ listStyle: "none", pl: 0, m: 0 }}>
      {features.map((feature) => (
        <Stack key={feature} component="li" direction="row" spacing={1} alignItems="center">
          <CheckCircleOutlineRoundedIcon fontSize="small" color="action" aria-hidden="true" />
          <Typography variant="body2" color="text.secondary">
            {feature}
          </Typography>
        </Stack>
      ))}
    </Stack>
  );
}
