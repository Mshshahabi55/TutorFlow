import { Link as RouterLink } from "react-router-dom";
import { Avatar, Box, Button, Chip, Stack, Typography } from "@mui/material";
import PersonRoundedIcon from "@mui/icons-material/PersonRounded";
import VerifiedRoundedIcon from "@mui/icons-material/VerifiedRounded";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import { paths } from "@/routes/paths";
import type { TutorDto } from "@/services/api/dtos";

/**
 * The Tutor Profile's own `<h1>` — there is no separate generic
 * "Tutor detail" page title above it (Phase 3 Step 3): the entity a
 * marketplace profile is about belongs at the top of the heading
 * hierarchy, not a second, less meaningful heading above it.
 *
 * `TutorDto` has no name field, so `subject` stands in as the heading —
 * same convention as the Directory's `TutorCard` (Phase 3 Step 2). No
 * bio/introduction is rendered because none exists in the API; a
 * "Message" secondary action is omitted for the same reason — there is no
 * messaging capability anywhere in this app to link to.
 */
export function TutorProfileHero({ tutor }: { tutor: TutorDto }) {
  return (
    <Stack
      direction={{ xs: "column", sm: "row" }}
      spacing={3}
      alignItems={{ xs: "flex-start", sm: "center" }}
    >
      <Avatar sx={{ width: 88, height: 88, bgcolor: "action.selected" }}>
        <PersonRoundedIcon sx={{ fontSize: 48 }} color="disabled" aria-hidden="true" />
      </Avatar>

      <Box flex={1} minWidth={0}>
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
          <Typography variant="h4" component="h1">
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
        <Stack direction="row" spacing={1} flexWrap="wrap" mt={0.5}>
          {tutor.location ? (
            <Typography variant="body1" color="text.secondary">
              {tutor.location}
            </Typography>
          ) : null}
          {tutor.language ? (
            <Typography variant="body1" color="text.secondary">
              {tutor.location ? `· Speaks ${tutor.language}` : `Speaks ${tutor.language}`}
            </Typography>
          ) : null}
        </Stack>
      </Box>

      <Button
        component={RouterLink}
        to={`${paths.scheduling.bookSession}?tutorId=${tutor.tutorId}`}
        variant="contained"
        size="large"
        startIcon={<EventRoundedIcon />}
        sx={{ flexShrink: 0, alignSelf: { xs: "stretch", sm: "center" } }}
      >
        Book Session
      </Button>
    </Stack>
  );
}
