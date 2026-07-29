import { useEffect, type MouseEvent, type ReactNode } from "react";
import { Link as RouterLink, useParams } from "react-router-dom";
import { Box, Button, Chip, Link as MuiLink, Stack, Typography, alpha } from "@mui/material";
import ReviewsRoundedIcon from "@mui/icons-material/ReviewsRounded";
import HelpOutlineRoundedIcon from "@mui/icons-material/HelpOutlineRounded";
import WorkspacePremiumRoundedIcon from "@mui/icons-material/WorkspacePremiumRounded";
import PsychologyRoundedIcon from "@mui/icons-material/PsychologyRounded";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import { useTutor } from "@/features/identity/hooks/useTutorQueries";
import { useRecentlyViewedTutors } from "@/features/discovery/hooks/useRecentlyViewedTutors";
import { useTutorAvailabilitySlots } from "@/features/scheduling/hooks/useAvailabilitySlotQueries";
import { useNextAvailableLabel } from "@/features/scheduling/hooks/useNextAvailableLabel";
import { AvailabilitySummaryCardSkeleton } from "@/features/scheduling/components/AvailabilitySummaryCardSkeleton";
import { TutorApprovalActions } from "@/features/identity/components/TutorApprovalActions";
import { TutorProfileHero } from "@/features/identity/components/TutorProfileHero";
import { TutorProfileSkeleton } from "@/features/identity/components/TutorProfileSkeleton";
import { ProfileCompletionCard } from "@/features/identity/components/ProfileCompletionCard";
import { ProfilePlaceholderSection } from "@/features/identity/components/ProfilePlaceholderSection";
import { AvailabilityPreviewCalendar } from "@/features/identity/components/AvailabilityPreviewCalendar";
import { MobileBookingBar, MOBILE_BOOKING_BAR_HEIGHT } from "@/features/identity/components/MobileBookingBar";
import { RelatedTutorsSection } from "@/features/identity/components/RelatedTutorsSection";
import { deriveProfileCompletion } from "@/features/identity/utils/profileCompletion";
import { SectionCard } from "@/shared/components/SectionCard";
import { LearningPlanCard } from "@/features/learningPlans/components/LearningPlanCard";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { formatMinutesList } from "@/shared/utils/duration";
import { formatToman } from "@/shared/money/rial";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { UnavailableState } from "@/shared/components/feedback/UnavailableState";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { CopyableId } from "@/shared/components/CopyableId";
import { useEffectiveRole } from "@/shared/hooks/useEffectiveRole";
import { paths } from "@/routes/paths";
import type { TutorDto } from "@/services/api/dtos";
import type { LearningPlanPreview } from "@/features/learningPlans/types";

const PROFILE_SECTIONS = [
  { id: "about", label: "About" },
  { id: "learning-plans", label: "Learning Plans" },
  { id: "availability", label: "Availability" },
  { id: "reviews", label: "Reviews" },
  { id: "faq", label: "FAQ" },
  { id: "similar-tutors", label: "Similar tutors" },
] as const;

/**
 * In-page anchor jumps, not routes — every section already renders on this
 * one page (there is no per-section data to lazily load), so this is only
 * a faster way to reach a section that's already there, same spirit as
 * `Breadcrumbs`' "don't invent a second navigation model." `top: 88`
 * (Phase 4 fix — was `top: 0`, which sat directly behind the fixed
 * `AppHeader` instead of flush beneath it) matches the same offset this
 * page's own sticky booking rail and Phase 3's sticky search bar both use.
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
        top: 88,
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
        to={paths.identity.tutorOnboarding(tutor.tutorId)}
        variant="contained"
        size="small"
        sx={{ alignSelf: "flex-start" }}
      >
        Edit profile
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

/**
 * The Tutor's own "how complete is my listing" checklist — same
 * `deriveProfileCompletion`/`ProfileCompletionCard` the Dashboard shows,
 * reused here rather than duplicated. Only the Tutor viewing their own
 * profile sees it: it's a self-service checklist, not an Admin or public
 * concern (unlike "Manage this listing", which both Tutor and Admin need).
 */
function OwnProfileCompletionSection({ tutor }: { tutor: TutorDto }) {
  const role = useEffectiveRole();
  const slotsQuery = useTutorAvailabilitySlots(tutor.tutorId);

  if (role !== "Tutor" || !slotsQuery.isSuccess) {
    return null;
  }

  return (
    <ProfileCompletionCard
      completion={deriveProfileCompletion(tutor, slotsQuery.data.length > 0)}
      tutorId={tutor.tutorId}
    />
  );
}

/**
 * "Why learn with this tutor" (Phase 4 PART 1/3 — renamed and merged from
 * the previous separate "Subjects & Languages" + "Teaching Information"
 * sections): every real, decision-relevant fact about what this Tutor
 * teaches and how, as a small grid of highlight boxes rather than a plain
 * caption+chip stack or a database-style field list. The hourly rate is
 * deliberately NOT repeated here even though it's real (already prominent
 * in the Hero, Phase 4 PART 2) — showing it a second time would be noise,
 * not new information, the same declutter reasoning `TutorCard`'s meta
 * block already applies to Subject vs. its own heading.
 *
 * Phase 9: `biography` and `otherLanguages` were added to `TutorDto` by
 * ADR-024 but weren't read here yet — a bio paragraph now renders above the
 * highlights grid when present (the closest thing this profile has to a
 * real "About" write-up), and other spoken languages join the existing
 * Subjects/Session-lengths highlights rather than needing their own
 * section (this section's whole point is merging related facts, not
 * flat-listing every field). The primary spoken language is still not
 * repeated here — it already has its own badge in the Hero.
 */
function WhyLearnSection({ tutor }: { tutor: TutorDto }) {
  const hasSessionLengths = tutor.offeredDurations.length > 0;
  const otherLanguages = tutor.otherLanguages ?? [];

  if (!tutor.subject && !hasSessionLengths && !tutor.biography && otherLanguages.length === 0) {
    return null;
  }

  const highlights: { key: string; label: string; content: ReactNode }[] = [];

  if (tutor.subject) {
    highlights.push({ key: "subject", label: "Subjects", content: <Chip label={tutor.subject} size="small" /> });
  }
  if (otherLanguages.length > 0) {
    highlights.push({
      key: "otherLanguages",
      label: "Also speaks",
      content: (
        <Stack direction="row" flexWrap="wrap" gap={0.5}>
          {otherLanguages.map((lang) => (
            <Chip key={lang} label={lang} size="small" />
          ))}
        </Stack>
      ),
    });
  }
  if (hasSessionLengths) {
    highlights.push({
      key: "durations",
      label: "Session lengths",
      content: (
        <Typography variant="body1" fontWeight={600}>
          {formatMinutesList(tutor.offeredDurations)} minutes
        </Typography>
      ),
    });
  }

  return (
    <SectionCard id="about" headingComponent="h2" title="Why learn with this tutor">
      <Stack spacing={2}>
        {tutor.biography ? (
          <Typography variant="body1" color="text.secondary" sx={{ whiteSpace: "pre-line" }}>
            {tutor.biography}
          </Typography>
        ) : null}
        {highlights.length > 0 ? (
          <Stack direction="row" flexWrap="wrap" gap={2}>
            {highlights.map((highlight) => (
              <Box
                key={highlight.key}
                sx={{
                  flex: "1 1 200px",
                  p: 2,
                  borderRadius: 3,
                  bgcolor: (t) => alpha(t.palette.primary.main, t.palette.mode === "dark" ? 0.14 : 0.06),
                }}
              >
                <Typography variant="caption" color="text.secondary">
                  {highlight.label}
                </Typography>
                <Box mt={1}>{highlight.content}</Box>
              </Box>
            ))}
          </Stack>
        ) : null}
      </Stack>
    </SectionCard>
  );
}

/**
 * Phase 9: `education`/`certifications`/`yearsOfExperience` exist on
 * `TutorDto` (ADR-024) but this section previously always rendered
 * `ProfilePlaceholderSection`'s generic "no certificates or experience
 * added yet" regardless of whether that data existed — real content is now
 * shown when any of the three is present, falling back to the exact same
 * honest placeholder only when a Tutor genuinely hasn't filled any of them
 * in, preserving `ProfilePlaceholderSection`'s own "never a fabricated
 * value" contract.
 */
function ExperienceSection({ tutor }: { tutor: TutorDto }) {
  if (!tutor.education && !tutor.certifications && tutor.yearsOfExperience == null) {
    return (
      <ProfilePlaceholderSection
        id="certificates"
        title="Certificates & Experience"
        icon={<WorkspacePremiumRoundedIcon fontSize="large" aria-hidden="true" />}
        heading="No certificates or experience added yet"
        description="This tutor hasn't added any certificates or teaching experience to their profile yet."
      />
    );
  }

  return (
    <SectionCard id="certificates" headingComponent="h2" title="Certificates & Experience">
      <Stack spacing={2}>
        {tutor.yearsOfExperience != null ? (
          <Typography variant="body1" fontWeight={600}>
            {tutor.yearsOfExperience} {tutor.yearsOfExperience === 1 ? "year" : "years"} of teaching experience
          </Typography>
        ) : null}
        {tutor.education ? (
          <Box>
            <Typography variant="caption" color="text.secondary">
              Education
            </Typography>
            <Typography variant="body1" sx={{ whiteSpace: "pre-line" }}>
              {tutor.education}
            </Typography>
          </Box>
        ) : null}
        {tutor.certifications ? (
          <Box>
            <Typography variant="caption" color="text.secondary">
              Certifications
            </Typography>
            <Typography variant="body1" sx={{ whiteSpace: "pre-line" }}>
              {tutor.certifications}
            </Typography>
          </Box>
        ) : null}
      </Stack>
    </SectionCard>
  );
}

/**
 * Phase 9: same pattern as `ExperienceSection` — `teachingMethodology`/
 * `lessonSpecialties` exist on `TutorDto` (ADR-024) but weren't read here
 * yet; falls back to the existing honest placeholder when both are absent.
 */
function TeachingStyleSection({ tutor }: { tutor: TutorDto }) {
  const specialties = tutor.lessonSpecialties ?? [];

  if (!tutor.teachingMethodology && specialties.length === 0) {
    return (
      <ProfilePlaceholderSection
        id="teaching-style"
        title="Teaching Style"
        icon={<PsychologyRoundedIcon fontSize="large" aria-hidden="true" />}
        heading="No teaching style details added yet"
        description="This tutor hasn't described their teaching style or methods yet."
      />
    );
  }

  return (
    <SectionCard id="teaching-style" headingComponent="h2" title="Teaching Style">
      <Stack spacing={2}>
        {tutor.teachingMethodology ? (
          <Typography variant="body1" color="text.secondary" sx={{ whiteSpace: "pre-line" }}>
            {tutor.teachingMethodology}
          </Typography>
        ) : null}
        {specialties.length > 0 ? (
          <Stack direction="row" flexWrap="wrap" gap={1}>
            {specialties.map((specialty) => (
              <Chip key={specialty} label={specialty} size="small" />
            ))}
          </Stack>
        ) : null}
      </Stack>
    </SectionCard>
  );
}

/**
 * RC5.0: no Learning Plan capability exists in this API version yet
 * (`docs/adr/ADR-021...`, Proposed, not Accepted) — an honest empty state,
 * never a fabricated plan. `plans` is always empty today; the mapped branch
 * exists so this section lights up with real `LearningPlanCard`s the
 * moment a real endpoint exists, with no structural change needed here.
 */
function LearningPlansSection({ tutor }: { tutor: TutorDto }) {
  const plans: LearningPlanPreview[] = [];

  return (
    <SectionCard id="learning-plans" headingComponent="h2" title="Learning Plans">
      {plans.length === 0 ? (
        <EmptyState
          title="No learning plans yet"
          description={`${tutor.subject ?? "This tutor"} hasn't published a structured Learning Plan yet — book a single lesson to get started in the meantime.`}
          action={
            <Button
              component={RouterLink}
              to={`${paths.scheduling.bookSession}?tutorId=${tutor.tutorId}`}
              variant="outlined"
              startIcon={<EventRoundedIcon />}
            >
              Book a single lesson instead
            </Button>
          }
        />
      ) : (
        <Stack direction="row" flexWrap="wrap" gap={2}>
          {plans.map((plan) => (
            <LearningPlanCard key={plan.learningPlanId} plan={plan} />
          ))}
        </Stack>
      )}
    </SectionCard>
  );
}

/**
 * Reuses `useTutorAvailabilitySlots` — the same capability the Booking
 * wizard's Choose Date/Choose Time steps and the Tutor Dashboard's
 * Availability Summary already use. Phase 9: the previous flat row of
 * up-to-3 slot cards (each clickable straight into the booking wizard) is
 * replaced by a read-only `AvailabilityPreviewCalendar` — a month view is a
 * more honest "preview," and per this phase's own constraint, no booking
 * logic (slot selection, wizard deep links) lives inside the profile page
 * anymore; the existing "View full schedule" link (unchanged) is the one
 * path from here into booking.
 */
function AvailabilityPreviewSection({ tutor }: { tutor: TutorDto }) {
  const slotsQuery = useTutorAvailabilitySlots(tutor.tutorId);

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

  return (
    <SectionCard id="availability" headingComponent="h2" title="Availability">
      <Typography variant="body2" color="text.secondary" mb={2}>
        A preview of this tutor&rsquo;s open teaching times (Tehran time) — book a lesson to choose an exact time.
      </Typography>
      <AvailabilityPreviewCalendar slots={slotsQuery.data} />
      <Button
        component={RouterLink}
        to={`${paths.scheduling.bookSession}?tutorId=${tutor.tutorId}`}
        variant="text"
        startIcon={<EventRoundedIcon />}
        sx={{ mt: 2 }}
      >
        View full schedule
      </Button>
    </SectionCard>
  );
}

/**
 * Phase 9: enriched from a bare "Book a lesson directly with this tutor"
 * card into a real sticky booking summary — price, trial-lesson info, and
 * next-available all reuse data/formatting already established elsewhere
 * on this exact page (`formatToman` in the Hero, `useNextAvailableLabel`
 * now shared with it) rather than duplicating that logic here. Still just
 * one "Book Lesson" button underneath — no new interaction.
 */
function BookingCallToActionSection({ tutor }: { tutor: TutorDto }) {
  const nextAvailableLabel = useNextAvailableLabel(tutor.tutorId);

  return (
    <SectionCard headingComponent="h2" title="Ready to get started?">
      <Stack spacing={2} alignItems="flex-start">
        <Typography
          variant="h5"
          component="p"
          fontWeight={700}
          color={tutor.hourlyRate !== null ? "primary.main" : "text.secondary"}
        >
          {tutor.hourlyRate !== null ? `${formatToman(tutor.hourlyRate)} Toman/hr` : "Rate not set"}
        </Typography>
        {tutor.trialLessonAvailable ? (
          <StatusPill
            label={
              tutor.trialLessonPrice != null
                ? `Trial lesson — ${formatToman(tutor.trialLessonPrice)} Toman`
                : "Trial lesson available"
            }
            tone="success"
          />
        ) : null}
        {nextAvailableLabel ? (
          <Typography variant="body2" color="text.secondary">
            Next available {nextAvailableLabel}
          </Typography>
        ) : null}
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

/**
 * A friendly placeholder for "the id in the URL doesn't resolve" — whether
 * that's a genuine 404, a network failure, or anything else the backend
 * reports, the actionable message for a Student browsing tutors is the
 * same either way: try again, or go find another tutor. `onRetry` covers
 * the transient (network) case; "Find Tutors" covers "this tutor really
 * doesn't exist" without ever surfacing the backend's own wording.
 */
function TutorNotFound({ onRetry }: { onRetry: () => void }) {
  return (
    <UnavailableState
      title="Tutor unavailable"
      description="This tutor is no longer available. This profile may have been removed, or the link might be broken."
      actions={[
        { label: "Try again", onClick: onRetry },
        {
          label: "Find another tutor",
          to: paths.discovery.tutorSearch,
          variant: "contained",
          icon: <SearchRoundedIcon />,
        },
      ]}
    />
  );
}

/**
 * GET /tutors/{id} (`useTutor`) — the same query hook and route as before.
 * Phase 4 rebuilt the page around a decision narrative — Hero → About →
 * Certificates & Experience → Teaching Style → Availability → Learning
 * Plans → Reviews → FAQ → Related tutors (only rendered when a real,
 * already-supported "same subject" search actually returns another Tutor)
 * — rather than a flat list of database-shaped sections. Phase 9 wired
 * `ExperienceSection`/`TeachingStyleSection`/`WhyLearnSection` to the real
 * `education`/`certifications`/`yearsOfExperience`/`teachingMethodology`/
 * `lessonSpecialties`/`biography`/`otherLanguages` fields ADR-024 already
 * added to `TutorDto` but this page hadn't read yet — each still falls
 * back to its original honest placeholder when a Tutor hasn't filled that
 * section in. Reviews and FAQ remain placeholders unchanged: no review or
 * FAQ endpoint is queried, because there is no such capability in this API
 * version at all — not silently invented to match the section list.
 */
export function TutorDetailPage() {
  const { tutorId } = useParams<{ tutorId: string }>();
  const tutorQuery = useTutor(tutorId);
  const { recordView } = useRecentlyViewedTutors();

  // Records a view only once the Tutor is confirmed to actually exist —
  // never for an id that 404s, so "Recently Viewed" can't fill up with
  // dead links.
  useEffect(() => {
    if (tutorQuery.isSuccess) {
      recordView(tutorQuery.data.tutorId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tutorQuery.isSuccess, tutorQuery.data?.tutorId]);

  if (tutorQuery.isPending) {
    return <TutorProfileSkeleton />;
  }

  if (tutorQuery.isError) {
    return <TutorNotFound onRetry={() => void tutorQuery.refetch()} />;
  }

  const tutor = tutorQuery.data;

  return (
    <Stack spacing={3}>
      <TutorProfileHero tutor={tutor} />
      <TutorProfileSectionNav />

      <Stack direction={{ xs: "column", md: "row" }} spacing={3} alignItems="flex-start">
        <Stack flex={2} spacing={3} width="100%">
          <WhyLearnSection tutor={tutor} />
          <ExperienceSection tutor={tutor} />
          <TeachingStyleSection tutor={tutor} />
          <AvailabilityPreviewSection tutor={tutor} />
          <LearningPlansSection tutor={tutor} />
          <ProfilePlaceholderSection
            id="reviews"
            title="Reviews"
            icon={<ReviewsRoundedIcon fontSize="large" aria-hidden="true" />}
            heading="No reviews yet"
            description="This tutor hasn't received any reviews yet. Book a lesson to be one of the first to share your experience."
          />
          <ProfilePlaceholderSection
            id="faq"
            title="FAQ"
            icon={<HelpOutlineRoundedIcon fontSize="large" aria-hidden="true" />}
            heading="No frequently asked questions yet"
            description="Common questions about this tutor's teaching style and Learning Plans will appear here soon."
          />
          <Box id="similar-tutors">
            <RelatedTutorsSection tutor={tutor} />
          </Box>
        </Stack>
        <Stack
          flex={1}
          spacing={3}
          width="100%"
          sx={{ position: { md: "sticky" }, top: { md: 88 } }}
        >
          <ManageListingSection tutor={tutor} />
          <OwnProfileCompletionSection tutor={tutor} />
          <BookingCallToActionSection tutor={tutor} />
        </Stack>
      </Stack>

      {/* Reserves space so the fixed mobile booking bar never covers this page's last section. */}
      <Box sx={{ display: { xs: "block", md: "none" }, height: MOBILE_BOOKING_BAR_HEIGHT }} aria-hidden="true" />
      <MobileBookingBar tutor={tutor} />
    </Stack>
  );
}
