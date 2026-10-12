'use client';

import { useMemo, useState } from 'react';
import { Button } from '@/components/ui/Button';
import { FormField } from '@/components/ui/FormField';
import { FormSection } from '@/components/ui/FormSection';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Card } from '@/components/ui/Card';
import { Alert } from '@/components/ui/Alert';
import { LoadingState } from '@/components/ui/LoadingState';
import { ErrorState } from '@/components/ui/ErrorState';
import { DAY_OPTIONS, dayNameToNumber } from '../constants';
import { AvailabilityConfiguration, WeeklyRuleInput } from '../types';
import { useCurrentAvailabilityConfiguration, useReplaceWeeklyRules, useUpdateBookingSettings } from '../useAvailability';
import styles from './TutorAvailabilityManager.module.css';

const EMPTY_RULE: WeeklyRuleInput = { dayOfWeek: 1, startTime: '09:00', endTime: '17:00' };

/** The tutor-facing availability-management surface (independent audit, 2026-10-05: "a tutor
 * who completed onboarding months ago has no way to change their weekly schedule"). Reuses the
 * existing `Availability` module's real endpoints/domain logic — no second calendar
 * abstraction, no new module. Unlike the onboarding wizard's Availability step, this page's
 * entire job is viewing and editing an *already-saved* schedule, so it loads the current
 * configuration first and derives its editable state directly from that response (a child
 * component keyed by the loaded data, not an effect syncing state after the fact). */
export function TutorAvailabilityManager() {
  const { data, isLoading, isError, refetch } = useCurrentAvailabilityConfiguration(true);

  if (isLoading) {
    return <LoadingState />;
  }

  if (isError || !data) {
    return (
      <ErrorState
        title="Couldn't load your availability"
        description="Something went wrong loading your current schedule."
        onRetry={() => refetch()}
      />
    );
  }

  return <AvailabilityEditor config={data} />;
}

function AvailabilityEditor({ config }: { config: AvailabilityConfiguration }) {
  const [rules, setRules] = useState<WeeklyRuleInput[]>(() =>
    config.weeklyRules.length > 0
      ? config.weeklyRules.map((r) => ({
          dayOfWeek: dayNameToNumber(r.dayOfWeek),
          startTime: r.startTime.slice(0, 5),
          endTime: r.endTime.slice(0, 5),
        }))
      : [{ ...EMPTY_RULE }],
  );
  const [timeZoneId, setTimeZoneId] = useState(config.bookingSettings.timeZoneId);
  const [minNoticeHours, setMinNoticeHours] = useState(String(config.bookingSettings.minNoticeHours));
  const [maxBookingWindowDays, setMaxBookingWindowDays] = useState(String(config.bookingSettings.maxBookingWindowDays));
  const [defaultSlotDurationMinutes, setDefaultSlotDurationMinutes] = useState(
    String(config.bookingSettings.defaultSlotDurationMinutes),
  );
  const [justSaved, setJustSaved] = useState(false);

  const replaceRules = useReplaceWeeklyRules();
  const updateSettings = useUpdateBookingSettings();
  const isSaving = replaceRules.isPending || updateSettings.isPending;

  const timeZoneOptions = useMemo(() => {
    const zones = typeof Intl.supportedValuesOf === 'function' ? Intl.supportedValuesOf('timeZone') : ['UTC'];
    return zones.map((zone) => ({ value: zone, label: zone }));
  }, []);

  const handleSave = async () => {
    setJustSaved(false);
    try {
      await replaceRules.mutateAsync(rules);
      await updateSettings.mutateAsync({
        timeZoneId,
        minNoticeHours: Number(minNoticeHours),
        maxBookingWindowDays: Number(maxBookingWindowDays),
        defaultSlotDurationMinutes: Number(defaultSlotDurationMinutes),
      });
      setJustSaved(true);
    } catch {
      // Surfaced via replaceRules.isError / updateSettings.isError above — nothing further to do here.
    }
  };

  return (
    <div className={styles.page}>
      <h1 className={styles.title}>Availability</h1>
      <p className={styles.description}>Manage the hours you&apos;re open for lessons and your booking rules.</p>

      {justSaved && !isSaving && (
        <Alert variant="success" title="Saved">
          Your availability has been updated.
        </Alert>
      )}
      {(replaceRules.isError || updateSettings.isError) && (
        <Alert variant="error" title="Couldn't save your availability">
          Check your weekly hours and booking settings, then try again.
        </Alert>
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

      <div className={styles.footer}>
        <Button onClick={handleSave} loading={isSaving} disabled={!timeZoneId}>
          Save changes
        </Button>
      </div>
    </div>
  );
}
