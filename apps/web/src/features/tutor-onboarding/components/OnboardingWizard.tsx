'use client';

import { useMemo, useState } from 'react';
import { ApiError } from '@/lib/api/client';
import {
  useReopenTutorApplicationForRevision,
  useSubmitTutorApplication,
  useUpdateTutorApplicationDraft,
} from '../useTutorOnboarding';
import { firstIncompleteStep, TutorApplication, UpdateDraftCommand, WIZARD_STEPS, WizardStep } from '../types';
import { ApplicationStatusPanel } from './ApplicationStatusPanel';
import { AvailabilityStep } from './AvailabilityStep';
import { BasicsStep } from './BasicsStep';
import { CredentialsStep } from './CredentialsStep';
import { PhotoStep } from './PhotoStep';
import { RateStep } from './RateStep';
import { ReviewStep } from './ReviewStep';
import { TeachingStep } from './TeachingStep';
import { VideoStep } from './VideoStep';
import { WizardProgress } from './WizardProgress';
import styles from './OnboardingWizard.module.css';

export interface OnboardingWizardProps {
  application: TutorApplication;
}

function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.problem.detail ?? error.message;
  }
  return "Something went wrong. Please try again.";
}

/** The orchestrator (`WizardShell`, ADR-0026 §7) — owns which step is showing and advances on a
 * successful save. Which step is *showing* is plain component state, never persisted (not local
 * storage, not the URL): a refresh simply re-derives the first incomplete step from the real,
 * already-saved application data, so nothing is ever lost, only the current scroll position. */
export function OnboardingWizard({ application }: OnboardingWizardProps) {
  const [step, setStep] = useState<WizardStep>(() => firstIncompleteStep(application));
  const updateDraft = useUpdateTutorApplicationDraft();
  const submit = useSubmitTutorApplication();
  const reopen = useReopenTutorApplicationForRevision();

  const completedSteps = useMemo(() => {
    const complete = new Set<WizardStep>();
    if (application.displayName && application.headline) complete.add('basics');
    if (application.profilePhotoReference) complete.add('photo');
    if (application.subjectIds.length > 0 && application.spokenLanguages.length > 0) complete.add('teaching');
    if (application.educationEntries.length > 0 || application.certificateEntries.length > 0) complete.add('credentials');
    if (application.introVideoReference) complete.add('video');
    if (application.availabilitySettingsConfigured) complete.add('availability');
    if (application.hourlyRateAmountMinorUnits) complete.add('rate');
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

  const saveError = updateDraft.isError ? errorMessage(updateDraft.error) : null;

  return (
    <div className={styles.wizard}>
      <WizardProgress currentStep={step} completedSteps={completedSteps} onSelectStep={setStep} />

      {step === 'basics' && (
        <BasicsStep application={application} onContinue={handleContinue} isSaving={updateDraft.isPending} error={saveError} />
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
      {step === 'teaching' && (
        <TeachingStep
          application={application}
          onContinue={handleContinue}
          onBack={goToPreviousStep}
          isSaving={updateDraft.isPending}
          error={saveError}
        />
      )}
      {step === 'credentials' && (
        <CredentialsStep
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
      {step === 'rate' && (
        <RateStep
          application={application}
          onContinue={handleContinue}
          onBack={goToPreviousStep}
          isSaving={updateDraft.isPending}
          error={saveError}
        />
      )}
      {step === 'review' && (
        <ReviewStep
          application={application}
          onBack={goToPreviousStep}
          onSubmit={() => submit.mutate()}
          isSubmitting={submit.isPending}
          error={submit.isError ? errorMessage(submit.error) : null}
        />
      )}
    </div>
  );
}
