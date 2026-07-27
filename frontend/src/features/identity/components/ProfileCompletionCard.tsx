import { Link as RouterLink } from "react-router-dom";
import { Button, LinearProgress, Stack, Typography } from "@mui/material";
import CheckCircleRoundedIcon from "@mui/icons-material/CheckCircleRounded";
import RadioButtonUncheckedRoundedIcon from "@mui/icons-material/RadioButtonUncheckedRounded";
import { SectionCard } from "@/shared/components/SectionCard";
import type { ProfileCompletion } from "@/features/identity/utils/profileCompletion";
import { paths } from "@/routes/paths";

export interface ProfileCompletionCardProps {
  completion: ProfileCompletion;
  tutorId: string;
}

/**
 * Every item here mirrors `deriveProfileCompletion` — real `TutorDto`
 * fields (plus a real Availability check), never a placeholder value.
 * "Verification completed" never gets the "Complete your profile" CTA
 * appended below: it's an Admin decision the Tutor can't act on directly.
 */
export function ProfileCompletionCard({ completion, tutorId }: ProfileCompletionCardProps) {
  const progressPercent = (completion.completedCount / completion.totalCount) * 100;

  return (
    <SectionCard title="Profile Completion">
      <Stack spacing={2}>
        <Stack spacing={1}>
          <LinearProgress
            variant="determinate"
            value={progressPercent}
            aria-label="Profile completion progress"
            sx={{ height: 8, borderRadius: 999 }}
          />
          <Typography variant="body2" color="text.secondary">
            {completion.isComplete
              ? "Your profile is complete."
              : `${completion.completedCount} of ${completion.totalCount} steps complete.`}
          </Typography>
        </Stack>

        <Stack spacing={1}>
          {completion.items.map((item) => (
            <Stack key={item.label} direction="row" spacing={1} alignItems="center">
              {item.done ? (
                <CheckCircleRoundedIcon color="success" fontSize="small" aria-hidden="true" />
              ) : (
                <RadioButtonUncheckedRoundedIcon color="disabled" fontSize="small" aria-hidden="true" />
              )}
              <Typography variant="body2" color={item.done ? "text.primary" : "text.secondary"}>
                {item.label}
              </Typography>
            </Stack>
          ))}
        </Stack>

        {completion.isComplete ? null : (
          <Button
            component={RouterLink}
            to={paths.identity.tutorEdit(tutorId)}
            variant="outlined"
            size="small"
            sx={{ alignSelf: "flex-start" }}
          >
            Complete your profile
          </Button>
        )}
      </Stack>
    </SectionCard>
  );
}
