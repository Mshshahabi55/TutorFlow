'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Checkbox } from '@/components/ui/Checkbox';
import { FormField } from '@/components/ui/FormField';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { EducationEntryDraft, TutorApplication, UpdateDraftCommand } from '../types';
import { StepShell } from './StepShell';
import styles from './EducationStep.module.css';

export interface EducationStepProps {
  application: TutorApplication;
  onContinue: (patch: UpdateDraftCommand) => void;
  onBack: () => void;
  isSaving: boolean;
  error: string | null;
}

const EMPTY_EDUCATION: EducationEntryDraft = { institution: '', degree: '', fieldOfStudy: null, startYear: null, endYear: null };

const PRESENT_VALUE = 'present';

function buildYearOptions(): { value: string; label: string }[] {
  const currentYear = new Date().getFullYear();
  const years: { value: string; label: string }[] = [];
  for (let year = currentYear + 1; year >= 1950; year--) {
    years.push({ value: String(year), label: String(year) });
  }
  return years;
}

const START_YEAR_OPTIONS = buildYearOptions();
const END_YEAR_OPTIONS = [{ value: PRESENT_VALUE, label: 'Present' }, ...START_YEAR_OPTIONS];

/** Preply's evidenced "Education" step (step 4 of 8 —
 * docs/product/preply-reverse-engineering/18-screenshot-evidence.md, SS04/SS05;
 * freshly re-confirmed against evidence/Preply_Educations/ in
 * 27-phase3-audit-completion-2026-10-25.md §1.3) — split out from the former combined
 * `CredentialsStep`, which merged this with Certification into one TutorFlow step even though
 * Preply's real flow keeps them separate (OBSERVED, not just DOCUMENTED). Optional on both sides
 * — no sourced rule requires it (ADR-0026 §3). The "Diploma verified" badge upload (SS05) has no
 * TutorFlow equivalent — not built here, out of scope for a step-reordering change.
 *
 * Specialization/Years-of-study mirror `fieldOfStudy`/`startYear`/`endYear` — fields that already
 * existed on `EducationEntryDraft` and already round-trip through the real backend
 * (`EducationEntry.cs`, `UpdateDraft.cs`, `GetCurrentApplication.cs`) but had no input control
 * until now; the 27-phase3-audit flagged this as a stronger gap than the already-known,
 * accepted diploma-upload omission, since these are real, persisted fields invisible to every
 * tutor. "I don't have a higher education degree" mirrors `CertificationStep.tsx`'s identical,
 * already-established pattern for the same reason: no separately persisted flag — an empty
 * `educationEntries` collection already represents it (ADR-0026 §3 precedent).
 *
 * Deliberately NOT included here: a "Degree type" field (Teaching degree / Degree in the subject
 * you teach / Other degree — directly observed on Preply's real step). Unlike the three fields
 * above, no backend field exists for it at all (`EducationEntry.cs` has no such property) — adding
 * it needs a schema decision (migration, domain property, DTO/validator updates) this change
 * deliberately does not make unilaterally. Flagged for the architect, not implemented here. */
export function EducationStep({ application, onContinue, onBack, isSaving, error }: EducationStepProps) {
  const [education, setEducation] = useState<EducationEntryDraft[]>(application.educationEntries);
  const [noEducation, setNoEducation] = useState(false);
  const [localError, setLocalError] = useState<string | null>(null);

  const updateEntry = (index: number, patch: Partial<EducationEntryDraft>) => {
    setEducation((prev) => prev.map((entry, i) => (i === index ? { ...entry, ...patch } : entry)));
  };

  const handleNoEducationChange = (checked: boolean) => {
    setNoEducation(checked);
    if (checked) {
      setEducation([]);
      setLocalError(null);
    }
  };

  const handleContinue = () => {
    const invalid = education.some((entry) => entry.startYear !== null && entry.endYear !== null && entry.startYear > entry.endYear);
    if (invalid) {
      setLocalError('Start year must not be after end year.');
      return;
    }
    setLocalError(null);
    onContinue({ educationEntries: education });
  };

  return (
    <StepShell
      title="Education"
      description="Tell students more about the higher education that you've completed or are working on."
      onBack={onBack}
      onContinue={handleContinue}
      isSaving={isSaving}
      error={localError ?? error}
    >
      <Checkbox checked={noEducation} onCheckedChange={handleNoEducationChange}>
        I don&apos;t have a higher education degree
      </Checkbox>

      {!noEducation && (
        <div className={styles.entries}>
          {education.map((entry, index) => (
            <Card key={index} className={styles.entry}>
              <FormField label="Institution">
                {(props) => (
                  <Input
                    {...props}
                    value={entry.institution}
                    onChange={(e) => updateEntry(index, { institution: e.target.value })}
                  />
                )}
              </FormField>
              <FormField label="Degree">
                {(props) => (
                  <Input {...props} value={entry.degree} onChange={(e) => updateEntry(index, { degree: e.target.value })} />
                )}
              </FormField>
              <FormField label="Specialization" helperText="Optional">
                {(props) => (
                  <Input
                    {...props}
                    value={entry.fieldOfStudy ?? ''}
                    onChange={(e) => updateEntry(index, { fieldOfStudy: e.target.value || null })}
                  />
                )}
              </FormField>
              <div className={styles.yearRange}>
                <FormField label="Start year">
                  {(props) => (
                    <Select
                      {...props}
                      value={entry.startYear !== null ? String(entry.startYear) : ''}
                      onValueChange={(value) => updateEntry(index, { startYear: Number(value) })}
                      options={START_YEAR_OPTIONS}
                      placeholder="Select"
                    />
                  )}
                </FormField>
                <FormField label="End year">
                  {(props) => (
                    <Select
                      {...props}
                      value={entry.endYear !== null ? String(entry.endYear) : PRESENT_VALUE}
                      onValueChange={(value) => updateEntry(index, { endYear: value === PRESENT_VALUE ? null : Number(value) })}
                      options={END_YEAR_OPTIONS}
                      placeholder="Select"
                    />
                  )}
                </FormField>
              </div>
              <Button type="button" variant="ghost" size="sm" onClick={() => setEducation((prev) => prev.filter((_, i) => i !== index))}>
                Remove
              </Button>
            </Card>
          ))}
          <Button type="button" variant="secondary" size="sm" onClick={() => setEducation((prev) => [...prev, { ...EMPTY_EDUCATION }])}>
            Add another education
          </Button>
        </div>
      )}
    </StepShell>
  );
}
