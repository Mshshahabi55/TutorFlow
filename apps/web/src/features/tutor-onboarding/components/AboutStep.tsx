'use client';

import { useState } from 'react';
import { FormField } from '@/components/ui/FormField';
import { FormSection } from '@/components/ui/FormSection';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { MultiSelect } from '@/components/ui/MultiSelect';
import { Skeleton } from '@/components/ui/Skeleton';
import { useAuth } from '@/features/auth/useAuth';
import { useProfile, useUpdateProfile } from '@/features/auth/useProfile';
import { useLanguages, useSubjects } from '@/features/catalog/useCatalog';
import { COUNTRY_OPTIONS } from '@/features/search/constants';
import { LanguageProficiency, SpokenLanguageDraft, TutorApplication, UpdateDraftCommand } from '../types';
import { StepShell } from './StepShell';
import styles from './AboutStep.module.css';

export interface AboutStepProps {
  application: TutorApplication;
  onContinue: (patch: UpdateDraftCommand) => void;
  isSaving: boolean;
  error: string | null;
}

// CEFR scale, matching Preply's evidenced Level dropdown exactly
// (docs/product/preply-reverse-engineering/24-about-stage-live-evidence-2026-10-18.md §2 item 6).
const PROFICIENCY_OPTIONS: { value: LanguageProficiency; label: string }[] = [
  { value: 'A1', label: 'A1' },
  { value: 'A2', label: 'A2' },
  { value: 'B1', label: 'B1' },
  { value: 'B2', label: 'B2' },
  { value: 'C1', label: 'C1' },
  { value: 'C2', label: 'C2' },
  { value: 'Native', label: 'Native' },
];

/** Consolidates the Preply-evidenced "About" stage — a single step on the real site, split
 * across TutorFlow's prior Basics/Teaching steps — into one step: Display name, First/Last name
 * (Identity-owned, see ApplicationUser.cs), Email (read-only redisplay — Identity already knows
 * it, never re-collected here), Country of birth, Subject (single-select), Languages+CEFR
 * proficiency, Phone number (optional).
 * (docs/product/preply-reverse-engineering/24-about-stage-live-evidence-2026-10-18.md §2,
 * 26-about-stage-implementation-plan-2026-10-18.md §1/§4). Headline/About-you and
 * Specialties/Years-of-experience/Experience-summary deliberately stay on their existing steps —
 * doc 26 §1 scoped this consolidation to only the fields already evidenced on Preply's About
 * stage, not a full wizard redesign.
 *
 * Continue is never disabled by client-side field state (ADR-0026 §3 addendum) — matching
 * Preply's own evidenced About stage, which the account holder confirmed never disables its
 * Continue button either (24-...md §2, USER-CONFIRMED). The backend's
 * `GetSubmissionReadinessErrors()` required-field set is unchanged and remains the real gate;
 * a missing field only ever surfaces as a real backend error at submission time (PricingStep.tsx,
 * the final step) — there is no dedicated review/checklist screen (16-product-decisions-and-
 * open-questions.md item 11, resolved by removing Review to match Preply's real flow exactly). */
export function AboutStep({ application, onContinue, isSaving, error }: AboutStepProps) {
  const { email } = useAuth();
  const { data: profile, isLoading: profileLoading } = useProfile(true);
  const updateProfile = useUpdateProfile();

  const [displayName, setDisplayName] = useState(application.displayName ?? '');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [profileHydrated, setProfileHydrated] = useState(false);
  const [countryOfBirthCode, setCountryOfBirthCode] = useState(application.countryOfBirthCode ?? '');
  const [subjectIds, setSubjectIds] = useState<string[]>(application.subjectIds);
  const [languages, setLanguages] = useState<SpokenLanguageDraft[]>(application.spokenLanguages);
  const [phoneNumber, setPhoneNumber] = useState(application.phoneNumber ?? '');
  const [profileError, setProfileError] = useState<string | null>(null);

  // useProfile loads asynchronously after mount; hydrate the local first/last name fields once,
  // the same pattern every other step uses for its own application-backed initial state.
  if (profile && !profileHydrated) {
    setFirstName(profile.firstName ?? '');
    setLastName(profile.lastName ?? '');
    setProfileHydrated(true);
  }

  const { data: subjects, isLoading: subjectsLoading } = useSubjects();
  const { data: languageCatalog, isLoading: languagesLoading } = useLanguages();

  const subjectOptions = (subjects ?? []).map((s) => ({ value: s.id, label: s.displayName }));
  const languageOptions = (languageCatalog ?? []).map((l) => ({ value: l.code, label: l.displayName }));

  const setLanguageCodes = (codes: string[]) => {
    setLanguages((prev) => [
      ...codes.map((code) => prev.find((l) => l.languageCode === code) ?? { languageCode: code, proficiency: 'Native' as LanguageProficiency }),
    ]);
  };

  const setProficiency = (code: string, proficiency: LanguageProficiency) => {
    setLanguages((prev) => prev.map((l) => (l.languageCode === code ? { ...l, proficiency } : l)));
  };

  const handleContinue = async () => {
    setProfileError(null);
    const nameChanged = firstName !== (profile?.firstName ?? '') || lastName !== (profile?.lastName ?? '');
    if (nameChanged) {
      try {
        await updateProfile.mutateAsync({ firstName, lastName });
      } catch {
        setProfileError("Couldn't save your name. Please try again.");
        return;
      }
    }

    onContinue({
      displayName,
      countryOfBirthCode: countryOfBirthCode || undefined,
      subjectIds,
      spokenLanguages: languages,
      phoneNumber: phoneNumber || undefined,
    });
  };

  return (
    <StepShell
      title="About"
      description="Start creating your public tutor profile."
      onContinue={handleContinue}
      isSaving={isSaving || updateProfile.isPending}
      error={error ?? profileError}
    >
      <FormField label="Display name" required>
        {(props) => <Input {...props} value={displayName} onChange={(e) => setDisplayName(e.target.value)} maxLength={200} />}
      </FormField>

      {profileLoading ? (
        <Skeleton width="100%" height="2.75rem" />
      ) : (
        <>
          <FormField label="First name">
            {(props) => <Input {...props} value={firstName} onChange={(e) => setFirstName(e.target.value)} maxLength={200} />}
          </FormField>
          <FormField label="Last name">
            {(props) => <Input {...props} value={lastName} onChange={(e) => setLastName(e.target.value)} maxLength={200} />}
          </FormField>
        </>
      )}

      <FormField label="Email" helperText="From your account — change it from account settings.">
        {(props) => <Input {...props} value={email ?? ''} disabled readOnly />}
      </FormField>

      <FormField label="Country of birth">
        {(props) => (
          <Select
            id={props.id}
            value={countryOfBirthCode}
            onValueChange={setCountryOfBirthCode}
            options={COUNTRY_OPTIONS}
            placeholder="Choose country…"
          />
        )}
      </FormField>

      <FormSection title="Subject you teach">
        {subjectsLoading ? (
          <Skeleton width="100%" height="2.75rem" />
        ) : (
          <Select
            aria-label="Subject you teach"
            placeholder="Choose subject…"
            options={subjectOptions}
            value={subjectIds[0]}
            onValueChange={(value) => setSubjectIds(value ? [value] : [])}
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
              placeholder="Language…"
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

      <FormField label="Phone number" helperText="Optional.">
        {(props) => <Input {...props} type="tel" value={phoneNumber} onChange={(e) => setPhoneNumber(e.target.value)} maxLength={32} />}
      </FormField>
    </StepShell>
  );
}
