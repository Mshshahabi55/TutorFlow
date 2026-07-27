import { Link as RouterLink, useParams } from "react-router-dom";
import { Box, Button, Chip, Stack, Typography } from "@mui/material";
import ReviewsRoundedIcon from "@mui/icons-material/ReviewsRounded";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import ArrowBackRoundedIcon from "@mui/icons-material/ArrowBackRounded";
import { useTutor } from "@/features/identity/hooks/useTutorQueries";
import { TutorApprovalActions } from "@/features/identity/components/TutorApprovalActions";
import { TutorProfileHero } from "@/features/identity/components/TutorProfileHero";
import { TutorProfileSkeleton } from "@/features/identity/components/TutorProfileSkeleton";
import { ProfileSectionCard } from "@/features/identity/components/ProfileSectionCard";
import { formatMinutesList } from "@/shared/utils/duration";
import { formatToman } from "@/shared/money/rial";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { CopyableId } from "@/shared/components/CopyableId";
import { useEffectiveRole } from "@/shared/hooks/useEffectiveRole";
import { paths } from "@/routes/paths";
import type { TutorDto } from "@/services/api/dtos";

/**
 * Phase 4.9 Task 4: Edit offering (Tutor-only, `ManageTutorOffering`) and
 * Approve/Suspend (Admin-only, `ApproveTutor`/`SuspendTutor`) previously
 * rendered unconditionally for every viewer. Gated on the same
 * `useEffectiveRole` signal NavSidebar and the route guard already use, not
 * a new one. A Student/Parent-Guardian viewer gets no action here —
 * booking itself is a separate, later phase this one is a prerequisite
 * for, not yet wired to this page.
 */
function TutorDetailActions({ tutor }: { tutor: TutorDto }) {
  const role = useEffectiveRole();

  if (role === "AdminStaff") {
    return <TutorApprovalActions tutor={tutor} />;
  }

  if (role === "Tutor") {
    return (
      <Button
        component={RouterLink}
        to={paths.identity.tutorEdit(tutor.tutorId)}
        variant="contained"
        size="small"
        sx={{ alignSelf: "flex-start" }}
      >
        Edit offering
      </Button>
    );
  }

  return null;
}

/**
 * Phase 3 Step 3: the raw business/moderation state (Approved/Suspended/
 * Discoverable) is operationally useful to Admin staff and to a Tutor
 * viewing their own listing, but has no consumer value to a Student or
 * Parent/Guardian deciding whether to book — so it renders only for those
 * two viewers, inside its own "Manage this listing" section, never in the
 * public-facing hero (which shows only the friendly "Verified" badge,
 * derived from the same `isApproved` field, to every viewer).
 */
function ManageListingSection({ tutor }: { tutor: TutorDto }) {
  const role = useEffectiveRole();

  if (role !== "AdminStaff" && role !== "Tutor") {
    return null;
  }

  return (
    <ProfileSectionCard title="Manage this listing">
      <Stack spacing={2}>
        <CopyableId id={tutor.tutorId} />
        <Stack direction="row" spacing={1} flexWrap="wrap">
          <StatusPill
            label={tutor.isApproved ? "Approved" : "Pending approval"}
            tone={tutor.isApproved ? "success" : "warning"}
          />
          <StatusPill
            label={tutor.isSuspended ? "Suspended" : "Not suspended"}
            tone={tutor.isSuspended ? "critical" : "neutral"}
          />
          <StatusPill
            label={tutor.isDiscoverable ? "Discoverable" : "Not discoverable"}
            tone={tutor.isDiscoverable ? "info" : "neutral"}
          />
        </Stack>
        <TutorDetailActions tutor={tutor} />
      </Stack>
    </ProfileSectionCard>
  );
}

function SubjectsAndLanguagesSection({ tutor }: { tutor: TutorDto }) {
  if (!tutor.subject && !tutor.language) {
    return null;
  }

  return (
    <ProfileSectionCard title="Subjects & Languages">
      <Stack spacing={2}>
        {tutor.subject ? (
          <Box>
            <Typography variant="caption" color="text.secondary">
              Subjects
            </Typography>
            <Box mt={0.5}>
              <Chip label={tutor.subject} size="small" />
            </Box>
          </Box>
        ) : null}
        {tutor.language ? (
          <Box>
            <Typography variant="caption" color="text.secondary">
              Languages
            </Typography>
            <Box mt={0.5}>
              <Chip label={tutor.language} size="small" variant="outlined" />
            </Box>
          </Box>
        ) : null}
      </Stack>
    </ProfileSectionCard>
  );
}

function TeachingInformationSection({ tutor }: { tutor: TutorDto }) {
  return (
    <ProfileSectionCard title="Teaching Information">
      <Stack spacing={2}>
        <Box>
          <Typography variant="caption" color="text.secondary">
            Hourly rate
          </Typography>
          <Typography variant="body1" fontWeight={600}>
            {tutor.hourlyRate !== null ? `${formatToman(tutor.hourlyRate)} Toman/hr` : "Not set"}
          </Typography>
        </Box>
        <Box>
          <Typography variant="caption" color="text.secondary">
            Session lengths
          </Typography>
          <Typography variant="body1">
            {tutor.offeredDurations.length > 0
              ? `${formatMinutesList(tutor.offeredDurations)} minutes`
              : "Not set"}
          </Typography>
        </Box>
      </Stack>
    </ProfileSectionCard>
  );
}

/** No review capability exists in this API version — a professional placeholder, never a fabricated review. */
function ReviewsSection() {
  return (
    <ProfileSectionCard title="Reviews">
      <Stack spacing={1} alignItems="flex-start">
        <ReviewsRoundedIcon color="disabled" fontSize="large" aria-hidden="true" />
        <Typography variant="body1" fontWeight={600}>
          No reviews yet
        </Typography>
        <Typography variant="body2" color="text.secondary">
          This tutor hasn&rsquo;t received any reviews yet. Book a session to be one of the first to
          share your experience.
        </Typography>
      </Stack>
    </ProfileSectionCard>
  );
}

function BookingCallToActionSection({ tutor }: { tutor: TutorDto }) {
  return (
    <ProfileSectionCard title="Ready to get started?">
      <Stack spacing={2} alignItems="flex-start">
        <Typography variant="body2" color="text.secondary">
          Book a session directly with this tutor.
        </Typography>
        <Button
          component={RouterLink}
          to={`${paths.scheduling.bookSession}?tutorId=${tutor.tutorId}`}
          variant="contained"
          startIcon={<EventRoundedIcon />}
          fullWidth
        >
          Book Session
        </Button>
      </Stack>
    </ProfileSectionCard>
  );
}

function TutorNotFound({ error, onRetry }: { error: unknown; onRetry: () => void }) {
  return (
    <Stack spacing={3} alignItems="center" textAlign="center" py={6}>
      <Typography variant="h4" component="h1">
        Tutor not found
      </Typography>
      <Box maxWidth={480} width="100%">
        <ErrorState error={error} onRetry={onRetry} title="This profile could not be loaded" />
      </Box>
      <Button
        component={RouterLink}
        to={paths.discovery.tutorSearch}
        variant="outlined"
        startIcon={<ArrowBackRoundedIcon />}
      >
        Back to search
      </Button>
    </Stack>
  );
}

/**
 * GET /tutors/{id} (`useTutor`) — the same query hook and route as before
 * (Phase 3 Step 3 is presentation-only). No booking, availability, or
 * review endpoint is queried here: there is no per-Tutor availability or
 * review capability in this API version, so those sections are omitted
 * rather than fabricated (Statistics is omitted for the same reason — no
 * such field exists on `TutorDto`).
 */
export function TutorDetailPage() {
  const { tutorId } = useParams<{ tutorId: string }>();
  const tutorQuery = useTutor(tutorId);

  if (tutorQuery.isPending) {
    return <TutorProfileSkeleton />;
  }

  if (tutorQuery.isError) {
    return <TutorNotFound error={tutorQuery.error} onRetry={() => void tutorQuery.refetch()} />;
  }

  const tutor = tutorQuery.data;

  return (
    <Stack spacing={3}>
      <TutorProfileHero tutor={tutor} />

      <Stack direction={{ xs: "column", md: "row" }} spacing={3} alignItems="flex-start">
        <Stack flex={2} spacing={3} width="100%">
          <SubjectsAndLanguagesSection tutor={tutor} />
          <ReviewsSection />
        </Stack>
        <Stack flex={1} spacing={3} width="100%">
          <TeachingInformationSection tutor={tutor} />
          <ManageListingSection tutor={tutor} />
          <BookingCallToActionSection tutor={tutor} />
        </Stack>
      </Stack>
    </Stack>
  );
}
