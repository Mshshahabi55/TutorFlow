import { useCallback } from "react";
import { Box, Button, Stack, Typography, alpha } from "@mui/material";
import RocketLaunchRoundedIcon from "@mui/icons-material/RocketLaunchRounded";
import ScheduleRoundedIcon from "@mui/icons-material/ScheduleRounded";
import CheckCircleRoundedIcon from "@mui/icons-material/CheckCircleRounded";

const PROFILE_BENEFITS = [
  "Appear in Student search results once an Admin approves your profile",
  "Show up as a trustworthy, complete profile — not a bare listing",
  "Let Students see your rate, subjects, and availability up front",
] as const;

export interface OnboardingWelcomeScreenProps {
  /** The wizard's own step labels — rendered as a short "what we'll cover" list, never duplicated elsewhere. */
  steps: readonly string[];
  onStart: () => void;
}

/**
 * Shown once, before Step 1, for a Tutor who has never opened the wizard
 * (`TutorOnboardingWizardPage` gates this on whether a step has ever been
 * persisted to `localStorage` for this tutor — no separate "seen" flag).
 * Self-focuses its own region on mount, the same convention every step
 * panel already uses, so screen-reader users hear the welcome copy first
 * rather than focus silently landing nowhere.
 */
export function OnboardingWelcomeScreen({ steps, onStart }: OnboardingWelcomeScreenProps) {
  const focusRegion = useCallback((node: HTMLElement | null) => {
    node?.focus();
  }, []);

  return (
    <Stack
      ref={focusRegion}
      tabIndex={-1}
      role="region"
      aria-label="Welcome"
      spacing={4}
      sx={{ outline: "none", py: { xs: 1, sm: 2 } }}
    >
      <Stack spacing={1.5} alignItems="flex-start">
        <Box
          aria-hidden="true"
          sx={{
            width: 56,
            height: 56,
            borderRadius: "50%",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            bgcolor: (t) => alpha(t.palette.primary.main, t.palette.mode === "dark" ? 0.18 : 0.1),
            color: "primary.main",
          }}
        >
          <RocketLaunchRoundedIcon fontSize="medium" />
        </Box>
        <Typography variant="h5" component="h5" fontWeight={700}>
          Welcome — let&rsquo;s build your Tutor profile
        </Typography>
        <Typography variant="body1" color="text.secondary">
          A richer profile helps Students trust and choose you. We&rsquo;ll walk through {steps.length} short
          steps — every step saves automatically, so you can leave any time and pick up right where you left
          off.
        </Typography>
      </Stack>

      <Stack direction="row" spacing={1} alignItems="center" color="text.secondary">
        <ScheduleRoundedIcon fontSize="small" aria-hidden="true" />
        <Typography variant="body2">About 8–10 minutes</Typography>
      </Stack>

      <Stack spacing={1.25}>
        <Typography variant="subtitle2" fontWeight={600}>
          What we&rsquo;ll cover
        </Typography>
        <Stack component="ol" spacing={1} sx={{ listStyle: "none", m: 0, p: 0 }}>
          {steps.map((label, index) => (
            <Stack key={label} component="li" direction="row" spacing={1.25} alignItems="center">
              <Typography variant="body2" fontWeight={700} color="primary.main" sx={{ width: 20, flexShrink: 0 }}>
                {index + 1}
              </Typography>
              <Typography variant="body2">{label}</Typography>
            </Stack>
          ))}
        </Stack>
      </Stack>

      <Stack spacing={1.25}>
        <Typography variant="subtitle2" fontWeight={600}>
          Why complete your profile
        </Typography>
        <Stack spacing={0.75}>
          {PROFILE_BENEFITS.map((benefit) => (
            <Stack key={benefit} direction="row" spacing={1} alignItems="flex-start">
              <CheckCircleRoundedIcon color="success" fontSize="small" aria-hidden="true" sx={{ mt: 0.25 }} />
              <Typography variant="body2" color="text.secondary">
                {benefit}
              </Typography>
            </Stack>
          ))}
        </Stack>
      </Stack>

      <Button type="button" variant="contained" size="large" onClick={onStart} sx={{ alignSelf: "flex-start" }}>
        Get started
      </Button>
    </Stack>
  );
}
