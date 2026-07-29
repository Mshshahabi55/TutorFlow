import { useCallback, useEffect, useState } from "react";
import { Link as RouterLink, useSearchParams } from "react-router-dom";
import { Box, Button, LinearProgress, Step, StepLabel, Stepper, Stack, Typography } from "@mui/material";
import ArrowBackRoundedIcon from "@mui/icons-material/ArrowBackRounded";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useBookSession } from "@/features/scheduling/hooks/useSessionMutations";
import {
  useAvailabilitySlot,
  useTutorAvailabilitySlots,
} from "@/features/scheduling/hooks/useAvailabilitySlotQueries";
import { useTutor } from "@/features/identity/hooks/useTutorQueries";
import { useRelationshipsForAccount } from "@/features/identity/hooks/useRelationshipQueries";
import {
  bookSessionSchema,
  type BookSessionFormValues,
} from "@/features/scheduling/validation/bookSessionSchema";
import { BookingHeader } from "@/features/scheduling/components/BookingHeader";
import { SectionCard } from "@/shared/components/SectionCard";
import { TutorSummaryCard } from "@/features/scheduling/components/TutorSummaryCard";
import { AvailabilityCard } from "@/features/scheduling/components/AvailabilityCard";
import { BookingSummaryCard } from "@/features/scheduling/components/BookingSummaryCard";
import { BookingStatusBanner } from "@/features/scheduling/components/BookingStatusBanner";
import { BookingPageSkeleton } from "@/features/scheduling/components/BookingPageSkeleton";
import { MonthCalendarGrid } from "@/features/scheduling/components/MonthCalendarGrid";
import { addMonths, monthKeyFromDateKey, todayMonthKey } from "@/features/scheduling/utils/monthCalendar";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { FormSelect } from "@/shared/components/forms/FormSelect";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { UnavailableState } from "@/shared/components/feedback/UnavailableState";
import { useNotification } from "@/shared/hooks/useNotification";
import { useOwnId } from "@/shared/hooks/useOwnId";
import { tehranDateKey, tehranDateLabel } from "@/shared/time/tehranTime";
import { RelationshipStatus } from "@/services/api/dtos";
import type { AvailabilitySlotDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

const STEPS = ["Choose Tutor", "Choose Date", "Choose Time", "Review", "Confirm"] as const;

/**
 * Pins the wizard's primary action(s) to the bottom of the viewport on
 * mobile, where the button would otherwise scroll out of reach below a
 * long Tutor summary/booking summary — desktop keeps the button inline,
 * already visible without scrolling. `env(safe-area-inset-bottom)` clears
 * a device's home-indicator/gesture bar instead of the button sitting
 * flush against it. Phase 5: also used for the Back-only rows (Choose
 * Date/Choose Time) so every step's navigation is sticky on mobile, not
 * only Review/Confirm's.
 */
const mobileStickyActionsSx = {
  position: { xs: "sticky", sm: "static" },
  bottom: 0,
  bgcolor: "background.default",
  mx: { xs: -2, sm: 0 },
  px: { xs: 2, sm: 0 },
  pt: { xs: 1.5, sm: 0 },
  pb: { xs: "calc(12px + env(safe-area-inset-bottom))", sm: 0 },
  borderTop: { xs: "1px solid", sm: "none" },
  borderColor: "divider",
} as const;

/**
 * POST /sessions books a Session against an already-declared Availability
 * Slot — same command, validation, and route as before. A 5-step wizard
 * (Choose Tutor → Choose Date → Choose Time → Review → Confirm) so the
 * whole form is never shown at once, reusing the exact same query
 * parameters, hooks, and mutation:
 *
 * - `tutorId` (from the Tutor Profile's "Book Lesson" CTA) resolves
 *   `useTutor` for Choose Tutor, and `useTutorAvailabilitySlots` (the same
 *   capability `SessionDetailPage`'s reschedule picker already reuses) for
 *   Choose Date/Choose Time.
 * - `availabilitySlotId` (from `AvailabilitySlotDetailPage`'s "Book this
 *   slot" link) resolves `useAvailabilitySlot`, and — when `tutorId` itself
 *   wasn't given — supplies the Tutor id via the slot's own `tutorId` field.
 *
 * Without either parameter there is no tutor to build a wizard around, so
 * the page shows a friendly prompt to go find one instead of a bare
 * id-entry fallback.
 *
 * Phase 5 (Booking Experience): `TutorSummaryCard` is now rendered once,
 * outside the per-step content, and stays visible across every step
 * (PART 5's "one reusable compact summary card, reused across every
 * step") — the Tutor's identity and hourly rate no longer need repeating
 * inside `BookingSummaryCard` at Review/Confirm (PART 2's "avoid showing
 * the same tutor information/prices repeatedly"). The "Choose Tutor" step
 * itself was evaluated for outright removal (PART 1: "can this step be
 * simpler?") — kept as its own step (its label is still tested/expected
 * navigation), but its content was reduced to a single sentence, since the
 * persistent summary card above it already answers "who am I booking."
 */
export function BookSessionPage() {
  const [searchParams] = useSearchParams();
  const bookSession = useBookSession();
  const { notify } = useNotification();

  // Who is booking, resolved from the real signed-in session whenever
  // possible (`useOwnId`) — never asked as a raw id. The route only admits
  // Student or ParentGuardian (`router.tsx`), so at most one of these two
  // resolves from a real session at a time; neither does under the dev-only
  // "Acting as" preview, which falls back to manual entry below.
  const ownStudent = useOwnId("student");
  const ownParentGuardian = useOwnId("parentGuardian");
  const bookingMode: "self" | "parent" | "manual" = ownStudent.isFromSession
    ? "self"
    : ownParentGuardian.isFromSession
      ? "parent"
      : "manual";

  // A Parent/Guardian must choose which confirmed child this lesson is
  // for — never type the child's id. `useRelationshipsForAccount` is the
  // same query `RelationshipsPage`'s "My Children" view already uses.
  const childrenQuery = useRelationshipsForAccount(
    bookingMode === "parent" ? ownParentGuardian.id : undefined,
  );
  const confirmedChildren = (childrenQuery.data ?? []).filter(
    (relationship) => relationship.status === RelationshipStatus.Confirmed,
  );

  const tutorIdParam = searchParams.get("tutorId") ?? undefined;
  const availabilitySlotIdParam = searchParams.get("availabilitySlotId") ?? undefined;

  const slotByIdQuery = useAvailabilitySlot(availabilitySlotIdParam);
  const effectiveTutorId = tutorIdParam ?? slotByIdQuery.data?.tutorId;
  const tutorQuery = useTutor(effectiveTutorId);
  const slotsQuery = useTutorAvailabilitySlots(effectiveTutorId);

  // Arriving with a specific Availability Slot already chosen (e.g. from
  // AvailabilitySlotDetailPage's "Book this slot" link) skips straight to
  // Review — asking the user to re-pick a date and time they already chose
  // would be a regression, not a wizard.
  const [activeStep, setActiveStep] = useState(availabilitySlotIdParam ? 3 : 0);
  const [selectedDateKey, setSelectedDateKey] = useState<string | undefined>(undefined);
  // null until the Student explicitly navigates — until then, the month
  // shown defaults to wherever the Tutor's earliest open slot actually is
  // (computed below, once slot data is available), not always "this
  // calendar month," which could easily have zero availability.
  const [calendarMonthKeyOverride, setCalendarMonthKeyOverride] = useState<string | null>(null);

  // Phase 5 PART 12: moves keyboard/screen-reader focus to the new step's
  // own content region as soon as it mounts — whether that's from
  // advancing/going back (the previous step's region unmounts, the new
  // one mounts) or from the initial loading skeleton finishing (the first
  // step's region mounts for the first time). A callback ref (rather than
  // an object ref + `useEffect([activeStep])`) is what makes the
  // loading-finishes case work correctly: an effect keyed only on
  // `activeStep` fires before the real region exists while the page is
  // still on `BookingPageSkeleton`, so `activeStep` never changes again
  // once it does mount and the effect would never re-fire.
  const focusStepRegion = useCallback((node: HTMLElement | null) => {
    node?.focus();
  }, []);

  const form = useForm<BookSessionFormValues>({
    resolver: zodResolver(bookSessionSchema),
    defaultValues: {
      availabilitySlotId: availabilitySlotIdParam ?? "",
      // "parent" mode always starts blank — the Parent must actively choose
      // which child, never inherit a stale prior choice. "self" mode is
      // always the real session's own id. "manual" mode (dev preview, no
      // real session) prefills from whatever was remembered before, same
      // as prior to RC4.3.
      studentId: bookingMode === "parent" ? "" : (ownStudent.id ?? ""),
      parentGuardianId: bookingMode === "self" ? "" : (ownParentGuardian.id ?? ""),
    },
  });

  // Keeps the auto-resolved id in sync if the session finishes resolving
  // after this form already mounted with empty defaults (e.g. a session
  // restored asynchronously) — `useForm`'s `defaultValues` are captured
  // once at mount and don't otherwise pick up a later-arriving id.
  useEffect(() => {
    if (bookingMode === "self" && ownStudent.id) {
      form.setValue("studentId", ownStudent.id);
    }
    if (bookingMode === "parent" && ownParentGuardian.id) {
      form.setValue("parentGuardianId", ownParentGuardian.id);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [bookingMode, ownStudent.id, ownParentGuardian.id]);

  const selectedSlotId = form.watch("availabilitySlotId");
  const selectedSlot: AvailabilitySlotDto | undefined =
    slotsQuery.data?.find((slot) => slot.availabilitySlotId === selectedSlotId) ??
    (slotByIdQuery.data?.availabilitySlotId === selectedSlotId ? slotByIdQuery.data : undefined);

  const hasContext = Boolean(effectiveTutorId);
  // Only blocks on a skeleton once a Tutor is actually known (`tutorId` was
  // given directly, or the slot lookup already resolved one).
  const isContextLoading = hasContext && (tutorQuery.isPending || slotsQuery.isPending);

  // A deep link that named a specific Availability Slot (e.g.
  // AvailabilitySlotDetailPage's "Book this slot") whose slot no longer
  // resolves — already booked, or the link is stale. If a `tutorId` was
  // also given, there's still a real wizard to fall back into; if not,
  // there's nothing to recover into but a fresh search.
  const slotLookupFailed = Boolean(availabilitySlotIdParam) && slotByIdQuery.isError;

  // Recovers automatically rather than dumping the user on the Review step
  // with no selected time: if the deep-linked slot turned out to be gone
  // and nothing else has since filled in a selection, drop back to Choose
  // a Time (step 2) with the field cleared, so the wizard falls through to
  // its normal "pick an open slot" flow instead of silently submitting a
  // dead id.
  useEffect(() => {
    if (slotLookupFailed && hasContext && activeStep === 3 && !selectedSlot) {
      form.setValue("availabilitySlotId", "");
      setActiveStep(2);
      notify({
        message: "That time is no longer available — choose another.",
        severity: "info",
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [slotLookupFailed, hasContext]);

  function handleSubmit(values: BookSessionFormValues) {
    bookSession.mutate(
      {
        availabilitySlotId: values.availabilitySlotId,
        studentId: values.studentId,
        parentGuardianId: values.parentGuardianId || null,
      },
      {
        onSuccess: () => {
          // No-ops when resolved from a real session (`useOwnId`) — only the
          // dev-only "Acting as" preview's manual entry needs remembering.
          ownStudent.remember(values.studentId);
          if (values.parentGuardianId) {
            ownParentGuardian.remember(values.parentGuardianId);
          }
          notify({ message: "Lesson booked.", severity: "success" });
        },
      },
    );
  }

  function handleSelectSlot(slot: AvailabilitySlotDto) {
    form.setValue("availabilitySlotId", slot.availabilitySlotId, {
      shouldValidate: true,
      shouldDirty: true,
    });
    setActiveStep(3);
  }

  function handleSelectDate(dateKey: string) {
    setSelectedDateKey(dateKey);
    setActiveStep(2);
  }

  async function handleReviewNext() {
    const valid = await form.trigger(["studentId", "parentGuardianId"]);
    if (valid) {
      setActiveStep(4);
    }
  }

  if (isContextLoading) {
    return (
      <Stack spacing={3} maxWidth={720}>
        <BookingHeader hasContext={hasContext} />
        <BookingPageSkeleton />
      </Stack>
    );
  }

  if (bookSession.isSuccess) {
    return (
      <Stack spacing={3} maxWidth={720}>
        <BookingHeader hasContext={hasContext} />
        <BookingStatusBanner status="success" session={bookSession.data} tutorId={effectiveTutorId} />
      </Stack>
    );
  }

  if (hasContext && tutorQuery.isError) {
    return (
      <UnavailableState
        title="Tutor unavailable"
        description="This tutor is no longer available. Try again, or find another tutor with open times now."
        actions={[
          { label: "Try again", onClick: () => void tutorQuery.refetch() },
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

  // A slot-only deep link (no `tutorId` given) whose slot doesn't resolve
  // — there's no Tutor context to fall back into, only a fresh search.
  if (slotLookupFailed && !hasContext) {
    return (
      <UnavailableState
        title="Availability unavailable"
        description="This teaching time is no longer available. It may have already been booked, or the link might be broken."
        actions={[
          {
            label: "Find a tutor",
            to: paths.discovery.tutorSearch,
            variant: "contained",
            icon: <SearchRoundedIcon />,
          },
        ]}
      />
    );
  }

  const openSlots = (slotsQuery.data ?? []).filter((slot) => !slot.isConsumed);
  const noAvailability = hasContext && slotsQuery.isSuccess && openSlots.length === 0 && !selectedSlot;

  if (noAvailability) {
    return (
      <Stack spacing={3} maxWidth={720}>
        <BookingHeader hasContext={hasContext} />
        <SectionCard title="No availability right now">
          <Stack spacing={2} alignItems="flex-start">
            <Typography variant="body1" color="text.secondary">
              This tutor has no open Availability Slots at the moment. Check back later, or find
              another tutor with open times now.
            </Typography>
            <Stack direction="row" spacing={1} flexWrap="wrap">
              <Button component={RouterLink} to={paths.discovery.tutorSearch} variant="contained">
                Back to search
              </Button>
              {effectiveTutorId ? (
                <Button
                  component={RouterLink}
                  to={paths.identity.tutorDetail(effectiveTutorId)}
                  variant="outlined"
                >
                  View tutor profile
                </Button>
              ) : null}
            </Stack>
          </Stack>
        </SectionCard>
      </Stack>
    );
  }

  const openSlotCountByDate = new Map<string, number>();
  for (const slot of openSlots) {
    const key = tehranDateKey(slot.startTimeUtc);
    openSlotCountByDate.set(key, (openSlotCountByDate.get(key) ?? 0) + 1);
  }
  const earliestOpenDateKey = [...openSlotCountByDate.keys()].sort()[0];
  const calendarMonthKey =
    calendarMonthKeyOverride ?? (earliestOpenDateKey ? monthKeyFromDateKey(earliestOpenDateKey) : todayMonthKey());
  const slotsForSelectedDate = selectedDateKey
    ? openSlots.filter((slot) => tehranDateKey(slot.startTimeUtc) === selectedDateKey)
    : [];

  return (
    <Stack spacing={3} maxWidth={720}>
      <BookingHeader hasContext={hasContext} />

      {hasContext ? (
        <>
          <Stepper activeStep={activeStep} alternativeLabel sx={{ display: { xs: "none", sm: "flex" } }}>
            {STEPS.map((label) => (
              <Step key={label}>
                <StepLabel>{label}</StepLabel>
              </Step>
            ))}
          </Stepper>
          {/* Phase 5 PART 3/4/11: a mobile-only progress indicator with real
              visual weight (not just a sentence) — the current step's name
              made prominent, plus a thin bar showing how far through the
              wizard the user already is and how much remains. */}
          <Stack spacing={0.75} sx={{ display: { xs: "flex", sm: "none" } }}>
            <Typography variant="caption" color="text.secondary">
              Step {activeStep + 1} of {STEPS.length}
            </Typography>
            <Typography variant="subtitle1" fontWeight={700}>
              {STEPS[activeStep]}
            </Typography>
            <LinearProgress
              variant="determinate"
              value={((activeStep + 1) / STEPS.length) * 100}
              aria-label={`Step ${activeStep + 1} of ${STEPS.length}: ${STEPS[activeStep]}`}
              sx={{ height: 6, borderRadius: 999 }}
            />
          </Stack>
        </>
      ) : null}

      {!hasContext ? (
        <EmptyState
          title="Choose a tutor to get started"
          description="Browse tutors and pick one to book your first lesson."
          action={
            <Button
              component={RouterLink}
              to={paths.discovery.tutorSearch}
              variant="contained"
              startIcon={<SearchRoundedIcon />}
            >
              Find Tutors
            </Button>
          }
        />
      ) : (
        <>
          {/*
           * Phase 5 PART 5: one reusable, compact Tutor summary — visible
           * across every step (not just "Choose Tutor") so who/how-much is
           * always in view without repeating either fact inside each
           * step's own content (PART 2).
           */}
          {tutorQuery.isSuccess ? <TutorSummaryCard tutor={tutorQuery.data} /> : null}

          <Form form={form} onSubmit={handleSubmit}>
            <Stack spacing={3}>
              {activeStep === 0 ? (
                <Stack ref={focusStepRegion} tabIndex={-1} role="group" aria-label={STEPS[0]} spacing={3} sx={{ outline: "none" }}>
                  <Typography variant="body2" color="text.secondary">
                    Looking for a multi-week plan? Structured Learning Plans are coming soon — for
                    now, continue below to book a single lesson.
                  </Typography>
                  <Stack sx={mobileStickyActionsSx}>
                    <Button
                      type="button"
                      variant="contained"
                      size="large"
                      onClick={() => setActiveStep(1)}
                      sx={{ alignSelf: { xs: "stretch", sm: "flex-start" } }}
                    >
                      Continue
                    </Button>
                  </Stack>
                </Stack>
              ) : null}

              {activeStep === 1 ? (
                <Stack ref={focusStepRegion} tabIndex={-1} role="group" aria-label={STEPS[1]} spacing={3} sx={{ outline: "none" }}>
                  {slotsQuery.isError ? (
                    <ErrorState
                      error={slotsQuery.error}
                      onRetry={() => void slotsQuery.refetch()}
                      title="This tutor's availability could not be loaded"
                    />
                  ) : (
                    <SectionCard title="Choose a date">
                      <Stack spacing={2}>
                        <Typography variant="body2" color="text.secondary">
                          All times are shown in Tehran local time.
                        </Typography>
                        <MonthCalendarGrid
                          monthKey={calendarMonthKey}
                          onNavigateMonth={(direction) => setCalendarMonthKeyOverride(addMonths(calendarMonthKey, direction))}
                          renderDay={(dateKey, meta) => {
                            const count = openSlotCountByDate.get(dateKey) ?? 0;
                            const isSelectable = count > 0 && !meta.isPast;
                            const isSelected = dateKey === selectedDateKey;
                            const dayNumber = Number(dateKey.slice(8, 10));

                            return (
                              <Box
                                component="button"
                                type="button"
                                disabled={!isSelectable}
                                onClick={() => handleSelectDate(dateKey)}
                                aria-current={meta.isToday ? "date" : undefined}
                                aria-pressed={isSelected}
                                aria-label={
                                  isSelectable
                                    ? `${tehranDateLabel(dateKey)} — ${count} available ${count === 1 ? "time" : "times"}`
                                    : `${tehranDateLabel(dateKey)} — no availability`
                                }
                                sx={{
                                  width: "100%",
                                  minHeight: 44,
                                  display: "flex",
                                  flexDirection: "column",
                                  alignItems: "center",
                                  justifyContent: "center",
                                  gap: 0.25,
                                  border: "1px solid",
                                  borderColor: isSelected ? "primary.main" : "divider",
                                  borderRadius: 1,
                                  bgcolor: isSelected ? "action.selected" : "background.paper",
                                  opacity: meta.isCurrentMonth ? (isSelectable ? 1 : 0.4) : 0.25,
                                  cursor: isSelectable ? "pointer" : "default",
                                  font: "inherit",
                                  color: "inherit",
                                  "&:focus-visible": { outline: "2px solid", outlineColor: "primary.main", outlineOffset: 2 },
                                }}
                              >
                                <Typography variant="body2" fontWeight={meta.isToday ? 700 : 400}>
                                  {dayNumber}
                                </Typography>
                                {isSelectable ? (
                                  <Box
                                    aria-hidden="true"
                                    sx={{ width: 6, height: 6, borderRadius: "50%", bgcolor: "success.main" }}
                                  />
                                ) : null}
                              </Box>
                            );
                          }}
                        />
                      </Stack>
                    </SectionCard>
                  )}
                  <Stack sx={mobileStickyActionsSx}>
                    <Button
                      type="button"
                      variant="outlined"
                      onClick={() => setActiveStep(0)}
                      startIcon={<ArrowBackRoundedIcon />}
                      sx={{ alignSelf: { xs: "stretch", sm: "flex-start" } }}
                    >
                      Back
                    </Button>
                  </Stack>
                </Stack>
              ) : null}

              {activeStep === 2 ? (
                <Stack ref={focusStepRegion} tabIndex={-1} role="group" aria-label={STEPS[2]} spacing={3} sx={{ outline: "none" }}>
                  <SectionCard title="Choose a time">
                    <Stack spacing={2}>
                      <Typography variant="body2" color="text.secondary">
                        All times are shown in Tehran local time.
                      </Typography>
                      <Stack direction="row" flexWrap="wrap" gap={2}>
                        {slotsForSelectedDate.map((slot) => (
                          <AvailabilityCard
                            key={slot.availabilitySlotId}
                            slot={slot}
                            selected={slot.availabilitySlotId === selectedSlotId}
                            onSelect={handleSelectSlot}
                          />
                        ))}
                      </Stack>
                    </Stack>
                  </SectionCard>
                  <Stack sx={mobileStickyActionsSx}>
                    <Button
                      type="button"
                      variant="outlined"
                      onClick={() => setActiveStep(1)}
                      startIcon={<ArrowBackRoundedIcon />}
                      sx={{ alignSelf: { xs: "stretch", sm: "flex-start" } }}
                    >
                      Back
                    </Button>
                  </Stack>
                </Stack>
              ) : null}

              {activeStep === 3 ? (
                <Stack ref={focusStepRegion} tabIndex={-1} role="group" aria-label={STEPS[3]} spacing={3} sx={{ outline: "none" }}>
                  {selectedSlot ? <BookingSummaryCard slot={selectedSlot} /> : null}
                  <SectionCard title="Who is this lesson for?">
                    {bookingMode === "self" ? (
                      <Typography variant="body2" color="text.secondary">
                        Booking for yourself.
                      </Typography>
                    ) : bookingMode === "parent" ? (
                      childrenQuery.isPending ? (
                        <Typography variant="body2" color="text.secondary">
                          Loading your children…
                        </Typography>
                      ) : childrenQuery.isError ? (
                        <ErrorState
                          error={childrenQuery.error}
                          onRetry={() => void childrenQuery.refetch()}
                        />
                      ) : confirmedChildren.length === 0 ? (
                        <EmptyState
                          title="Add a child first"
                          description="You haven't added a confirmed child yet — add one to book a lesson for them."
                          action={
                            <Button
                              component={RouterLink}
                              to={paths.identity.relationships}
                              variant="contained"
                            >
                              Add a child
                            </Button>
                          }
                        />
                      ) : (
                        <FormSelect
                          name="studentId"
                          label="Choose your child"
                          options={confirmedChildren.map((relationship) => ({
                            value: relationship.studentId,
                            label: `Child (${relationship.studentId.slice(0, 8)}…)`,
                          }))}
                        />
                      )
                    ) : (
                      <Stack spacing={2} alignItems="flex-start" width="100%">
                        <FormTextField
                          name="studentId"
                          label="Student id"
                          helperText="The Student's own account id."
                        />
                        <FormTextField
                          name="parentGuardianId"
                          label="Parent/Guardian id (optional)"
                          helperText="Leave blank when an adult Student books independently."
                        />
                      </Stack>
                    )}
                  </SectionCard>
                  <Stack direction="row" spacing={1.5} sx={mobileStickyActionsSx}>
                    <Button
                      type="button"
                      variant="outlined"
                      onClick={() => setActiveStep(2)}
                      startIcon={<ArrowBackRoundedIcon />}
                    >
                      Back
                    </Button>
                    <Button
                      type="button"
                      variant="contained"
                      disabled={bookingMode === "parent" && childrenQuery.isSuccess && confirmedChildren.length === 0}
                      onClick={() => void handleReviewNext()}
                      sx={{ flex: { xs: 1, sm: "initial" } }}
                    >
                      Continue
                    </Button>
                  </Stack>
                </Stack>
              ) : null}

              {activeStep === 4 ? (
                <Stack ref={focusStepRegion} tabIndex={-1} role="group" aria-label={STEPS[4]} spacing={3} sx={{ outline: "none" }}>
                  {selectedSlot ? <BookingSummaryCard slot={selectedSlot} /> : null}
                  <Typography variant="body2" color="text.secondary">
                    Once confirmed, your tutor is notified immediately and this lesson appears in My
                    Lessons — you can message your tutor or reschedule from there at any time.
                  </Typography>
                  {bookSession.isError ? (
                    <BookingStatusBanner
                      status="error"
                      error={bookSession.error}
                      tutorId={effectiveTutorId}
                      onChooseAnotherTime={() => {
                        bookSession.reset();
                        form.setValue("availabilitySlotId", "");
                        void slotsQuery.refetch();
                        setActiveStep(2);
                      }}
                    />
                  ) : null}
                  <Stack direction="row" spacing={1.5} sx={mobileStickyActionsSx}>
                    <Button
                      type="button"
                      variant="outlined"
                      onClick={() => setActiveStep(3)}
                      disabled={bookSession.isPending}
                      startIcon={<ArrowBackRoundedIcon />}
                    >
                      Back
                    </Button>
                    <Button
                      type="submit"
                      variant="contained"
                      size="large"
                      disabled={bookSession.isPending}
                      sx={{ flex: 1 }}
                    >
                      {bookSession.isPending ? "Booking…" : "Confirm Your Lesson"}
                    </Button>
                  </Stack>
                </Stack>
              ) : null}
            </Stack>
          </Form>
        </>
      )}
    </Stack>
  );
}
