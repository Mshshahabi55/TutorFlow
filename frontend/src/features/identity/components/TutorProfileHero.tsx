import { Link as RouterLink, useNavigate } from "react-router-dom";
import { Avatar, Box, Button, Chip, Link as MuiLink, Stack, Typography, alpha } from "@mui/material";
import PersonRoundedIcon from "@mui/icons-material/PersonRounded";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import ChatBubbleOutlineRoundedIcon from "@mui/icons-material/ChatBubbleOutlineRounded";
import TranslateRoundedIcon from "@mui/icons-material/TranslateRounded";
import { useStartConversation } from "@/features/communication/hooks/useConversationMutations";
import { FavoriteToggleButton } from "@/features/discovery/components/FavoriteToggleButton";
import { useNextAvailableLabel } from "@/features/scheduling/hooks/useNextAvailableLabel";
import { TrustIndicators } from "@/features/identity/components/TrustIndicators";
import { useEffectiveRole } from "@/shared/hooks/useEffectiveRole";
import { useNotification } from "@/shared/hooks/useNotification";
import { formatToman } from "@/shared/money/rial";
import { paths } from "@/routes/paths";
import type { TutorDto } from "@/services/api/dtos";

/**
 * RC5.1: real messaging now exists (docs/adr/ADR-022-...) — a Student or
 * Parent/Guardian may start (or resume, idempotently) a Conversation with
 * this Tutor from here. Restricted to those two roles because
 * StartConversationCommandHandler itself restricts who may start a
 * genuinely new Conversation with a Tutor the same way (a Tutor viewing
 * their own profile, or an Admin, has no reason to message via this CTA).
 */
function SendMessageAction({ tutorId }: { tutorId: string }) {
  const role = useEffectiveRole();
  const navigate = useNavigate();
  const { notify } = useNotification();
  const startConversation = useStartConversation();

  if (role !== "Student" && role !== "ParentGuardian") {
    return null;
  }

  function handleClick() {
    startConversation.mutate(tutorId, {
      onSuccess: (conversation) => {
        void navigate(paths.messages.conversationDetail(conversation.conversationId));
      },
      onError: () => {
        notify({ message: "Couldn't start a conversation. Please try again.", severity: "error" });
      },
    });
  }

  return (
    <Button
      variant="outlined"
      size="large"
      startIcon={<ChatBubbleOutlineRoundedIcon />}
      onClick={handleClick}
      disabled={startConversation.isPending}
    >
      Send Message
    </Button>
  );
}

/**
 * The Tutor Profile's own `<h1>` — there is no separate generic
 * "Tutor detail" page title above it (Phase 3 Step 3): the entity a
 * marketplace profile is about belongs at the top of the heading
 * hierarchy, not a second, less meaningful heading above it.
 *
 * Phase 9: `TutorDto` was enriched by ADR-024 with `displayName`,
 * `headline`, and `photoUrl` — all wired in here now (a real photo instead
 * of a generic avatar icon, a real name instead of `subject` standing in
 * for one, and a headline subtitle), rather than continuing to treat data
 * that's actually available as if it were still missing.
 */
export function TutorProfileHero({ tutor }: { tutor: TutorDto }) {
  const nextAvailableLabel = useNextAvailableLabel(tutor.tutorId);

  return (
    <Stack
      direction={{ xs: "column", sm: "row" }}
      spacing={3}
      alignItems={{ xs: "flex-start", sm: "center" }}
    >
      <Avatar
        src={tutor.photoUrl ?? undefined}
        sx={{
          width: 96,
          height: 96,
          bgcolor: (t) => alpha(t.palette.primary.main, t.palette.mode === "dark" ? 0.22 : 0.12),
          color: "primary.main",
        }}
      >
        <PersonRoundedIcon sx={{ fontSize: 52 }} aria-hidden="true" />
      </Avatar>

      <Box flex={1} minWidth={0}>
        <Stack direction="row" spacing={0.5} alignItems="center">
          <Typography variant="h4" component="h1">
            {tutor.displayName ?? tutor.subject ?? "Tutor"}
          </Typography>
          <FavoriteToggleButton tutorId={tutor.tutorId} size="medium" />
        </Stack>
        {tutor.headline ? (
          <Typography variant="body1" color="text.secondary" mt={0.25}>
            {tutor.headline}
          </Typography>
        ) : null}
        <Stack direction="row" spacing={1} flexWrap="wrap" alignItems="center" mt={0.5}>
          {tutor.location ? (
            <Typography variant="body1" color="text.secondary">
              {tutor.location}
            </Typography>
          ) : null}
          {tutor.language ? (
            <Chip
              icon={<TranslateRoundedIcon fontSize="small" />}
              label={tutor.language}
              size="small"
              variant="outlined"
            />
          ) : null}
        </Stack>
        <Box mt={1.5}>
          <TrustIndicators tutor={tutor} nextAvailableLabel={nextAvailableLabel} showLanguage={false} />
        </Box>
      </Box>

      <Stack spacing={1} alignItems={{ xs: "stretch", sm: "center" }} sx={{ flexShrink: 0 }}>
        <Typography
          variant="h5"
          component="p"
          fontWeight={700}
          color={tutor.hourlyRate !== null ? "primary.main" : "text.secondary"}
          textAlign={{ xs: "left", sm: "center" }}
        >
          {tutor.hourlyRate !== null ? `${formatToman(tutor.hourlyRate)} Toman/hr` : "Rate not set"}
        </Typography>
        <Button
          component={RouterLink}
          to={`${paths.scheduling.bookSession}?tutorId=${tutor.tutorId}`}
          variant="contained"
          size="large"
          startIcon={<EventRoundedIcon />}
        >
          Book Lesson
        </Button>
        <SendMessageAction tutorId={tutor.tutorId} />
        <MuiLink
          component={RouterLink}
          to={`${paths.identity.tutorDetail(tutor.tutorId)}#learning-plans`}
          variant="body2"
          underline="hover"
          textAlign="center"
        >
          View Learning Plans
        </MuiLink>
      </Stack>
    </Stack>
  );
}
