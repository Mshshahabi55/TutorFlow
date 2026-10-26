'use client';

import { useMemo, useState } from 'react';
import { Button } from '@/components/ui/Button';
import { ErrorState } from '@/components/ui/ErrorState';
import { FormField } from '@/components/ui/FormField';
import { FormSection } from '@/components/ui/FormSection';
import { Input } from '@/components/ui/Input';
import { LoadingState } from '@/components/ui/LoadingState';
import { Select } from '@/components/ui/Select';
import { Card } from '@/components/ui/Card';
import { DAY_OPTIONS, dayNameToNumber } from '@/features/availability/constants';
import { AvailabilityConfiguration, WeeklyRuleInput } from '@/features/availability/types';
import {
  useCurrentAvailabilityConfiguration,
  useReplaceWeeklyRules,
  useUpdateBookingSettings,
} from '@/features/availability/useAvailability';
import { TutorApplication, UpdateDraftCommand } from '../types';
import { StepShell } from './StepShell';
import styles from './AvailabilityStep.module.css';

export interface AvailabilityStepProps {
  application: TutorApplication;
  onContinue: (patch: UpdateDraftCommand) => void;
  onBack: () => void;
  isSaving: boolean;
  error: string | null;
}

const EMPTY_RULE: WeeklyRuleInput = { dayOfWeek: 1, startTime: '09:00', endTime: '17:00' };

/** Default weekly template: every day, 09:00–17:00 — matches Preply's own default-proposed
 * schedule, directly observed rather than assumed
 * (docs/product/preply-reverse-engineering/18-screenshot-evidence.md, SS11–SS12: "all 7 days of
 * the week follow the identical default-enabled, 09:00–17:00... pattern"). Used only for a
 * genuine first-time visit — see `AvailabilityEditor` below. */
const DEFAULT_RULES: WeeklyRuleInput[] = ['1', '2', '3', '4', '5', '6', '0'].map((dayOfWeek) => ({
  dayOfWeek: Number(dayOfWeek),
  startTime: '09:00',
  endTime: '17:00',
}));

/** Real integration with the existing `Availability` module (ADR-0026 §5), via the shared
 * `features/availability/` module — a tutor-facing availability-management page
 * (`/become-a-tutor/availability`, `TutorAvailabilityManager.tsx`) is the second real consumer,
 * so these calls were promoted out of this feature folder rather than duplicated.
 *
 * Phase 3 audit fix (silent-overwrite defect): this step now loads the tutor's authoritative
 * saved configuration via the same `useCurrentAvailabilityConfiguration` hook
 * `TutorAvailabilityManager` already uses, before rendering anything editable — mirroring that
 * component's established loading/error/hydration pattern rather than inventing a new one.
 * Replacing weekly rules on Continue is still a full replace (matches the backend contract
 * exactly, unchanged), but it now replaces the *loaded* rules the tutor is actually looking at,
 * not a hardcoded template that silently clobbers whatever they'd saved before. `DEFAULT_RULES`
 * (and the original empty-string/24h/30-day/60-minute booking-setting defaults) are now used
 * only when the loaded configuration confirms there is nothing saved yet — never merely because
 * loading hasn't finished or failed (see `AvailabilityStep`'s loading/error branches below, which
 * render no save control at all until a real configuration — empty or populated — is in hand).
 *
 * Saves go through `useReplaceWeeklyRules`/`useUpdateBookingSettings` (not the raw `api.ts`
 * functions) specifically so their existing `onSuccess` cache invalidation fires — the same
 * mutation hooks `TutorAvailabilityManager` already uses. Without it, navigating away from this
 * step and back (no full page reload) re-mounts this component against a React Query cache the
 * app's global `staleTime` (`app/providers.tsx`) still considers fresh, serving the pre-save
 * snapshot instead of refetching — a confirmed, live-reproduced regression
 * (`docs/product/preply-reverse-engineering/27-phase3-audit-completion-2026-10-25.md` §2.3). */
export function AvailabilityStep({ application, onContinue, onBack, isSaving, error }: AvailabilityStepProps) {
  const { data: config, isLoading, isError, refetch } = useCurrentAvailabilityConfiguration(true);

  if (isLoading) {
    return <LoadingState label="Loading your availability…" />;
  }

  if (isError || !config) {
    return (
      <ErrorState
        title="Couldn't load your availability"
        description="Something went wrong loading your current schedule. Your existing data, if any, is unaffected."
        onRetry={() => refetch()}
      />
    );
  }

  return (
    <AvailabilityEditor
      application={application}
      config={config}
      onContinue={onContinue}
      onBack={onBack}
      isSaving={isSaving}
      error={error}
    />
  );
}

interface AvailabilityEditorProps {
  application: TutorApplication;
  config: AvailabilityConfiguration;
  onContinue: (patch: UpdateDraftCommand) => void;
  onBack: () => void;
  isSaving: boolean;
  error: string | null;
}

/** Derives its editable state directly from the loaded `config` prop via `useState`'s
 * initializer — run once, on this component instance's first render, never resynced by an
 * effect. A background refetch of the parent query (e.g. on window refocus) changes `config`'s
 * object identity but does not remount this component or reset its state, so a tutor's in-
 * progress edits this visit are never clobbered by a late-arriving response — the same guarantee
 * `TutorAvailabilityManager`'s identical structure already relies on. */
function AvailabilityEditor({ application, config, onContinue, onBack, isSaving, error }: AvailabilityEditorProps) {
  const [rules, setRules] = useState<WeeklyRuleInput[]>(() =>
    config.weeklyRules.length > 0
      ? config.weeklyRules.map((r) => ({
          dayOfWeek: dayNameToNumber(r.dayOfWeek),
          startTime: r.startTime.slice(0, 5),
          endTime: r.endTime.slice(0, 5),
        }))
      : DEFAULT_RULES.map((rule) => ({ ...rule })),
  );
  const [timeZoneId, setTimeZoneId] = useState(config.bookingSettings.isConfigured ? config.bookingSettings.timeZoneId : '');
  const [minNoticeHours, setMinNoticeHours] = useState(
    String(config.bookingSettings.isConfigured ? config.bookingSettings.minNoticeHours : 24),
  );
  const [maxBookingWindowDays, setMaxBookingWindowDays] = useState(
    String(config.bookingSettings.isConfigured ? config.bookingSettings.maxBookingWindowDays : 30),
  );
  const [defaultSlotDurationMinutes, setDefaultSlotDurationMinutes] = useState(
    String(config.bookingSettings.isConfigured ? config.bookingSettings.defaultSlotDurationMinutes : 60),
  );
  const [localError, setLocalError] = useState<string | null>(null);

  const replaceRules = useReplaceWeeklyRules();
  const updateSettings = useUpdateBookingSettings();
  const isSubmittingAvailability = replaceRules.isPending || updateSettings.isPending;

  const timeZoneOptions = useMemo(() => {
    const zones = typeof Intl.supportedValuesOf === 'function' ? Intl.supportedValuesOf('timeZone') : ['UTC'];
    return zones.map((zone) => ({ value: zone, label: zone }));
  }, []);

  const handleContinue = async () => {
    setLocalError(null);
    if (!timeZoneId) {
      setLocalError('Select your timezone.');
      return;
    }

    try {
      await replaceRules.mutateAsync(rules);
      await updateSettings.mutateAsync({
        timeZoneId,
        minNoticeHours: Number(minNoticeHours),
        maxBookingWindowDays: Number(maxBookingWindowDays),
        defaultSlotDurationMinutes: Number(defaultSlotDurationMinutes),
      });
      onContinue({ availabilitySettingsConfigured: true, bookingSettingsConfigured: true });
    } catch {
      setLocalError("Couldn't save your availability. Check your weekly hours and try again.");
    }
  };

  return (
    <StepShell
      title="Availability & timezone"
      description="When you're generally open for lessons. You can refine this anytime after your profile is live."
      onBack={onBack}
      onContinue={handleContinue}
      isSaving={isSaving || isSubmittingAvailability}
      error={localError ?? error}
    >
      {application.availabilitySettingsConfigured && (
        <p className={styles.alreadyConfigured}>Availability already configured — saving again replaces it.</p>
      )}

      <FormSection title="Weekly hours">
        <div className={styles.rules}>
          {rules.map((rule, index) => (
            <Card key={index} className={styles.ruleRow}>
              <Select
                value={rule.dayOfWeek.toString()}
                onValueChange={(v) => setRules((prev) => prev.map((r, i) => (i === index ? { ...r, dayOfWeek: Number(v) } : r)))}
                options={DAY_OPTIONS}
                aria-label="Day of week"
              />
              <Input
                type="time"
                value={rule.startTime}
                onChange={(e) => setRules((prev) => prev.map((r, i) => (i === index ? { ...r, startTime: e.target.value } : r)))}
                aria-label="Start time"
              />
              <Input
                type="time"
                value={rule.endTime}
                onChange={(e) => setRules((prev) => prev.map((r, i) => (i === index ? { ...r, endTime: e.target.value } : r)))}
                aria-label="End time"
              />
              <Button type="button" variant="ghost" size="sm" onClick={() => setRules((prev) => prev.filter((_, i) => i !== index))}>
                Remove
              </Button>
            </Card>
          ))}
          <Button type="button" variant="secondary" size="sm" onClick={() => setRules((prev) => [...prev, { ...EMPTY_RULE }])}>
            Add a time block
          </Button>
        </div>
      </FormSection>

      <FormSection title="Booking settings">
        <FormField label="Timezone" required>
          {(props) => (
            <Select
              id={props.id}
              value={timeZoneId || undefined}
              onValueChange={setTimeZoneId}
              options={timeZoneOptions}
              placeholder="Select your timezone"
            />
          )}
        </FormField>

        <FormField label="Minimum notice (hours)" helperText="How far in advance a student must book.">
          {(props) => <Input {...props} type="number" min={0} value={minNoticeHours} onChange={(e) => setMinNoticeHours(e.target.value)} />}
        </FormField>

        <FormField label="Booking window (days)" helperText="How far in the future students can book.">
          {(props) => (
            <Input
              {...props}
              type="number"
              min={1}
              value={maxBookingWindowDays}
              onChange={(e) => setMaxBookingWindowDays(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Default lesson length (minutes)">
          {(props) => (
            <Input
              {...props}
              type="number"
              min={1}
              value={defaultSlotDurationMinutes}
              onChange={(e) => setDefaultSlotDurationMinutes(e.target.value)}
            />
          )}
        </FormField>
      </FormSection>
    </StepShell>
  );
}
