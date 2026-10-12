'use client';

import { useState } from 'react';
import { FormField } from '@/components/ui/FormField';
import { FormSection } from '@/components/ui/FormSection';
import { Input } from '@/components/ui/Input';
import { Textarea } from '@/components/ui/Textarea';
import { Select } from '@/components/ui/Select';
import { MultiSelect } from '@/components/ui/MultiSelect';
import { Skeleton } from '@/components/ui/Skeleton';
import { useLanguages, useSpecialties, useSubjects } from '@/features/catalog/useCatalog';
import { COUNTRY_OPTIONS } from '@/features/search/constants';
import { LanguageProficiency, SpokenLanguageDraft, TutorApplication, UpdateDraftCommand } from '../types';
import { StepShell } from './StepShell';
import styles from './TeachingStep.module.css';

export interface TeachingStepProps {
  application: TutorApplication;
  onContinue: (patch: UpdateDraftCommand) => void;
  onBack: () => void;
  isSaving: boolean;
  error: string | null;
}

const PROFICIENCY_OPTIONS: { value: LanguageProficiency; label: string }[] = [
  { value: 'Basic', label: 'Basic' },
  { value: 'Conversational', label: 'Conversational' },
  { value: 'Fluent', label: 'Fluent' },
  { value: 'Native', label: 'Native' },
];

export function TeachingStep({ application, onContinue, onBack, isSaving, error }: TeachingStepProps) {
  const [subjectIds, setSubjectIds] = useState<string[]>(application.subjectIds);
  const [specialtyIds, setSpecialtyIds] = useState<string[]>(application.specialtyIds);
  const [languages, setLanguages] = useState<SpokenLanguageDraft[]>(application.spokenLanguages);
  const [countryOfBirthCode, setCountryOfBirthCode] = useState(application.countryOfBirthCode ?? '');
  const [yearsOfExperience, setYearsOfExperience] = useState(application.yearsOfExperience?.toString() ?? '');
  const [experienceSummary, setExperienceSummary] = useState(application.experienceSummary ?? '');

  const { data: subjects, isLoading: subjectsLoading } = useSubjects();
  const { data: specialties, isLoading: specialtiesLoading } = useSpecialties();
  const { data: languageCatalog, isLoading: languagesLoading } = useLanguages();

  const subjectOptions = (subjects ?? []).map((s) => ({ value: s.id, label: s.displayName }));
  const specialtyOptions = (specialties ?? []).map((s) => ({ value: s.id, label: s.displayName }));
  const languageOptions = (languageCatalog ?? []).map((l) => ({ value: l.code, label: l.displayName }));

  const setLanguageCodes = (codes: string[]) => {
    setLanguages((prev) => [
      ...codes.map((code) => prev.find((l) => l.languageCode === code) ?? { languageCode: code, proficiency: 'Fluent' as LanguageProficiency }),
    ]);
  };

  const setProficiency = (code: string, proficiency: LanguageProficiency) => {
    setLanguages((prev) => prev.map((l) => (l.languageCode === code ? { ...l, proficiency } : l)));
  };

  return (
    <StepShell
      title="What you teach"
      description="Subjects, specialties, and languages students use to find you."
      onBack={onBack}
      onContinue={() =>
        onContinue({
          subjectIds,
          specialtyIds,
          spokenLanguages: languages,
          countryOfBirthCode: countryOfBirthCode || undefined,
          yearsOfExperience: yearsOfExperience ? Number(yearsOfExperience) : undefined,
          experienceSummary,
        })
      }
      continueDisabled={subjectIds.length === 0 || languages.length === 0}
      isSaving={isSaving}
      error={error}
    >
      <FormSection title="Subjects">
        {subjectsLoading ? (
          <Skeleton width="100%" height="2.75rem" />
        ) : (
          <MultiSelect
            aria-label="Subjects you teach"
            placeholder="Select at least one subject"
            options={subjectOptions}
            selected={subjectIds}
            onChange={setSubjectIds}
          />
        )}
      </FormSection>

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

      <FormSection title="Languages you speak">
        {languagesLoading ? (
          <Skeleton width="100%" height="2.75rem" />
        ) : (
          <>
            <MultiSelect
              aria-label="Languages you speak"
              placeholder="Select at least one language"
              options={languageOptions}
              selected={languages.map((l) => l.languageCode)}
              onChange={setLanguageCodes}
            />
            {languages.length > 0 && (
              <ul className={styles.proficiencyList}>
                {languages.map((language) => (
                  <li key={language.languageCode} className={styles.proficiencyRow}>
                    <span>{languageOptions.find((o) => o.value === language.languageCode)?.label ?? language.languageCode}</span>
                    <Select
                      value={language.proficiency}
                      onValueChange={(v) => setProficiency(language.languageCode, v as LanguageProficiency)}
                      options={PROFICIENCY_OPTIONS}
                      aria-label={`Proficiency for ${language.languageCode}`}
                    />
                  </li>
                ))}
              </ul>
            )}
          </>
        )}
      </FormSection>

      <FormSection title="About your experience">
        <FormField label="Country of birth" helperText="Shown to students as a trust signal — optional.">
          {(props) => (
            <Select
              id={props.id}
              value={countryOfBirthCode || undefined}
              onValueChange={setCountryOfBirthCode}
              options={COUNTRY_OPTIONS}
              placeholder="Select a country"
            />
          )}
        </FormField>

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
      </FormSection>
    </StepShell>
  );
}
