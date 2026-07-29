import { useCallback, useEffect, useState, type ReactNode } from "react";
import { useParams } from "react-router-dom";
import { Box, Button, Fade, LinearProgress, Step, StepButton, StepLabel, Stepper, Stack, Typography } from "@mui/material";
import type { StepIconProps } from "@mui/material/StepIcon";
import ArrowBackRoundedIcon from "@mui/icons-material/ArrowBackRounded";
import CheckCircleRoundedIcon from "@mui/icons-material/CheckCircleRounded";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTutor } from "@/features/identity/hooks/useTutorQueries";
import { useTutorAvailabilitySlots } from "@/features/scheduling/hooks/useAvailabilitySlotQueries";
import {
  useSetTutorLanguage,
  useSetTutorMedia,
  useSetTutorPersonalInfo,
  useSetTutorPricing,
  useSetTutorSubject,
  useSetTutorTeachingInfo,
  useSubmitTutorProfile,
} from "@/features/identity/hooks/useTutorMutations";
import {
  tutorOnboardingSchema,
  TUTOR_ONBOARDING_DEFAULT_VALUES,
  type TutorOnboardingFormValues,
} from "@/features/identity/validation/tutorOnboardingSchema";
import { parseCommaList, formatCommaList } from "@/features/identity/utils/commaList";
import { encouragingMessageForProgress } from "@/features/identity/utils/onboardingProgressMessage";
import { OnboardingWelcomeScreen } from "@/features/identity/components/onboarding/OnboardingWelcomeScreen";
import { OnboardingWizardSkeleton } from "@/features/identity/components/onboarding/OnboardingWizardSkeleton";
import { PersonalInfoStep } from "@/features/identity/components/onboarding/PersonalInfoStep";
import { TeachingInfoStep } from "@/features/identity/components/onboarding/TeachingInfoStep";
import { MediaStep } from "@/features/identity/components/onboarding/MediaStep";
import { PricingStep } from "@/features/identity/components/onboarding/PricingStep";
import { AvailabilityStep } from "@/features/identity/components/onboarding/AvailabilityStep";
import { VerificationStep } from "@/features/identity/components/onboarding/VerificationStep";
import { ReviewPublishStep } from "@/features/identity/components/onboarding/ReviewPublishStep";
import { Form } from "@/shared/components/forms/Form";
import { PageHeader } from "@/shared/components/PageHeader";
import { UnavailableState } from "@/shared/components/feedback/UnavailableState";
import { useNotification } from "@/shared/hooks/useNotification";
import { tomanToRial, toTomanInputValue } from "@/shared/money/rial";
import type { TutorDto } from "@/services/api/dtos";

const STEPS = [
  "Personal Information",
  "Teaching Information",
  "Profile Media",
  "Pricing",
  "Availability",
  "Verification",
  "Review & Publish",
] as const;

/**
 * Pins each step's primary action(s) to the bottom of the viewport on
 * mobile, the same convention `BookSessionPage` established (Phase 5) —
 * kept as a page-local copy rather than a shared import so this phase's
 * changes stay scoped to the onboarding page.
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

function lastStepStorageKey(tutorId: string) {
  return `tutorflow.onboardingStep.${tutorId}`;
}

function toFormValues(tutor: TutorDto): TutorOnboardingFormValues {
  return {
    ...TUTOR_ONBOARDING_DEFAULT_VALUES,
    displayName: tutor.displayName ?? "",
    headline: tutor.headline ?? "",
    biography: tutor.biography ?? "",
    country: tutor.country ?? "",
    city: tutor.city ?? "",
    nativeLanguage: tutor.language ?? "",
    otherLanguages: formatCommaList(tutor.otherLanguages ?? []),
    tutorSubjects: (tutor.tutorSubjects ?? []).map((entry) => ({ subject: entry.subject, level: entry.level ?? "" })),
    primarySubject: tutor.subject ?? "",
    yearsOfExperience: tutor.yearsOfExperience !== null && tutor.yearsOfExperience !== undefined ? String(tutor.yearsOfExperience) : "",
    education: tutor.education ?? "",
    certifications: tutor.certifications ?? "",
    teachingMethodology: tutor.teachingMethodology ?? "",
    lessonSpecialties: formatCommaList(tutor.lessonSpecialties ?? []),
    photoUrl: tutor.photoUrl ?? "",
    introVideoUrl: tutor.introVideoUrl ?? "",
    galleryImageUrls: formatCommaList(tutor.galleryImageUrls ?? []),
    hourlyRate: tutor.hourlyRate !== null ? toTomanInputValue(tutor.hourlyRate) : "",
    trialLessonAvailable: tutor.trialLessonAvailable ?? false,
    trialLessonPrice: tutor.trialLessonPrice !== null && tutor.trialLessonPrice !== undefined ? toTomanInputValue(tutor.trialLessonPrice) : "",
  };
}

/** Completed steps render a checkmark instead of a number — the same icon `ProfileCompletionCard` already uses for a finished checklist item. */
function OnboardingStepIcon({ active, completed, icon }: StepIconProps) {
  if (completed) {
    return <CheckCircleRoundedIcon color="primary" fontSize="small" aria-hidden="true" />;
  }
  return (
    <Box
      aria-hidden="true"
      sx={{
        width: 24,
        height: 24,
        borderRadius: "50%",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        fontSize: 13,
        fontWeight: 700,
        bgcolor: active ? "primary.main" : "action.disabledBackground",
        color: active ? "primary.contrastText" : "text.secondary",
      }}
    >
      {icon}
    </Box>
  );
}

/** Wraps a step's content in the existing focus-on-mount region convention plus a subtle fade-in, since the ternary below already mounts/unmounts each step fresh. */
function StepPanel({ label, panelRef, children }: { label: string; panelRef: (node: HTMLElement | null) => void; children: ReactNode }) {
  return (
    <Fade in appear timeout={250}>
      <Stack ref={panelRef} tabIndex={-1} role="group" aria-label={label} spacing={3} sx={{ outline: "none" }}>
        {children}
      </Stack>
    </Fade>
  );
}

/**
 * The 7-step Tutor Onboarding Wizard (ADR-024, Accepted, 2026-07-28).
 * One `useForm` instance spans every step — the same "one form across the
 * whole wizard" pattern `BookSessionPage` already established (Phase 5) —
 * each step's own "Continue" autosaves that step's field group via its own
 * PATCH (never a combined "save everything" call: the backend has no such
 * capability, by design, same as `TutorOfferingForm`), then advances.
 * "Resume later" falls out naturally: every field is already persisted by
 * the time a Tutor leaves, and `GET /tutors/{id}` (`useTutor`) is re-read
 * on return; which step to land on is remembered client-side only (a
 * `localStorage` key, same convention `tutorflow.sidebarCollapsed`/
 * `tutorflow.rememberedId.*` already use elsewhere in this app). That same
 * key doubles as the "has this Tutor ever opened the wizard" signal for
 * the one-time Welcome screen below — no separate flag needed.
 */
export function TutorOnboardingWizardPage() {
  const { tutorId } = useParams<{ tutorId: string }>();
  const tutorQuery = useTutor(tutorId);
  const slotsQuery = useTutorAvailabilitySlots(tutorId);
  const { notify } = useNotification();

  const setPersonalInfo = useSetTutorPersonalInfo(tutorId ?? "");
  const setLanguage = useSetTutorLanguage(tutorId ?? "");
  const setTeachingInfo = useSetTutorTeachingInfo(tutorId ?? "");
  const setSubject = useSetTutorSubject(tutorId ?? "");
  const setMedia = useSetTutorMedia(tutorId ?? "");
  const setPricing = useSetTutorPricing(tutorId ?? "");
  const submitProfile = useSubmitTutorProfile(tutorId ?? "");

  const [showWelcome, setShowWelcome] = useState(() => Boolean(tutorId) && window.localStorage.getItem(lastStepStorageKey(tutorId ?? "")) === null);

  const [activeStep, setActiveStep] = useState(() => {
    if (!tutorId) {
      return 0;
    }
    const remembered = Number(window.localStorage.getItem(lastStepStorageKey(tutorId)));
    return Number.isInteger(remembered) && remembered >= 0 && remembered < STEPS.length ? remembered : 0;
  });

  const form = useForm<TutorOnboardingFormValues>({
    resolver: zodResolver(tutorOnboardingSchema),
    defaultValues: TUTOR_ONBOARDING_DEFAULT_VALUES,
  });

  // Prefills the form once the Tutor's current, already-saved data loads —
  // `defaultValues` above is captured once at mount, before that data
  // exists, so a later-arriving fetch needs an explicit reset.
  useEffect(() => {
    if (tutorQuery.isSuccess) {
      form.reset(toFormValues(tutorQuery.data));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tutorQuery.isSuccess, tutorQuery.data?.tutorId]);

  // Unsaved-change protection: each step already autosaves on "Continue,"
  // so this only guards against losing in-progress typing on this step if
  // the Tutor closes the tab/navigates away before clicking it.
  useEffect(() => {
    function handleBeforeUnload(event: BeforeUnloadEvent) {
      if (form.formState.isDirty) {
        event.preventDefault();
      }
    }
    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [form.formState.isDirty]);

  const focusStepRegion = useCallback((node: HTMLElement | null) => {
    node?.focus();
  }, []);

  function goToStep(step: number) {
    setActiveStep(step);
    if (tutorId) {
      window.localStorage.setItem(lastStepStorageKey(tutorId), String(step));
    }
  }

  function handleStart() {
    setShowWelcome(false);
    goToStep(0);
  }

  async function handlePersonalInfoNext() {
    const valid = await form.trigger(["displayName", "headline", "biography", "country", "city", "otherLanguages"]);
    if (!valid) {
      return;
    }
    const values = form.getValues();
    try {
      await Promise.all([
        setPersonalInfo.mutateAsync({
          displayName: values.displayName || null,
          headline: values.headline || null,
          biography: values.biography || null,
          country: values.country || null,
          city: values.city || null,
          otherLanguages: parseCommaList(values.otherLanguages),
        }),
        values.nativeLanguage ? setLanguage.mutateAsync(values.nativeLanguage) : Promise.resolve(),
      ]);
      notify({ message: "Progress saved.", severity: "success", autoHideDurationMs: 2500 });
      goToStep(1);
    } catch {
      notify({ message: "Some changes could not be saved.", severity: "error" });
    }
  }

  async function handleTeachingInfoNext() {
    const valid = await form.trigger(["yearsOfExperience", "tutorSubjects", "lessonSpecialties"]);
    if (!valid) {
      return;
    }
    const values = form.getValues();
    try {
      await Promise.all([
        setTeachingInfo.mutateAsync({
          tutorSubjects: values.tutorSubjects
            .filter((entry) => entry.subject.trim().length > 0)
            .map((entry) => ({ subject: entry.subject, level: entry.level || null })),
          yearsOfExperience: values.yearsOfExperience !== "" ? Number(values.yearsOfExperience) : null,
          education: values.education || null,
          certifications: values.certifications || null,
          teachingMethodology: values.teachingMethodology || null,
          lessonSpecialties: parseCommaList(values.lessonSpecialties),
        }),
        values.primarySubject ? setSubject.mutateAsync(values.primarySubject) : Promise.resolve(),
      ]);
      notify({ message: "Progress saved.", severity: "success", autoHideDurationMs: 2500 });
      goToStep(2);
    } catch {
      notify({ message: "Some changes could not be saved.", severity: "error" });
    }
  }

  async function handleMediaNext() {
    const valid = await form.trigger(["photoUrl", "introVideoUrl", "galleryImageUrls"]);
    if (!valid) {
      return;
    }
    const values = form.getValues();
    try {
      await setMedia.mutateAsync({
        photoUrl: values.photoUrl || null,
        introVideoUrl: values.introVideoUrl || null,
        galleryImageUrls: parseCommaList(values.galleryImageUrls),
      });
      notify({ message: "Progress saved.", severity: "success", autoHideDurationMs: 2500 });
      goToStep(3);
    } catch {
      notify({ message: "Some changes could not be saved.", severity: "error" });
    }
  }

  async function handlePricingNext() {
    const valid = await form.trigger(["hourlyRate", "trialLessonPrice"]);
    if (!valid) {
      return;
    }
    const values = form.getValues();
    try {
      await setPricing.mutateAsync({
        hourlyRateAmount: values.hourlyRate !== "" ? tomanToRial(Number(values.hourlyRate)) : null,
        trialLessonAvailable: values.trialLessonAvailable,
        trialLessonPriceAmount:
          values.trialLessonAvailable && values.trialLessonPrice !== "" ? tomanToRial(Number(values.trialLessonPrice)) : null,
      });
      notify({ message: "Progress saved.", severity: "success", autoHideDurationMs: 2500 });
      goToStep(4);
    } catch {
      notify({ message: "Some changes could not be saved.", severity: "error" });
    }
  }

  async function handlePublish() {
    try {
      await submitProfile.mutateAsync();
      notify({ message: "Profile submitted for Admin review.", severity: "success" });
    } catch {
      notify({ message: "Could not publish your profile. Please try again.", severity: "error" });
    }
  }

  if (showWelcome) {
    return (
      <Stack spacing={3} maxWidth={720}>
        <PageHeader title="Complete your Tutor profile" />
        <OnboardingWelcomeScreen steps={STEPS} onStart={handleStart} />
      </Stack>
    );
  }

  if (tutorQuery.isPending) {
    return (
      <Stack spacing={3} maxWidth={720}>
        <PageHeader title="Complete your Tutor profile" />
        <OnboardingWizardSkeleton />
      </Stack>
    );
  }

  if (tutorQuery.isError) {
    return (
      <UnavailableState
        title="Tutor unavailable"
        description="Your profile could not be loaded. It may have been removed, or the link might be broken."
        actions={[{ label: "Try again", onClick: () => void tutorQuery.refetch() }]}
      />
    );
  }

  const tutor = tutorQuery.data;
  const progressPercent = Math.round(((activeStep + 1) / STEPS.length) * 100);

  return (
    <Stack spacing={3} maxWidth={720}>
      <PageHeader
        title="Complete your Tutor profile"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            A richer profile helps Students trust and choose you. Every step saves automatically —
            come back any time to finish later.
          </Typography>
        }
      />

      <Stepper activeStep={activeStep} alternativeLabel sx={{ display: { xs: "none", md: "flex" } }}>
        {STEPS.map((label, index) => (
          <Step key={label} completed={index < activeStep}>
            <StepButton onClick={() => goToStep(index)} disabled={index > activeStep}>
              <StepLabel StepIconComponent={OnboardingStepIcon}>{label}</StepLabel>
            </StepButton>
          </Step>
        ))}
      </Stepper>
      <Stack spacing={0.75} sx={{ display: { xs: "flex", md: "none" } }}>
        <Stack direction="row" justifyContent="space-between" alignItems="baseline">
          <Typography variant="caption" color="text.secondary">
            Step {activeStep + 1} of {STEPS.length}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {progressPercent}% complete
          </Typography>
        </Stack>
        <Typography variant="subtitle1" fontWeight={700}>
          {STEPS[activeStep]}
        </Typography>
        <LinearProgress
          variant="determinate"
          value={progressPercent}
          aria-label={`Step ${activeStep + 1} of ${STEPS.length}: ${STEPS[activeStep]}`}
          sx={{
            height: 6,
            borderRadius: 999,
            "& .MuiLinearProgress-bar1Determinate": { transition: "transform 400ms ease" },
          }}
        />
        <Typography variant="body2" color="text.secondary" aria-live="polite">
          {encouragingMessageForProgress(progressPercent)}
        </Typography>
      </Stack>

      <Form form={form} onSubmit={() => undefined}>
        {activeStep === 0 ? (
          <StepPanel label={STEPS[0]} panelRef={focusStepRegion}>
            <PersonalInfoStep />
            <Stack sx={mobileStickyActionsSx}>
              <Button
                type="button"
                variant="contained"
                size="large"
                onClick={() => void handlePersonalInfoNext()}
                disabled={setPersonalInfo.isPending || setLanguage.isPending}
                sx={{ alignSelf: "flex-start" }}
              >
                {setPersonalInfo.isPending ? "Saving…" : "Continue"}
              </Button>
            </Stack>
          </StepPanel>
        ) : null}

        {activeStep === 1 ? (
          <StepPanel label={STEPS[1]} panelRef={focusStepRegion}>
            <TeachingInfoStep />
            <Stack direction="row" spacing={1.5} sx={mobileStickyActionsSx}>
              <Button type="button" variant="outlined" startIcon={<ArrowBackRoundedIcon />} onClick={() => goToStep(0)}>
                Back
              </Button>
              <Button
                type="button"
                variant="contained"
                onClick={() => void handleTeachingInfoNext()}
                disabled={setTeachingInfo.isPending || setSubject.isPending}
              >
                {setTeachingInfo.isPending ? "Saving…" : "Continue"}
              </Button>
            </Stack>
          </StepPanel>
        ) : null}

        {activeStep === 2 ? (
          <StepPanel label={STEPS[2]} panelRef={focusStepRegion}>
            <MediaStep />
            <Stack direction="row" spacing={1.5} sx={mobileStickyActionsSx}>
              <Button type="button" variant="outlined" startIcon={<ArrowBackRoundedIcon />} onClick={() => goToStep(1)}>
                Back
              </Button>
              <Button type="button" variant="contained" onClick={() => void handleMediaNext()} disabled={setMedia.isPending}>
                {setMedia.isPending ? "Saving…" : "Continue"}
              </Button>
            </Stack>
          </StepPanel>
        ) : null}

        {activeStep === 3 ? (
          <StepPanel label={STEPS[3]} panelRef={focusStepRegion}>
            <PricingStep />
            <Stack direction="row" spacing={1.5} sx={mobileStickyActionsSx}>
              <Button type="button" variant="outlined" startIcon={<ArrowBackRoundedIcon />} onClick={() => goToStep(2)}>
                Back
              </Button>
              <Button type="button" variant="contained" onClick={() => void handlePricingNext()} disabled={setPricing.isPending}>
                {setPricing.isPending ? "Saving…" : "Continue"}
              </Button>
            </Stack>
          </StepPanel>
        ) : null}

        {activeStep === 4 && tutorId ? (
          <StepPanel label={STEPS[4]} panelRef={focusStepRegion}>
            <AvailabilityStep tutorId={tutorId} />
            <Stack direction="row" spacing={1.5} sx={mobileStickyActionsSx}>
              <Button type="button" variant="outlined" startIcon={<ArrowBackRoundedIcon />} onClick={() => goToStep(3)}>
                Back
              </Button>
              <Button type="button" variant="contained" onClick={() => goToStep(5)}>
                Continue
              </Button>
            </Stack>
          </StepPanel>
        ) : null}

        {activeStep === 5 ? (
          <StepPanel label={STEPS[5]} panelRef={focusStepRegion}>
            <VerificationStep tutor={tutor} />
            <Stack direction="row" spacing={1.5} sx={mobileStickyActionsSx}>
              <Button type="button" variant="outlined" startIcon={<ArrowBackRoundedIcon />} onClick={() => goToStep(4)}>
                Back
              </Button>
              <Button type="button" variant="contained" onClick={() => goToStep(6)}>
                Continue
              </Button>
            </Stack>
          </StepPanel>
        ) : null}

        {activeStep === 6 ? (
          <StepPanel label={STEPS[6]} panelRef={focusStepRegion}>
            <ReviewPublishStep
              tutor={tutor}
              hasAvailability={(slotsQuery.data?.length ?? 0) > 0}
              onPublish={() => void handlePublish()}
              isPublishing={submitProfile.isPending}
              publishError={submitProfile.isError ? "Could not publish your profile. Please try again." : undefined}
              onEditSection={goToStep}
            />
            <Stack sx={mobileStickyActionsSx}>
              <Button type="button" variant="outlined" startIcon={<ArrowBackRoundedIcon />} onClick={() => goToStep(5)} sx={{ alignSelf: "flex-start" }}>
                Back
              </Button>
            </Stack>
          </StepPanel>
        ) : null}
      </Form>
    </Stack>
  );
}
