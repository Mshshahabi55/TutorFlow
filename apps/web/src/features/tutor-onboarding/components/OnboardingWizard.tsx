'use client';

import { useMemo, useState } from 'react';
import { useSetHeaderVariant } from '@/components/layout/HeaderVariantContext';
import { ApiError } from '@/lib/api/client';
import {
  useReopenTutorApplicationForRevision,
  useSubmitTutorApplication,
  useUpdateTutorApplicationDraft,
} from '../useTutorOnboarding';
import { firstIncompleteStep, TutorApplication, UpdateDraftCommand, WIZARD_STEPS, WizardStep } from '../types';
import { AboutStep } from './AboutStep';
import { ApplicationStatusPanel } from './ApplicationStatusPanel';
import { AvailabilityStep } from './AvailabilityStep';
import { CertificationStep } from './CertificationStep';
import { DescriptionStep } from './DescriptionStep';
import { EducationStep } from './EducationStep';
import { PhotoStep } from './PhotoStep';
import { PricingStep } from './PricingStep';
import { VideoStep } from './VideoStep';
import { WizardProgress } from './WizardProgress';
import styles from './OnboardingWizard.module.css';

export interface OnboardingWizardProps {
  application: TutorApplication;
}

/** Surfaces the backend's specific per-field validation messages (ASP.NET's `errors` extension —
 * a flat string array for business-rule failures like `GetSubmissionReadinessErrors`, a
 * field-keyed object for FluentValidation failures) rather than falling back to a generic
 * "One or more errors occurred." title. This matters more now than before the Review step was
 * removed (16-product-decisions-and-open-questions.md item 11): Review used to show its own
 * frontend-computed "what's missing" checklist for the applicant to self-diagnose a failed
 * submit; Pricing — now the final step — has no such checklist, so a readable backend error is
 * the only remaining way an applicant learns *why* submission failed. Mirrors the identical,
 * already-established pattern in `features/auth/errorMessage.ts`. */
function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const errors = error.problem.errors;
    if (Array.isArray(errors) && errors.length > 0) {
      return errors.join(' ');
    }
    if (errors && typeof errors === 'object') {
      const values = Object.values(errors).flat();
      if (values.length > 0) {
        return values.join(' ');
      }
    }
    return error.problem.detail ?? error.message;
  }
  return 'Something went wrong. Please try again.';
}

/** The orchestrator (`WizardShell`, ADR-0026 §7) — owns which step is showing and advances on a
 * successful save. Which step is *showing* is plain component state, never persisted (not local
 * storage, not the URL): a refresh simply re-derives the first incomplete step from the real,
 * already-saved application data, so nothing is ever lost, only the current scroll position. */
export function OnboardingWizard({ application }: OnboardingWizardProps) {
  // Mirrors Preply's own real tutor-application wizard, which drops the main marketplace nav for
  // a minimal logo + help + avatar header while the applicant is mid-flow (OBSERVED,
  // docs/product/preply-reverse-engineering/18-screenshot-evidence.md, SS06).
  useSetHeaderVariant('minimal');
  const [step, setStep] = useState<WizardStep>(() => firstIncompleteStep(application));
  const updateDraft = useUpdateTutorApplicationDraft();
  const submit = useSubmitTutorApplication();
  const reopen = useReopenTutorApplicationForRevision();

  const completedSteps = useMemo(() => {
    const complete = new Set<WizardStep>();
    if (application.displayName && application.subjectIds.length > 0 && application.spokenLanguages.length > 0) complete.add('about');
    if (application.profilePhotoReference) complete.add('photo');
    if (application.certificateEntries.length > 0) complete.add('certification');
    if (application.educationEntries.length > 0) complete.add('education');
    if (application.headline) complete.add('description');
    if (application.introVideoReference) complete.add('video');
    if (application.availabilitySettingsConfigured) complete.add('availability');
    if (application.hourlyRateAmountMinorUnits) complete.add('pricing');
    return complete;
  }, [application]);

  if (application.status !== 'Draft') {
    return (
      <ApplicationStatusPanel
        application={application}
        onReopenForRevision={() => reopen.mutate()}
        isReopening={reopen.isPending}
        reopenError={reopen.isError ? errorMessage(reopen.error) : null}
      />
    );
  }

  const goToNextStep = () => {
    const index = WIZARD_STEPS.indexOf(step);
    setStep(WIZARD_STEPS[Math.min(index + 1, WIZARD_STEPS.length - 1)]);
  };

  const goToPreviousStep = () => {
    const index = WIZARD_STEPS.indexOf(step);
    setStep(WIZARD_STEPS[Math.max(index - 1, 0)]);
  };

  const handleContinue = (patch: UpdateDraftCommand) => {
    updateDraft.mutate(patch, { onSuccess: goToNextStep });
  };

  // Pricing is the final step and carries the submit action directly — matching Preply's real
  // flow, where "Complete registration" sits on the Pricing step itself with no separate review
  // screen (SS13). The rate patch is saved first, then submit only fires once that save
  // succeeds — the backend's own GetSubmissionReadinessErrors is still the sole authority on
  // whether the application is actually ready; this is purely sequencing, not a second
  // validation ruleset.
  const handleSubmit = (patch: UpdateDraftCommand) => {
    updateDraft.mutate(patch, { onSuccess: () => submit.mutate() });
  };

  const saveError = updateDraft.isError ? errorMessage(updateDraft.error) : null;
  const submitError = submit.isError ? errorMessage(submit.error) : null;

  return (
    <div className={styles.wizard}>
      <WizardProgress currentStep={step} completedSteps={completedSteps} onSelectStep={setStep} />

      {step === 'about' && (
        <AboutStep application={application} onContinue={handleContinue} isSaving={updateDraft.isPending} error={saveError} />
      )}
      {step === 'photo' && (
        <PhotoStep
          application={application}
          onContinue={handleContinue}
          onBack={goToPreviousStep}
          isSaving={updateDraft.isPending}
          error={saveError}
        />
      )}
      {step === 'certification' && (
        <CertificationStep
          application={application}
          onContinue={handleContinue}
          onBack={goToPreviousStep}
          isSaving={updateDraft.isPending}
          error={saveError}
        />
      )}
      {step === 'education' && (
        <EducationStep
          application={application}
          onContinue={handleContinue}
          onBack={goToPreviousStep}
          isSaving={updateDraft.isPending}
          error={saveError}
        />
      )}
      {step === 'description' && (
        <DescriptionStep
          application={application}
          onContinue={handleContinue}
          onBack={goToPreviousStep}
          isSaving={updateDraft.isPending}
          error={saveError}
        />
      )}
      {step === 'video' && (
        <VideoStep
          application={application}
          onContinue={handleContinue}
          onBack={goToPreviousStep}
          isSaving={updateDraft.isPending}
          error={saveError}
        />
      )}
      {step === 'availability' && (
        <AvailabilityStep
          application={application}
          onContinue={handleContinue}
          onBack={goToPreviousStep}
          isSaving={updateDraft.isPending}
          error={saveError}
        />
      )}
      {step === 'pricing' && (
        <PricingStep
          application={application}
          onBack={goToPreviousStep}
          onSubmit={handleSubmit}
          isSaving={updateDraft.isPending || submit.isPending}
          error={saveError ?? submitError}
        />
      )}
    </div>
  );
}
