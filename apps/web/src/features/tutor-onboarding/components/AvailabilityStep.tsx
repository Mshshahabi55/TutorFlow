'use client';

import { useMemo, useState } from 'react';
import { Button } from '@/components/ui/Button';
import { FormField } from '@/components/ui/FormField';
import { FormSection } from '@/components/ui/FormSection';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Card } from '@/components/ui/Card';
import { replaceWeeklyRules, updateBookingSettings } from '@/features/availability/api';
import { DAY_OPTIONS } from '@/features/availability/constants';
import { WeeklyRuleInput } from '@/features/availability/types';
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

/** Real integration with the existing `Availability` module (ADR-0026 §5), via the shared
 * `features/availability/` module — a tutor-facing availability-management page
 * (`/become-a-tutor/availability`) is the second real consumer, so these calls were promoted out
 * of this feature folder rather than duplicated. Replacing weekly rules is a full replace, not a
 * merge (matches the backend contract exactly) — this onboarding step still does not pre-load
 * any already-saved schedule from a prior visit (a deliberate, documented scope cut for *this*
 * step specifically, since the dedicated management page is where viewing/editing an existing
 * schedule actually belongs); `AvailabilitySettingsConfigured` already tells the wizard whether
 * this step is done. */
export function AvailabilityStep({ application, onContinue, onBack, isSaving, error }: AvailabilityStepProps) {
  const [rules, setRules] = useState<WeeklyRuleInput[]>([{ ...EMPTY_RULE }]);
  const [timeZoneId, setTimeZoneId] = useState('');
  const [minNoticeHours, setMinNoticeHours] = useState('24');
  const [maxBookingWindowDays, setMaxBookingWindowDays] = useState('30');
  const [defaultSlotDurationMinutes, setDefaultSlotDurationMinutes] = useState('60');
  const [localError, setLocalError] = useState<string | null>(null);
  const [isSubmittingAvailability, setIsSubmittingAvailability] = useState(false);

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

    setIsSubmittingAvailability(true);
    try {
      await replaceWeeklyRules(rules);
      await updateBookingSettings({
        timeZoneId,
        minNoticeHours: Number(minNoticeHours),
        maxBookingWindowDays: Number(maxBookingWindowDays),
        defaultSlotDurationMinutes: Number(defaultSlotDurationMinutes),
      });
      onContinue({ availabilitySettingsConfigured: true, bookingSettingsConfigured: true });
    } catch {
      setLocalError("Couldn't save your availability. Check your weekly hours and try again.");
    } finally {
      setIsSubmittingAvailability(false);
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
