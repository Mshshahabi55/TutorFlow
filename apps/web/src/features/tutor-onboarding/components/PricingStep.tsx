'use client';

import { useState } from 'react';
import { FormField } from '@/components/ui/FormField';
import { Input } from '@/components/ui/Input';
import { TutorApplication, UpdateDraftCommand } from '../types';
import { StepShell } from './StepShell';

export interface PricingStepProps {
  application: TutorApplication;
  onBack: () => void;
  onSubmit: (patch: UpdateDraftCommand) => void;
  isSaving: boolean;
  error: string | null;
}

/** Preply's real final step, step 8 of 8 — H1 "Set your 50 minute lesson price", footer
 * "Back" / "Complete registration" (docs/product/preply-reverse-engineering/
 * 18-screenshot-evidence.md, SS13). Preply's real flow has no distinct review/summary screen —
 * this step's own Continue action *is* the final submit action
 * (16-product-decisions-and-open-questions.md item 11, now resolved by this change: dropped to
 * match Preply exactly). The rate itself stays hourly/EUR, not a fixed 50-minute/USD price — an
 * already-approved, pre-existing TutorFlow deviation (ADR-0004), unrelated to this step-ordering
 * change and not altered here. The backend's own `GetSubmissionReadinessErrors` remains the sole
 * authoritative validation — this UI never duplicates it, so dropping the old review checklist
 * does not bypass anything the backend wasn't already independently enforcing. */
export function PricingStep({ application, onBack, onSubmit, isSaving, error }: PricingStepProps) {
  const [rate, setRate] = useState(
    application.hourlyRateAmountMinorUnits ? (application.hourlyRateAmountMinorUnits / 100).toString() : '',
  );

  const amountMinorUnits = Math.round(Number(rate) * 100);
  const isValid = rate.trim() !== '' && Number.isFinite(amountMinorUnits) && amountMinorUnits >= 0;

  return (
    <StepShell
      title="Your hourly rate"
      description="You can always adjust this later based on demand and experience."
      onBack={onBack}
      onContinue={() => onSubmit({ hourlyRate: { amountMinorUnits, currency: 'EUR' } })}
      continueLabel="Submit application"
      continueDisabled={!isValid}
      isSaving={isSaving}
      error={error}
    >
      <FormField label="Hourly rate (EUR)" required>
        {(props) => (
          <Input
            {...props}
            type="number"
            min={0}
            step="0.01"
            inputMode="decimal"
            value={rate}
            onChange={(e) => setRate(e.target.value)}
          />
        )}
      </FormField>
    </StepShell>
  );
}
