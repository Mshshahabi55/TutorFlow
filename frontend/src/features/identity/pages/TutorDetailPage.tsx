import type { MouseEvent } from "react";
import { Link as RouterLink, useNavigate, useParams } from "react-router-dom";
import { Box, Button, Chip, Link as MuiLink, Stack, Typography } from "@mui/material";
import ReviewsRoundedIcon from "@mui/icons-material/ReviewsRounded";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import ArrowBackRoundedIcon from "@mui/icons-material/ArrowBackRounded";
import { useTutor } from "@/features/identity/hooks/useTutorQueries";
import { useTutorAvailabilitySlots } from "@/features/scheduling/hooks/useAvailabilitySlotQueries";
import { AvailabilityCard } from "@/features/scheduling/components/AvailabilityCard";
import { AvailabilitySummaryCardSkeleton } from "@/features/scheduling/components/AvailabilitySummaryCardSkeleton";
import { TutorApprovalActions } from "@/features/identity/components/TutorApprovalActions";
import { TutorProfileHero } from "@/features/identity/components/TutorProfileHero";
import { TutorProfileSkeleton } from "@/features/identity/components/TutorProfileSkeleton";
import { SectionCard } from "@/shared/components/SectionCard";
import { formatMinutesList } from "@/shared/utils/duration";
import { formatToman } from "@/shared/money/rial";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { CopyableId } from "@/shared/components/CopyableId";
import { useEffectiveRole } from "@/shared/hooks/useEffectiveRole";
import { paths } from "@/routes/paths";
import type { AvailabilitySlotDto, TutorDto } from "@/services/api/dtos";

const AVAILABILITY_PREVIEW_LIMIT = 3;

const PROFILE_SECTIONS = [
  { id: "subjects", label: "Subjects" },
  { id: "teaching-information", label: "Teaching Info" },
  { id: "availability", label: "Availability" },
  { id: "reviews", label: "Reviews" },
] as const;

/**
 * In-page anchor jumps, not routes — every section already renders on this
 * one page (there is no per-section data to lazily load), so this is only
 * a faster way to reach a section that's already there, same spirit as
 * `Breadcrumbs`' "don't invent a second navigation model."
 */
function TutorProfileSectionNav() {
  function handleJump(event: MouseEvent<HTMLAnchorElement>, id: string) {
    event.preventDefault();
    document.getElementById(id)?.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  return (
    <Stack
      direction="row"
      spacing={3}
      flexWrap="wrap"
      sx={{
        position: "sticky",
        top: 0,
        zIndex: 1,
        bgcolor: "background.default",
        py: 1.5,
        borderBottom: "1px solid",
        borderColor: "divider",
      }}
    >
      {PROFILE_SECTIONS.map((section) => (
        <MuiLink
          key={section.id}
          href={`#${section.id}`}
          onClick={(event) => handleJump(event, section.id)}
          underline="hover"
          color="text.secondary"
          fontWeight={600}
          variant="body2"
        >
          {section.label}
        </MuiLink>
      ))}
    </Stack>
  );
}

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
    <SectionCard headingComponent="h2" title="Manage this listing">
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
    </SectionCard>
  );
}

function SubjectsAndLanguagesSection({ tutor }: { tutor: TutorDto }) {
  if (!tutor.subject && !tutor.language) {
    return null;
  }

  return (
    <SectionCard id="subjects" headingComponent="h2" title="Subjects & Languages">
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
    </SectionCard>
  );
}

function TeachingInformationSection({ tutor }: { tutor: TutorDto }) {
  return (
    <SectionCard id="teaching-information" headingComponent="h2" title="Teaching Information">
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
    </SectionCard>
  );
}

/** No review capability exists in this API version — a professional placeholder, never a fabricated review. */
function ReviewsSection() {
  return (
    <SectionCard id="reviews" headingComponent="h2" title="Reviews">
      <Stack spacing={1} alignItems="flex-start">
        <ReviewsRoundedIcon color="disabled" fontSize="large" aria-hidden="true" />
        <Typography variant="body1" fontWeight={600}>
          No reviews yet
        </Typography>
        <Typography variant="body2" color="text.secondary">
          This tutor hasn&rsquo;t received any reviews yet. Book a lesson to be one of the first to
          share your experience.
        </Typography>
      </Stack>
    </SectionCard>
  );
}

/**
 * Reuses `useTutorAvailabilitySlots` — the same capability the Booking
 * wizard's Choose Date/Choose Time steps and the Tutor Dashboard's
 * Availability Summary already use — for a read-only teaser of the next
 * few open times. Clicking one jumps straight into the booking wizard with
 * that exact slot pre-selected (skipping straight to Review), the same
 * deep link `AvailabilitySlotDetailPage`'s "Book this slot" link already
 * uses.
 */
function AvailabilityPreviewSection({ tutor }: { tutor: TutorDto }) {
  const navigate = useNavigate();
  const slotsQuery = useTutorAvailabilitySlots(tutor.tutorId);

  function openInWizard(slot: AvailabilitySlotDto) {
    void navigate(
      `${paths.scheduling.bookSession}?tutorId=${tutor.tutorId}&availabilitySlotId=${slot.availabilitySlotId}`,
    );
  }

  if (slotsQuery.isPending) {
    return (
      <SectionCard id="availability" headingComponent="h2" title="Availability">
        <Stack direction="row" flexWrap="wrap" gap={2}>
          {Array.from({ length: 2 }, (_, index) => (
            <AvailabilitySummaryCardSkeleton key={index} />
          ))}
        </Stack>
      </SectionCard>
    );
  }

  if (slotsQuery.isError) {
    return (
      <SectionCard id="availability" headingComponent="h2" title="Availability">
        <ErrorState error={slotsQuery.error} onRetry={() => void slotsQuery.refetch()} />
      </SectionCard>
    );
  }

  const openSlots = slotsQuery.data
    .filter((slot) => !slot.isConsumed)
    .sort((a, b) => Date.parse(a.startTimeUtc) - Date.parse(b.startTimeUtc))
    .slice(0, AVAILABILITY_PREVIEW_LIMIT);

  return (
    <SectionCard id="availability" headingComponent="h2" title="Availability">
      {openSlots.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          No open times right now — check back later.
        </Typography>
      ) : (
        <Stack direction="row" flexWrap="wrap" gap={2}>
          {openSlots.map((slot) => (
            <AvailabilityCard
              key={slot.availabilitySlotId}
              slot={slot}
              selected={false}
              onSelect={openInWizard}
            />
          ))}
        </Stack>
      )}
    </SectionCard>
  );
}

function BookingCallToActionSection({ tutor }: { tutor: TutorDto }) {
  return (
    <SectionCard headingComponent="h2" title="Ready to get started?">
      <Stack spacing={2} alignItems="flex-start">
        <Typography variant="body2" color="text.secondary">
          Book a lesson directly with this tutor.
        </Typography>
        <Button
          component={RouterLink}
          to={`${paths.scheduling.bookSession}?tutorId=${tutor.tutorId}`}
          variant="contained"
          startIcon={<EventRoundedIcon />}
          fullWidth
        >
          Book Lesson
        </Button>
      </Stack>
    </SectionCard>
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
 * GET /tutors/{id} (`useTutor`) — the same query hook and route as before.
 * RC2 adds an in-page section nav (jump links, not routes), an Availability
 * preview reusing `useTutorAvailabilitySlots` (the same capability the
 * Booking wizard and Tutor Dashboard already use), and a booking card that
 * stays in view while scrolling on desktop. No review endpoint is queried:
 * there is no review capability in this API version, so that section stays
 * an honest placeholder rather than a fabricated one (Statistics is
 * omitted for the same reason — no such field exists on `TutorDto`).
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
      <TutorProfileSectionNav />

      <Stack direction={{ xs: "column", md: "row" }} spacing={3} alignItems="flex-start">
        <Stack flex={2} spacing={3} width="100%">
          <SubjectsAndLanguagesSection tutor={tutor} />
          <TeachingInformationSection tutor={tutor} />
          <AvailabilityPreviewSection tutor={tutor} />
          <ReviewsSection />
        </Stack>
        <Stack
          flex={1}
          spacing={3}
          width="100%"
          sx={{ position: { md: "sticky" }, top: { md: 88 } }}
        >
          <ManageListingSection tutor={tutor} />
          <BookingCallToActionSection tutor={tutor} />
        </Stack>
      </Stack>
    </Stack>
  );
}
