import { Link as RouterLink, useSearchParams } from "react-router-dom";
import { Box, Button, Stack, Typography } from "@mui/material";
import ArrowBackRoundedIcon from "@mui/icons-material/ArrowBackRounded";
import EventAvailableRoundedIcon from "@mui/icons-material/EventAvailableRounded";
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
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { useNotification } from "@/shared/hooks/useNotification";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { timeSpanToMinutes } from "@/shared/utils/duration";
import { DeliveryMode } from "@/services/api/dtos";
import type { AvailabilitySlotDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

/**
 * POST /sessions books a Session against an already-declared Availability
 * Slot — the same command, validation, and route as before (Phase 3 Step
 * 4 is presentation-only). Two optional query parameters add context,
 * both reusing existing capabilities rather than a new endpoint:
 *
 * - `tutorId` (from the Tutor Profile's "Book Session" CTA) resolves
 *   `useTutor` (identity) for the Tutor Summary, and `useTutorAvailabilitySlots`
 *   (the same capability `SessionDetailPage`'s reschedule picker already
 *   reuses) for a visual Availability picker.
 * - `availabilitySlotId` (from `AvailabilitySlotDetailPage`'s "Book this
 *   slot" link, unchanged) resolves `useAvailabilitySlot` for a "Selected
 *   Session" summary, and — when `tutorId` itself wasn't given — supplies
 *   the Tutor id via the slot's own `tutorId` field.
 *
 * Without either parameter, the page behaves exactly as before: three
 * plain id fields, no fetched context.
 */
export function BookSessionPage() {
  const [searchParams] = useSearchParams();
  const bookSession = useBookSession();
  const { notify } = useNotification();

  const tutorIdParam = searchParams.get("tutorId") ?? undefined;
  const availabilitySlotIdParam = searchParams.get("availabilitySlotId") ?? undefined;

  const slotByIdQuery = useAvailabilitySlot(availabilitySlotIdParam);
  const effectiveTutorId = tutorIdParam ?? slotByIdQuery.data?.tutorId;
  const tutorQuery = useTutor(effectiveTutorId);
  const slotsQuery = useTutorAvailabilitySlots(effectiveTutorId);

  const form = useForm<BookSessionFormValues>({
    resolver: zodResolver(bookSessionSchema),
    defaultValues: {
      availabilitySlotId: availabilitySlotIdParam ?? "",
      studentId: "",
      parentGuardianId: "",
    },
  });

  const selectedSlotId = form.watch("availabilitySlotId");
  const selectedSlot: AvailabilitySlotDto | undefined =
    slotsQuery.data?.find((slot) => slot.availabilitySlotId === selectedSlotId) ??
    (slotByIdQuery.data?.availabilitySlotId === selectedSlotId ? slotByIdQuery.data : undefined);

  const hasContext = Boolean(effectiveTutorId);
  // Only blocks on a skeleton once a Tutor is actually known (`tutorId` was
  // given directly, or the slot lookup already resolved one) — arriving
  // with only an `availabilitySlotId` and no resolvable Tutor yet renders
  // the plain fallback form immediately instead, exactly as before.
  const isContextLoading = hasContext && (tutorQuery.isPending || slotsQuery.isPending);

  function handleSubmit(values: BookSessionFormValues) {
    bookSession.mutate(
      {
        availabilitySlotId: values.availabilitySlotId,
        studentId: values.studentId,
        parentGuardianId: values.parentGuardianId || null,
      },
      {
        onSuccess: () => notify({ message: "Session booked.", severity: "success" }),
      },
    );
  }

  function handleSelectSlot(slot: AvailabilitySlotDto) {
    form.setValue("availabilitySlotId", slot.availabilitySlotId, {
      shouldValidate: true,
      shouldDirty: true,
    });
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
      <Stack spacing={3} maxWidth={720} alignItems="center" textAlign="center" py={4}>
        <Typography variant="h4" component="h1">
          This tutor could not be loaded
        </Typography>
        <Box maxWidth={480} width="100%">
          <ErrorState error={tutorQuery.error} onRetry={() => void tutorQuery.refetch()} />
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

  const openSlots = (slotsQuery.data ?? []).filter((slot) => !slot.isConsumed);
  const noAvailability = hasContext && slotsQuery.isSuccess && openSlots.length === 0 && !selectedSlot;
  const showManualSlotField = !hasContext || openSlots.length === 0;

  return (
    <Stack spacing={3} maxWidth={720}>
      <BookingHeader hasContext={hasContext} />

      {hasContext && tutorQuery.isSuccess ? <TutorSummaryCard tutor={tutorQuery.data} /> : null}

      {noAvailability ? (
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
      ) : (
        <>
          {selectedSlot ? (
            <SectionCard title="Selected Session">
              <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
                <EventAvailableRoundedIcon color="primary" aria-hidden="true" />
                <Typography variant="body1">
                  {toTehranDisplay(selectedSlot.startTimeUtc)} ·{" "}
                  {timeSpanToMinutes(selectedSlot.duration)} min ·{" "}
                  {selectedSlot.deliveryMode === DeliveryMode.Online ? "Online" : "In-Person"}
                </Typography>
              </Stack>
            </SectionCard>
          ) : null}

          {hasContext && slotsQuery.isSuccess && openSlots.length > 0 ? (
            <SectionCard title="Availability">
              <Stack direction="row" flexWrap="wrap" gap={2}>
                {openSlots.map((slot) => (
                  <AvailabilityCard
                    key={slot.availabilitySlotId}
                    slot={slot}
                    selected={slot.availabilitySlotId === selectedSlotId}
                    onSelect={handleSelectSlot}
                  />
                ))}
              </Stack>
            </SectionCard>
          ) : null}

          {hasContext && slotsQuery.isError ? (
            <ErrorState
              error={slotsQuery.error}
              onRetry={() => void slotsQuery.refetch()}
              title="This tutor's availability could not be loaded"
            />
          ) : null}

          <Form form={form} onSubmit={handleSubmit}>
            <Stack spacing={3}>
              <SectionCard title="Booking Form">
                <Stack spacing={2} alignItems="flex-start" width="100%">
                  {showManualSlotField ? (
                    <FormTextField
                      name="availabilitySlotId"
                      label="Availability Slot id"
                      helperText="The id shared by the declaring Tutor, or copied from an Availability Slot's detail page."
                    />
                  ) : null}
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

              {bookSession.isError ? <BookingStatusBanner status="error" error={bookSession.error} /> : null}

              {hasContext && tutorQuery.isSuccess && selectedSlot ? (
                <BookingSummaryCard tutor={tutorQuery.data} slot={selectedSlot} />
              ) : null}

              <Button
                type="submit"
                variant="contained"
                size="large"
                disabled={bookSession.isPending}
                sx={{ alignSelf: { xs: "stretch", sm: "flex-start" } }}
              >
                {bookSession.isPending ? "Booking…" : "Book session"}
              </Button>
            </Stack>
          </Form>
        </>
      )}
    </Stack>
  );
}
