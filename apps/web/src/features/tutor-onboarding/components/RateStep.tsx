'use client';

import { useState } from 'react';
import { FormField } from '@/components/ui/FormField';
import { Input } from '@/components/ui/Input';
import { TutorApplication, UpdateDraftCommand } from '../types';
import { StepShell } from './StepShell';

export interface RateStepProps {
  application: TutorApplication;
  onContinue: (patch: UpdateDraftCommand) => void;
  onBack: () => void;
  isSaving: boolean;
  error: string | null;
}

/** EUR-only (matches the rest of the platform's V1 money convention — `ADR-0015` §6's own
 * EUR-only note, applied here too). No min/max is invented — none was sourced either side
 * (ADR-0026 §3, `PREPLY_FORM_DATA_ENTRY_SPEC.md`), and help-center material confirms the rate
 * is adjustable later regardless. */
export function RateStep({ application, onContinue, onBack, isSaving, error }: RateStepProps) {
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
      onContinue={() => onContinue({ hourlyRate: { amountMinorUnits, currency: 'EUR' } })}
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
