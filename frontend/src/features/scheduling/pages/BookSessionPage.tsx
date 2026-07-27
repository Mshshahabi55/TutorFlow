import { useState } from "react";
import { Link as RouterLink, useSearchParams } from "react-router-dom";
import { Button, Chip, Step, StepLabel, Stepper, Stack, Typography } from "@mui/material";
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
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { useNotification } from "@/shared/hooks/useNotification";
import { useRememberedId } from "@/shared/hooks/useRememberedId";
import { tehranDateKey, tehranDateLabel } from "@/shared/time/tehranTime";
import type { AvailabilitySlotDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

const STEPS = ["Choose Tutor", "Choose Date", "Choose Time", "Review", "Confirm"] as const;

/**
 * Pins the wizard's primary action(s) to the bottom of the viewport on
 * mobile, where the button would otherwise scroll out of reach below a
 * long Tutor summary/booking summary — desktop keeps the button inline,
 * already visible without scrolling. `env(safe-area-inset-bottom)` clears
 * a device's home-indicator/gesture bar instead of the button sitting
 * flush against it.
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
 * Slot — same command, validation, and route as before. RC2 replaces the
 * old single-page form with a 5-step wizard (Choose Tutor → Choose Date →
 * Choose Time → Review → Confirm) so the whole form is never shown at
 * once, reusing the exact same query parameters, hooks, and mutation:
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
 * the page shows a friendly prompt to go find one instead of the old bare
 * id-entry fallback (RC2: no raw GUID fields for a normal user).
 */
export function BookSessionPage() {
  const [searchParams] = useSearchParams();
  const bookSession = useBookSession();
  const { notify } = useNotification();
  const { id: rememberedStudentId, remember: rememberStudentId } = useRememberedId("student");
  const { id: rememberedParentGuardianId, remember: rememberParentGuardianId } =
    useRememberedId("parentGuardian");

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

  const form = useForm<BookSessionFormValues>({
    resolver: zodResolver(bookSessionSchema),
    defaultValues: {
      availabilitySlotId: availabilitySlotIdParam ?? "",
      studentId: rememberedStudentId ?? "",
      parentGuardianId: rememberedParentGuardianId ?? "",
    },
  });

  const selectedSlotId = form.watch("availabilitySlotId");
  const selectedSlot: AvailabilitySlotDto | undefined =
    slotsQuery.data?.find((slot) => slot.availabilitySlotId === selectedSlotId) ??
    (slotByIdQuery.data?.availabilitySlotId === selectedSlotId ? slotByIdQuery.data : undefined);

  const hasContext = Boolean(effectiveTutorId);
  // Only blocks on a skeleton once a Tutor is actually known (`tutorId` was
  // given directly, or the slot lookup already resolved one).
  const isContextLoading = hasContext && (tutorQuery.isPending || slotsQuery.isPending);

  function handleSubmit(values: BookSessionFormValues) {
    bookSession.mutate(
      {
        availabilitySlotId: values.availabilitySlotId,
        studentId: values.studentId,
        parentGuardianId: values.parentGuardianId || null,
      },
      {
        onSuccess: () => {
          rememberStudentId(values.studentId);
          if (values.parentGuardianId) {
            rememberParentGuardianId(values.parentGuardianId);
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
        <BookingStatusBanner status="success" session={bookSession.data} />
      </Stack>
    );
  }

  if (hasContext && tutorQuery.isError) {
    return (
      <Stack spacing={2} maxWidth={480} mx="auto" alignItems="center" textAlign="center" py={4}>
        <Typography variant="h4" component="h1">
          We couldn&rsquo;t load this tutor
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Something went wrong on our end. Try again, or find another tutor with open times now.
        </Typography>
        <Stack direction="row" spacing={1.5} mt={1}>
          <Button variant="outlined" onClick={() => void tutorQuery.refetch()}>
            Try again
          </Button>
          <Button
            component={RouterLink}
            to={paths.discovery.tutorSearch}
            variant="contained"
            startIcon={<SearchRoundedIcon />}
          >
            Find Tutors
          </Button>
        </Stack>
      </Stack>
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

  const dateKeys = Array.from(new Set(openSlots.map((slot) => tehranDateKey(slot.startTimeUtc)))).sort();
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
          <Typography variant="body2" color="text.secondary" sx={{ display: { xs: "block", sm: "none" } }}>
            Step {activeStep + 1} of {STEPS.length}: {STEPS[activeStep]}
          </Typography>
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
        <Form form={form} onSubmit={handleSubmit}>
          <Stack spacing={3}>
            {activeStep === 0 ? (
              <Stack spacing={3}>
                {tutorQuery.isSuccess ? <TutorSummaryCard tutor={tutorQuery.data} /> : null}
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
              <Stack spacing={3}>
                {slotsQuery.isError ? (
                  <ErrorState
                    error={slotsQuery.error}
                    onRetry={() => void slotsQuery.refetch()}
                    title="This tutor's availability could not be loaded"
                  />
                ) : (
                  <SectionCard title="Choose a date">
                    <Stack direction="row" flexWrap="wrap" gap={1.5}>
                      {dateKeys.map((dateKey) => (
                        <Chip
                          key={dateKey}
                          label={tehranDateLabel(dateKey)}
                          color={dateKey === selectedDateKey ? "primary" : "default"}
                          variant={dateKey === selectedDateKey ? "filled" : "outlined"}
                          onClick={() => handleSelectDate(dateKey)}
                          sx={{ height: 48, px: 1, fontSize: "0.95rem" }}
                        />
                      ))}
                    </Stack>
                  </SectionCard>
                )}
                <Button
                  type="button"
                  variant="outlined"
                  onClick={() => setActiveStep(0)}
                  startIcon={<ArrowBackRoundedIcon />}
                  sx={{ alignSelf: "flex-start" }}
                >
                  Back
                </Button>
              </Stack>
            ) : null}

            {activeStep === 2 ? (
              <Stack spacing={3}>
                <SectionCard title="Choose a time">
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
                </SectionCard>
                <Button
                  type="button"
                  variant="outlined"
                  onClick={() => setActiveStep(1)}
                  startIcon={<ArrowBackRoundedIcon />}
                  sx={{ alignSelf: "flex-start" }}
                >
                  Back
                </Button>
              </Stack>
            ) : null}

            {activeStep === 3 ? (
              <Stack spacing={3}>
                {selectedSlot && tutorQuery.isSuccess ? (
                  <BookingSummaryCard tutor={tutorQuery.data} slot={selectedSlot} />
                ) : null}
                <SectionCard title="Who is this lesson for?">
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
                  <Button type="button" variant="contained" onClick={() => void handleReviewNext()}>
                    Continue
                  </Button>
                </Stack>
              </Stack>
            ) : null}

            {activeStep === 4 ? (
              <Stack spacing={3}>
                {selectedSlot && tutorQuery.isSuccess ? (
                  <BookingSummaryCard tutor={tutorQuery.data} slot={selectedSlot} />
                ) : null}
                {bookSession.isError ? (
                  <BookingStatusBanner status="error" error={bookSession.error} />
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
      )}
    </Stack>
  );
}
