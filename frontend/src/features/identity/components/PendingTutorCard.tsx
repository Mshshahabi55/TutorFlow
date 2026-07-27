import { Card, CardActions, CardContent, Stack, Typography } from "@mui/material";
import { TutorApprovalActions } from "@/features/identity/components/TutorApprovalActions";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { formatToman } from "@/shared/money/rial";
import type { TutorDto } from "@/services/api/dtos";

export interface PendingTutorCardProps {
  tutor: TutorDto;
}

/**
 * One Tutor awaiting Admin review — reused by both `AdminPendingTutorsPage`
 * (the full moderation queue) and `AdminDashboardPage`'s preview (same
 * `usePendingTutors` data, no duplicate request). Uses the exact
 * "Pending approval"/"Approved" wording `TutorDetailPage`'s own
 * Manage-listing section already established — the same standardized
 * badge, not a new status. `TutorApprovalActions` (Approve/Suspend) is
 * reused unchanged; moderation logic itself is untouched.
 */
export function PendingTutorCard({ tutor }: PendingTutorCardProps) {
  return (
    <Card variant="outlined">
      <CardContent>
        <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}>
          <Typography variant="subtitle1" fontWeight={600}>
            {tutor.subject ?? "Tutor"}
          </Typography>
          <Stack direction="row" spacing={1} flexWrap="wrap">
            <StatusPill
              label={tutor.isApproved ? "Approved" : "Pending approval"}
              tone={tutor.isApproved ? "success" : "warning"}
            />
            {tutor.isSuspended ? <StatusPill label="Suspended" tone="critical" /> : null}
          </Stack>
        </Stack>
        <Stack direction="row" spacing={1} flexWrap="wrap" mt={0.5}>
          {tutor.language ? (
            <Typography variant="body2" color="text.secondary">
              Speaks {tutor.language}
            </Typography>
          ) : null}
          {tutor.location ? (
            <Typography variant="body2" color="text.secondary">
              {tutor.location}
            </Typography>
          ) : null}
        </Stack>
        <Typography variant="body2" fontWeight={600} mt={0.5}>
          {tutor.hourlyRate !== null ? `${formatToman(tutor.hourlyRate)} Toman/hr` : "Rate not set"}
        </Typography>
      </CardContent>
      <CardActions sx={{ px: 2, pb: 2, pt: 0 }}>
        <TutorApprovalActions tutor={tutor} />
      </CardActions>
    </Card>
  );
}
