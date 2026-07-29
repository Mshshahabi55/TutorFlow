import { Link as RouterLink } from "react-router-dom";
import { Avatar, Box, Button, Card, CardContent, Chip, Link as MuiLink, Stack, Typography, alpha } from "@mui/material";
import PersonRoundedIcon from "@mui/icons-material/PersonRounded";
import TranslateRoundedIcon from "@mui/icons-material/TranslateRounded";
import { PlanBadge } from "@/features/learningPlans/components/PlanBadge";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { TrustIndicators } from "@/features/identity/components/TrustIndicators";
import { formatToman } from "@/shared/money/rial";
import { formatMinutesList } from "@/shared/utils/duration";
import { paths } from "@/routes/paths";
import type { TutorDto } from "@/services/api/dtos";

/** Shared by `TutorCard` and `TutorCardSkeleton` so a loading grid never shifts once real cards arrive. */
export const TUTOR_CARD_SX = { flex: "1 1 280px", minWidth: 280, maxWidth: 360, height: "100%" } as const;

/**
 * Phase 9: `displayName`/`headline`/`photoUrl` were added to `TutorDto` by
 * ADR-024 but weren't wired into this card until now — `subject` remains
 * the fallback heading only when `displayName` is absent (older/incomplete
 * profiles), not the primary source of truth it used to be. Avatar shows a
 * real photo when set, falling back to the same brand-tinted placeholder
 * circle as before (never a broken image, never assumed to exist).
 */
function TutorCardHeader({ tutor }: { tutor: TutorDto }) {
  return (
    <Stack direction="row" spacing={1.5} alignItems="flex-start">
      <Avatar
        src={tutor.photoUrl ?? undefined}
        sx={{
          width: 56,
          height: 56,
          bgcolor: (t) => alpha(t.palette.primary.main, t.palette.mode === "dark" ? 0.22 : 0.12),
          color: "primary.main",
        }}
      >
        <PersonRoundedIcon aria-hidden="true" />
      </Avatar>
      <Box minWidth={0}>
        <Typography variant="subtitle1" component="h3" fontWeight={600} noWrap>
          {tutor.displayName ?? tutor.subject ?? "Tutor"}
        </Typography>
        {tutor.headline ? (
          <Typography variant="body2" color="text.secondary" noWrap>
            {tutor.headline}
          </Typography>
        ) : null}
        {tutor.location ? (
          <Typography variant="body2" color="text.secondary" noWrap>
            {tutor.location}
          </Typography>
        ) : null}
      </Box>
    </Stack>
  );
}

/**
 * The hourly rate is a search card's single strongest comparison-shopping
 * signal (alongside subject) — given its own prominent row instead of
 * being buried as one more line among several chips, so a Student scanning
 * a results grid can compare prices at a glance.
 */
function TutorCardPrice({ tutor }: { tutor: TutorDto }) {
  if (tutor.hourlyRate === null) {
    return (
      <Typography variant="body1" color="text.secondary" mt={1.5}>
        Rate not set
      </Typography>
    );
  }

  return (
    <Typography variant="h6" component="p" fontWeight={700} color="primary.main" mt={1.5}>
      {formatToman(tutor.hourlyRate)} Toman/hr
    </Typography>
  );
}

/**
 * Every field here comes straight off `TutorDto` — no rating, review count,
 * or online indicator is shown because none exists in the API (see
 * `TrustIndicators`' own doc comment). Phase 9 wires in the `tutorSubjects`/
 * `otherLanguages`/`trialLessonAvailable` fields ADR-024 added, which
 * existed on the DTO already but weren't read here yet. "Starting from
 * [lowest plan price]" is still not shown — no Learning Plan (and therefore
 * no plan price) exists anywhere in this API yet (`docs/adr/ADR-021...`,
 * Proposed, not Accepted) — a `PlanBadge` honestly signals "coming soon"
 * instead. The primary Subject itself is deliberately not repeated as a
 * chip here (Phase 3 declutter) — additional subjects beyond the primary
 * one are shown, since those are new information, not a repeat.
 */
function TutorCardMeta({ tutor }: { tutor: TutorDto }) {
  const additionalSubjects = (tutor.tutorSubjects ?? [])
    .filter((entry) => entry.subject !== tutor.subject)
    .slice(0, 2);
  const otherLanguages = (tutor.otherLanguages ?? []).slice(0, 2);

  return (
    <Stack spacing={1} mt={1}>
      {tutor.language || additionalSubjects.length > 0 ? (
        <Stack direction="row" flexWrap="wrap" gap={0.75}>
          {tutor.language ? <Chip label={tutor.language} size="small" variant="outlined" /> : null}
          {additionalSubjects.map((entry) => (
            <Chip
              key={entry.subject}
              label={entry.level ? `${entry.subject} (${entry.level})` : entry.subject}
              size="small"
              variant="outlined"
            />
          ))}
        </Stack>
      ) : null}
      {otherLanguages.length > 0 ? (
        <Stack direction="row" flexWrap="wrap" gap={0.75}>
          {otherLanguages.map((lang) => (
            <Chip key={lang} icon={<TranslateRoundedIcon fontSize="small" />} label={lang} size="small" variant="outlined" />
          ))}
        </Stack>
      ) : null}
      {tutor.offeredDurations.length > 0 ? (
        <Typography variant="body2" color="text.secondary">
          Sessions: {formatMinutesList(tutor.offeredDurations)} min
        </Typography>
      ) : null}
      {tutor.trialLessonAvailable ? (
        <Box>
          <StatusPill label="Trial lesson available" tone="success" />
        </Box>
      ) : null}
      <Box>
        <PlanBadge label="Learning Plans coming soon" tone="neutral" />
      </Box>
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
      <MuiLink
        component={RouterLink}
        to={`${paths.identity.tutorDetail(tutorId)}#learning-plans`}
        variant="body2"
        underline="hover"
        textAlign="center"
      >
        View Learning Plans
      </MuiLink>
    </Stack>
  );
}

export function TutorCard({ tutor }: { tutor: TutorDto }) {
  return (
    <Card variant="outlined" sx={TUTOR_CARD_SX}>
      <CardContent sx={{ display: "flex", flexDirection: "column", height: "100%" }}>
        <TutorCardHeader tutor={tutor} />
        <Box mt={1}>
          <TrustIndicators tutor={tutor} showLanguage={false} />
        </Box>
        <TutorCardPrice tutor={tutor} />
        <TutorCardMeta tutor={tutor} />
        <Box flexGrow={1} minHeight={16} />
        <TutorCardActions tutorId={tutor.tutorId} />
      </CardContent>
    </Card>
  );
}
