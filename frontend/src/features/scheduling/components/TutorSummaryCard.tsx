import { Avatar, Box, Card, CardContent, Chip, Stack, Typography } from "@mui/material";
import PersonRoundedIcon from "@mui/icons-material/PersonRounded";
import VerifiedRoundedIcon from "@mui/icons-material/VerifiedRounded";
import { formatToman } from "@/shared/money/rial";
import type { TutorDto } from "@/services/api/dtos";

/**
 * "Who am I booking?" — reuses the exact same `TutorDto` fields the
 * Tutor Directory's `TutorCard` (Phase 3 Step 2) and the Tutor Profile's
 * `TutorProfileHero` (Phase 3 Step 3) already show. `subject` stands in
 * for a name, same convention, since `TutorDto` has no name field.
 */
export function TutorSummaryCard({ tutor }: { tutor: TutorDto }) {
  return (
    <Card variant="outlined" aria-label={`Booking with ${tutor.subject ?? "Tutor"}`}>
      <CardContent>
        <Typography variant="overline" color="text.secondary">
          Booking with
        </Typography>
        <Stack direction="row" spacing={2} alignItems="center" mt={0.5}>
          <Avatar sx={{ width: 56, height: 56, bgcolor: "action.selected" }}>
            <PersonRoundedIcon color="disabled" aria-hidden="true" />
          </Avatar>
          <Box flex={1} minWidth={0}>
            <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
              <Typography variant="subtitle1" fontWeight={600} noWrap>
                {tutor.subject ?? "Tutor"}
              </Typography>
              {tutor.isApproved ? (
                <Chip
                  icon={<VerifiedRoundedIcon fontSize="small" />}
                  label="Verified"
                  color="primary"
                  size="small"
                />
              ) : null}
            </Stack>
            {tutor.language ? (
              <Typography variant="body2" color="text.secondary">
                Speaks {tutor.language}
              </Typography>
            ) : null}
          </Box>
          <Typography variant="subtitle1" fontWeight={600} whiteSpace="nowrap">
            {tutor.hourlyRate !== null ? `${formatToman(tutor.hourlyRate)} Toman/hr` : "Rate not set"}
          </Typography>
        </Stack>
      </CardContent>
    </Card>
  );
}
