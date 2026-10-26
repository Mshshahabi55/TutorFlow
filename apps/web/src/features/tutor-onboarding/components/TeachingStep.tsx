'use client';

import { useState } from 'react';
import { FormField } from '@/components/ui/FormField';
import { FormSection } from '@/components/ui/FormSection';
import { Input } from '@/components/ui/Input';
import { Textarea } from '@/components/ui/Textarea';
import { MultiSelect } from '@/components/ui/MultiSelect';
import { Skeleton } from '@/components/ui/Skeleton';
import { useSpecialties } from '@/features/catalog/useCatalog';
import { TutorApplication, UpdateDraftCommand } from '../types';
import { StepShell } from './StepShell';

export interface TeachingStepProps {
  application: TutorApplication;
  onContinue: (patch: UpdateDraftCommand) => void;
  onBack: () => void;
  isSaving: boolean;
  error: string | null;
}

/** DORMANT, not deleted — this component is no longer rendered anywhere in `OnboardingWizard`
 * (removed from `WIZARD_STEPS`), per the 2026-10-19 wizard-reordering change that aligned
 * TutorFlow's wizard to Preply's evidenced 8 real steps
 * (docs/product/preply-reverse-engineering/18-screenshot-evidence.md,
 * 07-wizard-inventory.md). This step's three fields — specialties, years of experience,
 * experience summary — have never been evidenced as belonging to any Preply step at all (not
 * folded into another step on purpose; there was no evidence to fold them into). The backend
 * field/schema/API contract (`specialtyIds`, `yearsOfExperience`, `experienceSummary` on
 * `TutorApplication`) is unchanged and untouched — any application that already saved this data
 * keeps it; it is simply no longer reachable or editable through the wizard UI. OPEN PRODUCT
 * QUESTION, not resolved by this change: should this data be exposed elsewhere (e.g. folded into
 * About or a future "profile extras" surface), kept permanently dormant, or have its
 * backend fields formally deprecated? This file is left in place, fully functional and tested in
 * isolation (`TeachingStep.test.tsx`), specifically so a future product decision can re-wire it
 * without reconstructing it from scratch. */
export function TeachingStep({ application, onContinue, onBack, isSaving, error }: TeachingStepProps) {
  const [specialtyIds, setSpecialtyIds] = useState<string[]>(application.specialtyIds);
  const [yearsOfExperience, setYearsOfExperience] = useState(application.yearsOfExperience?.toString() ?? '');
  const [experienceSummary, setExperienceSummary] = useState(application.experienceSummary ?? '');

  const { data: specialties, isLoading: specialtiesLoading } = useSpecialties();
  const specialtyOptions = (specialties ?? []).map((s) => ({ value: s.id, label: s.displayName }));

  return (
    <StepShell
      title="Your experience"
      description="Specialties and experience that help students choose you."
      onBack={onBack}
      onContinue={() =>
        onContinue({
          specialtyIds,
          yearsOfExperience: yearsOfExperience ? Number(yearsOfExperience) : undefined,
          experienceSummary,
        })
      }
      isSaving={isSaving}
      error={error}
    >
      <FormSection title="Specialties">
        {specialtiesLoading ? (
          <Skeleton width="100%" height="2.75rem" />
        ) : (
          <MultiSelect
            aria-label="Specialties"
            placeholder="Optional"
            options={specialtyOptions}
            selected={specialtyIds}
            onChange={setSpecialtyIds}
          />
        )}
      </FormSection>

      <FormField label="Years of experience">
        {(props) => (
          <Input
            {...props}
            type="number"
            min={0}
            value={yearsOfExperience}
            onChange={(e) => setYearsOfExperience(e.target.value)}
          />
        )}
      </FormField>

      <FormField label="Experience summary">
        {(props) => (
          <Textarea {...props} value={experienceSummary} onChange={(e) => setExperienceSummary(e.target.value)} rows={4} />
        )}
      </FormField>
    </StepShell>
  );
}
