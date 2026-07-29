import { Skeleton, Stack } from "@mui/material";

/**
 * Mirrors `TutorOnboardingWizardPage`'s real shape (title block, stepper
 * row, a handful of field-height bars, a button) so resolving `useTutor`
 * never shifts the layout — the same convention `TutorProfileSkeleton`
 * already established for the profile page.
 */
export function OnboardingWizardSkeleton() {
  return (
    <Stack spacing={3} data-testid="onboarding-wizard-skeleton">
      <Stack spacing={1}>
        <Skeleton variant="text" width="55%" height={40} />
        <Skeleton variant="text" width="80%" />
      </Stack>

      <Stack direction="row" spacing={2} sx={{ display: { xs: "none", md: "flex" } }}>
        {Array.from({ length: 7 }, (_, index) => (
          <Stack key={index} flex={1} spacing={1} alignItems="center">
            <Skeleton variant="circular" width={32} height={32} />
            <Skeleton variant="text" width="70%" />
          </Stack>
        ))}
      </Stack>
      <Stack spacing={0.75} sx={{ display: { xs: "flex", md: "none" } }}>
        <Skeleton variant="text" width="30%" />
        <Skeleton variant="text" width="50%" height={28} />
        <Skeleton variant="rectangular" height={6} sx={{ borderRadius: 999 }} />
      </Stack>

      <Stack spacing={2.5}>
        {Array.from({ length: 4 }, (_, index) => (
          <Skeleton key={index} variant="rectangular" height={56} sx={{ borderRadius: 1 }} />
        ))}
      </Stack>

      <Skeleton variant="rectangular" width={140} height={44} sx={{ borderRadius: 1 }} />
    </Stack>
  );
}
