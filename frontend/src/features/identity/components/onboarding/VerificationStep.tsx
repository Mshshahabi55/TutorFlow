import { Stack, Typography } from "@mui/material";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import type { TutorDto } from "@/services/api/dtos";

/**
 * Step 6 — Verification. ADR-024's own Verification section: v1 is
 * self-attestation only, reviewed holistically by the existing Admin
 * approve/suspend gate — there is no separate identity/education/
 * certificate/background-check status, because none of those are real
 * capabilities in this API. Showing four independent-looking status
 * checks with no real check behind three of them would be exactly the
 * kind of fabricated UI this whole redesign initiative has otherwise been
 * careful to avoid — this step shows only the two facts that are real.
 */
export function VerificationStep({ tutor }: { tutor: TutorDto }) {
  return (
    <Stack spacing={2.5}>
      <Typography variant="body1" color="text.secondary">
        TutorFlow doesn&rsquo;t verify documents or run background checks in v1. Once you publish
        your profile, an Admin reviews everything you&rsquo;ve entered and approves your account —
        the same review every Tutor already goes through.
      </Typography>
      <Stack direction="row" spacing={1} flexWrap="wrap">
        <StatusPill
          label={tutor.profileStatus === "Submitted" ? "Profile submitted" : "Profile in progress"}
          tone={tutor.profileStatus === "Submitted" ? "info" : "neutral"}
        />
        <StatusPill
          label={tutor.isApproved ? "Approved" : "Pending Admin review"}
          tone={tutor.isApproved ? "success" : "warning"}
        />
      </Stack>
    </Stack>
  );
}
