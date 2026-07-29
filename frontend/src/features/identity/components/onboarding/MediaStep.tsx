import { useFormContext } from "react-hook-form";
import { Box, Stack, Typography, alpha } from "@mui/material";
import PhotoCameraRoundedIcon from "@mui/icons-material/PhotoCameraRounded";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import type { TutorOnboardingFormValues } from "@/features/identity/validation/tutorOnboardingSchema";

/**
 * Step 3 — Profile Media. Plain URLs the Tutor pastes in, pointing at
 * content they already host elsewhere — TutorFlow has no file-upload
 * capability in v1 (ADR-024's own Media section explains why: no new
 * storage/CDN dependency for a UI-only-scoped wizard).
 */
export function MediaStep() {
  const { watch } = useFormContext<TutorOnboardingFormValues>();
  const [photoUrl, introVideoUrl, galleryImageUrls] = watch(["photoUrl", "introVideoUrl", "galleryImageUrls"]);
  const hasAnyMedia = Boolean(photoUrl || introVideoUrl || galleryImageUrls);

  return (
    <Stack spacing={2.5}>
      <Typography variant="body2" color="text.secondary">
        Paste links to content you already host elsewhere (e.g. YouTube, an image host) — TutorFlow
        doesn&rsquo;t offer file uploads yet.
      </Typography>
      {!hasAnyMedia ? (
        <Stack
          direction="row"
          spacing={1.5}
          alignItems="flex-start"
          sx={{
            p: 2,
            borderRadius: 1,
            bgcolor: (t) => alpha(t.palette.primary.main, t.palette.mode === "dark" ? 0.12 : 0.06),
          }}
        >
          <PhotoCameraRoundedIcon color="primary" fontSize="small" aria-hidden="true" sx={{ mt: 0.25 }} />
          <Box>
            <Typography variant="body2" fontWeight={600}>
              A photo makes a real difference
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Profiles with a clear, friendly headshot and a short introduction video get noticed first —
              add at least a photo URL if you have one.
            </Typography>
          </Box>
        </Stack>
      ) : null}
      <FormTextField name="photoUrl" label="Profile photo URL" placeholder="https://…" />
      <FormTextField name="introVideoUrl" label="Introduction video URL" placeholder="https://…" />
      <FormTextField
        name="galleryImageUrls"
        label="Additional gallery image URLs (comma-separated, optional)"
        placeholder="https://…, https://…"
      />
    </Stack>
  );
}
