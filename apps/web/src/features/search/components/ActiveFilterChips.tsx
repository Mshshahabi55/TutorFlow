'use client';

import styles from './ActiveFilterChips.module.css';
import { Chip } from '@/components/ui/Chip';
import { SearchFilters } from '../types';
import { COUNTRY_OPTIONS } from '../constants';
import { useLanguages, useSpecialties, useSubjects } from '@/features/catalog/useCatalog';

export interface ActiveFilterChipsProps {
  filters: SearchFilters;
  onChange: (partial: Partial<SearchFilters>) => void;
  onClearAll: () => void;
}

function countryLabel(code: string): string {
  return COUNTRY_OPTIONS.find((o) => o.value === code)?.label ?? code;
}

export function ActiveFilterChips({ filters, onChange, onClearAll }: ActiveFilterChipsProps) {
  const { data: subjects } = useSubjects();
  const { data: specialties } = useSpecialties();
  const { data: languages } = useLanguages();

  const subjectLabel = (id: string) => subjects?.find((s) => s.id === id)?.displayName ?? id;
  const specialtyLabel = (id: string) => specialties?.find((s) => s.id === id)?.displayName ?? id;
  const languageLabel = (code: string) => languages?.find((l) => l.code === code)?.displayName ?? code;

  const chips: { key: string; label: string; onRemove: () => void }[] = [];

  if (filters.subjectId) {
    chips.push({ key: 'subject', label: subjectLabel(filters.subjectId), onRemove: () => onChange({ subjectId: '' }) });
  }
  for (const s of filters.specialtyIds) {
    chips.push({
      key: `specialty-${s}`,
      label: specialtyLabel(s),
      onRemove: () => onChange({ specialtyIds: filters.specialtyIds.filter((v) => v !== s) }),
    });
  }
  for (const l of filters.languages) {
    chips.push({
      key: `language-${l}`,
      label: `Speaks ${languageLabel(l)}`,
      onRemove: () => onChange({ languages: filters.languages.filter((v) => v !== l) }),
    });
  }
  if (filters.nativeSpeakerOf) {
    chips.push({
      key: 'native',
      label: `Native ${languageLabel(filters.nativeSpeakerOf)}`,
      onRemove: () => onChange({ nativeSpeakerOf: '' }),
    });
  }
  if (filters.countryOfBirthCode) {
    chips.push({
      key: 'country',
      label: `Born in ${countryLabel(filters.countryOfBirthCode)}`,
      onRemove: () => onChange({ countryOfBirthCode: '' }),
    });
  }
  if (filters.professionalTutor) {
    chips.push({ key: 'professional', label: 'Professional tutor', onRemove: () => onChange({ professionalTutor: false }) });
  }
  if (filters.superTutor) {
    chips.push({ key: 'super', label: 'Super Tutor', onRemove: () => onChange({ superTutor: false }) });
  }
  if (filters.priceMinMinorUnits !== null || filters.priceMaxMinorUnits !== null) {
    const min = filters.priceMinMinorUnits !== null ? `€${filters.priceMinMinorUnits / 100}` : '€0';
    const max = filters.priceMaxMinorUnits !== null ? `€${filters.priceMaxMinorUnits / 100}` : '+';
    chips.push({
      key: 'price',
      label: `${min} – ${max}`,
      onRemove: () => onChange({ priceMinMinorUnits: null, priceMaxMinorUnits: null }),
    });
  }
  if (filters.minRating !== null) {
    chips.push({
      key: 'rating',
      label: `${filters.minRating.toFixed(1)}+ rating`,
      onRemove: () => onChange({ minRating: null }),
    });
  }
  if (filters.availableSoon) {
    chips.push({ key: 'available', label: 'Available soon', onRemove: () => onChange({ availableSoon: false }) });
  }

  if (chips.length === 0) {
    return null;
  }

  return (
    <div className={styles.row} role="group" aria-label="Active filters">
      {chips.map((chip) => (
        <Chip key={chip.key} onRemove={chip.onRemove} removeLabel={`Remove filter: ${chip.label}`}>
          {chip.label}
        </Chip>
      ))}
      <button type="button" className={styles.clearAll} onClick={onClearAll}>
        Clear all
      </button>
    </div>
  );
}
