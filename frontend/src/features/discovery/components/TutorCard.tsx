import { Link as RouterLink } from "react-router-dom";
import { Avatar, Box, Button, Card, CardContent, Chip, Stack, Typography } from "@mui/material";
import PersonRoundedIcon from "@mui/icons-material/PersonRounded";
import VerifiedRoundedIcon from "@mui/icons-material/VerifiedRounded";
import { formatToman } from "@/shared/money/rial";
import { formatMinutesList } from "@/shared/utils/duration";
import { paths } from "@/routes/paths";
import type { TutorDto } from "@/services/api/dtos";

/** Shared by `TutorCard` and `TutorCardSkeleton` so a loading grid never shifts once real cards arrive. */
export const TUTOR_CARD_SX = { flex: "1 1 280px", minWidth: 280, maxWidth: 360 } as const;

/**
 * `TutorDto` has no name field (see `services/api/dtos.ts` — no field is
 * added, renamed, or reshaped from the backend contract), so `subject`
 * stands in as the card's heading, same convention already used by the
 * Student Dashboard's Recommended Tutors widget.
 */
function TutorCardHeader({ tutor }: { tutor: TutorDto }) {
  return (
    <Stack direction="row" spacing={1.5} alignItems="center">
      <Avatar sx={{ width: 48, height: 48, bgcolor: "action.selected" }}>
        <PersonRoundedIcon color="disabled" aria-hidden="true" />
      </Avatar>
      <Box minWidth={0}>
        <Stack direction="row" spacing={0.5} alignItems="center">
          <Typography variant="subtitle1" component="h3" fontWeight={600} noWrap>
            {tutor.subject ?? "Tutor"}
          </Typography>
          {tutor.isApproved ? (
            <VerifiedRoundedIcon fontSize="small" color="primary" titleAccess="Verified tutor" />
          ) : null}
        </Stack>
        {tutor.location ? (
          <Typography variant="body2" color="text.secondary" noWrap>
            {tutor.location}
          </Typography>
        ) : null}
      </Box>
    </Stack>
  );
}

/** Every field here comes straight off `TutorDto` — no rating, review count, or bio is shown because none exists in the API. */
function TutorCardMeta({ tutor }: { tutor: TutorDto }) {
  return (
    <Stack spacing={1} mt={1.5}>
      <Stack direction="row" flexWrap="wrap" gap={0.75}>
        {tutor.subject ? <Chip label={tutor.subject} size="small" /> : null}
        {tutor.language ? <Chip label={tutor.language} size="small" variant="outlined" /> : null}
      </Stack>
      {tutor.offeredDurations.length > 0 ? (
        <Typography variant="body2" color="text.secondary">
          Sessions: {formatMinutesList(tutor.offeredDurations)} min
        </Typography>
      ) : null}
      <Typography variant="subtitle2" fontWeight={600}>
        {tutor.hourlyRate !== null ? `${formatToman(tutor.hourlyRate)} Toman/hr` : "Rate not set"}
      </Typography>
    </Stack>
  );
}

function TutorCardActions({ tutorId }: { tutorId: string }) {
  return (
    <Stack spacing={1}>
      <Button
        component={RouterLink}
        to={`${paths.scheduling.bookSession}?tutorId=${tutorId}`}
        variant="contained"
        fullWidth
      >
        Book Lesson
      </Button>
      <Button component={RouterLink} to={paths.identity.tutorDetail(tutorId)} variant="outlined" fullWidth>
        View profile
      </Button>
    </Stack>
  );
}

export function TutorCard({ tutor }: { tutor: TutorDto }) {
  return (
    <Card variant="outlined" sx={TUTOR_CARD_SX}>
      <CardContent sx={{ display: "flex", flexDirection: "column", height: "100%" }}>
        <TutorCardHeader tutor={tutor} />
        <TutorCardMeta tutor={tutor} />
        <Box flexGrow={1} minHeight={16} />
        <TutorCardActions tutorId={tutor.tutorId} />
      </CardContent>
    </Card>
  );
}
