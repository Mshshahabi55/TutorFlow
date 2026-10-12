'use client';

import styles from './FiltersPanel.module.css';
import { FormField } from '@/components/ui/FormField';
import { FormSection } from '@/components/ui/FormSection';
import { Select } from '@/components/ui/Select';
import { MultiSelect } from '@/components/ui/MultiSelect';
import { Input } from '@/components/ui/Input';
import { Switch } from '@/components/ui/Switch';
import { Skeleton } from '@/components/ui/Skeleton';
import { COUNTRY_OPTIONS, RATING_OPTIONS } from '../constants';
import { SearchFilters } from '../types';
import { useLanguages, useSpecialties, useSubjects } from '@/features/catalog/useCatalog';

export interface FiltersPanelProps {
  value: SearchFilters;
  onChange: (partial: Partial<SearchFilters>) => void;
}

const COUNTRY_SELECT_OPTIONS = [{ value: '', label: 'Any country' }, ...COUNTRY_OPTIONS];

const RATING_SELECT_OPTIONS = [{ value: '', label: 'Any rating' }, ...RATING_OPTIONS];

export function FiltersPanel({ value, onChange }: FiltersPanelProps) {
  const { data: subjects, isLoading: subjectsLoading } = useSubjects();
  // Unscoped (no subjectId) when none is selected yet — the backend already supports this,
  // and showing every specialty up front is more useful than an empty/disabled control.
  const { data: specialties, isLoading: specialtiesLoading } = useSpecialties(value.subjectId || undefined);
  const { data: languages, isLoading: languagesLoading } = useLanguages();

  const subjectSelectOptions = [
    { value: '', label: 'Any subject' },
    ...(subjects ?? []).map((s) => ({ value: s.id, label: s.displayName })),
  ];
  const specialtyMultiOptions = (specialties ?? []).map((s) => ({ value: s.id, label: s.displayName }));
  const languageMultiOptions = (languages ?? []).map((l) => ({ value: l.code, label: l.displayName }));
  const nativeSpeakerOptions = [
    { value: '', label: 'Any language' },
    ...(languages ?? []).map((l) => ({ value: l.code, label: l.displayName })),
  ];

  return (
    <div className={styles.panel}>
      <FormSection title="Subject">
        {subjectsLoading ? (
          <Skeleton width="100%" height="2.75rem" />
        ) : (
          <FormField label="Subject" hideLabel>
            {(props) => (
              <Select
                id={props.id}
                value={value.subjectId || undefined}
                onValueChange={(v) => onChange({ subjectId: v, specialtyIds: [] })}
                options={subjectSelectOptions}
                placeholder="Any subject"
              />
            )}
          </FormField>
        )}
      </FormSection>

      <FormSection title="Specialties">
        {specialtiesLoading ? (
          <Skeleton width="100%" height="2.75rem" />
        ) : (
          <MultiSelect
            aria-label="Specialties"
            placeholder="Any specialty"
            options={specialtyMultiOptions}
            selected={value.specialtyIds}
            onChange={(specialtyIds) => onChange({ specialtyIds })}
          />
        )}
      </FormSection>

      <FormSection title="Languages">
        {languagesLoading ? (
          <Skeleton width="100%" height="2.75rem" />
        ) : (
          <>
            <MultiSelect
              aria-label="Languages spoken"
              placeholder="Any language"
              options={languageMultiOptions}
              selected={value.languages}
              onChange={(languages) => onChange({ languages })}
            />
            <FormField label="Native speaker of" hideLabel>
              {(props) => (
                <Select
                  id={props.id}
                  value={value.nativeSpeakerOf || undefined}
                  onValueChange={(v) => onChange({ nativeSpeakerOf: v })}
                  options={nativeSpeakerOptions}
                  placeholder="Native speaker — any language"
                />
              )}
            </FormField>
          </>
        )}
      </FormSection>

      <FormSection title="Country of birth">
        <FormField label="Country of birth" hideLabel>
          {(props) => (
            <Select
              id={props.id}
              value={value.countryOfBirthCode || undefined}
              onValueChange={(v) => onChange({ countryOfBirthCode: v })}
              options={COUNTRY_SELECT_OPTIONS}
              placeholder="Any country"
            />
          )}
        </FormField>
      </FormSection>

      <FormSection title="Price per hour (EUR)">
        <div className={styles.priceRow}>
          <FormField label="Minimum price" hideLabel>
            {(props) => (
              <Input
                {...props}
                type="number"
                min={0}
                inputMode="decimal"
                placeholder="Min"
                value={value.priceMinMinorUnits === null ? '' : String(value.priceMinMinorUnits / 100)}
                onChange={(e) => {
                  const n = Number.parseFloat(e.target.value);
                  onChange({ priceMinMinorUnits: Number.isFinite(n) ? Math.round(n * 100) : null });
                }}
              />
            )}
          </FormField>
          <span className={styles.priceSeparator} aria-hidden="true">
            –
          </span>
          <FormField label="Maximum price" hideLabel>
            {(props) => (
              <Input
                {...props}
                type="number"
                min={0}
                inputMode="decimal"
                placeholder="Max"
                value={value.priceMaxMinorUnits === null ? '' : String(value.priceMaxMinorUnits / 100)}
                onChange={(e) => {
                  const n = Number.parseFloat(e.target.value);
                  onChange({ priceMaxMinorUnits: Number.isFinite(n) ? Math.round(n * 100) : null });
                }}
              />
            )}
          </FormField>
        </div>
      </FormSection>

      <FormSection title="Rating">
        <FormField label="Minimum rating" hideLabel>
          {(props) => (
            <Select
              id={props.id}
              value={value.minRating !== null ? String(value.minRating) : undefined}
              onValueChange={(v) => onChange({ minRating: v ? Number.parseFloat(v) : null })}
              options={RATING_SELECT_OPTIONS}
              placeholder="Any rating"
            />
          )}
        </FormField>
      </FormSection>

      <FormSection title="Tutor badges">
        <Switch
          label="Professional tutor"
          checked={value.professionalTutor}
          onCheckedChange={(professionalTutor) => onChange({ professionalTutor })}
        />
        <Switch
          label="Super Tutor"
          checked={value.superTutor}
          onCheckedChange={(superTutor) => onChange({ superTutor })}
        />
      </FormSection>

      <FormSection title="Availability">
        <Switch
          label="Available soon"
          checked={value.availableSoon}
          onCheckedChange={(availableSoon) => onChange({ availableSoon })}
        />
      </FormSection>
    </div>
  );
}
