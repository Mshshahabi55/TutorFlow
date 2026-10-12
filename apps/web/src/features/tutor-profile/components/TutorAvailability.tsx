'use client';

import { useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorState } from '@/components/ui/ErrorState';
import { EmptyState } from '@/components/ui/EmptyState';
import { Button } from '@/components/ui/Button';
import { TutorSlotDto } from '../api';
import { TrialDurationMinutes } from '../types';
import { useAvailabilityPreview, useTrialEligibility, useTutorSlots } from '../useTutorProfile';
import { ProfileSection } from './ProfileSection';
import { TrialDurationSelector } from './TrialDurationSelector';
import { AvailabilityCalendar, CalendarDate } from './AvailabilityCalendar';
import { TimeSlotGrid } from './TimeSlotGrid';
import { SlotSelectionSummary } from './SlotSelectionSummary';
import styles from './TutorAvailability.module.css';

export interface TutorAvailabilityProps {
  tutorUserId: string;
  tutorSlug: string;
  isAcceptingNewStudents: boolean;
  isAuthenticatedStudent: boolean;
  onRequireAuth: (action: string) => void;
}

const WEEK_MS = 7 * 24 * 60 * 60 * 1000;

function localDateKey(date: Date): string {
  return date.toDateString();
}

function formatDayLabel(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' });
}

/**
 * Availability only tells the user what is available — it never creates a hold itself (that
 * remains Bookings' job, see ADR-0013/ADR-0025). Guests see a coarse preview via the anonymous
 * endpoint; authenticated, trial-eligible Students see the real, duration- and date-aware picker
 * via the full endpoint. Selecting a slot and continuing hands off to the honest /bookings/new
 * boundary (ADR-0025) — this component's scope ends at "what's available, and what did the
 * student pick," not "create a booking."
 */
export function TutorAvailability({
  tutorUserId,
  tutorSlug,
  isAcceptingNewStudents,
  isAuthenticatedStudent,
  onRequireAuth,
}: TutorAvailabilityProps) {
  const router = useRouter();
  const preview = useAvailabilityPreview(tutorUserId);
  const trialEligibility = useTrialEligibility(tutorUserId, isAuthenticatedStudent && isAcceptingNewStudents);

  // Anchored once per mount rather than re-read on every render — Date.now() is an impure call
  // and React's render purity rules disallow calling it directly in render/useMemo. A lazy
  // useState initializer is the sanctioned one-time-impure-read pattern for this.
  const [anchorNowMs] = useState(() => Date.now());
  const [duration, setDuration] = useState<TrialDurationMinutes>(50);
  const [weekOffset, setWeekOffset] = useState(0);
  const [selectedDateKey, setSelectedDateKey] = useState<string | null>(null);
  const [selectedSlot, setSelectedSlot] = useState<TutorSlotDto | null>(null);

  const isEligible = isAuthenticatedStudent && trialEligibility.data?.hasUsedTrial === false;

  const { fromUtc, toUtc } = useMemo(() => {
    const from = new Date(anchorNowMs + weekOffset * WEEK_MS);
    return { fromUtc: from.toISOString(), toUtc: new Date(from.getTime() + WEEK_MS).toISOString() };
  }, [anchorNowMs, weekOffset]);

  const fullSlots = useTutorSlots(tutorUserId, { durationMinutes: duration, fromUtc, toUtc }, isEligible);

  const groupedSlots = useMemo(() => {
    const map = new Map<string, TutorSlotDto[]>();
    for (const slot of fullSlots.data?.slots ?? []) {
      const key = localDateKey(new Date(slot.startUtc));
      const list = map.get(key) ?? [];
      list.push(slot);
      map.set(key, list);
    }
    return map;
  }, [fullSlots.data]);

  const calendarDates: CalendarDate[] = useMemo(() => {
    const from = new Date(fromUtc);
    return Array.from({ length: 7 }, (_, i) => {
      const date = new Date(from.getTime() + i * 24 * 60 * 60 * 1000);
      const key = localDateKey(date);
      return { key, date, hasAvailability: (groupedSlots.get(key)?.length ?? 0) > 0 };
    });
  }, [fromUtc, groupedSlots]);

  // The fetched week/duration can change the selected date/slot's validity — rather than
  // reconciling with an effect+setState (which cascades an extra render), the "effective"
  // selection is derived directly from the latest data on every render. A selection the backend
  // no longer confirms is open is never shown as selected.
  const effectiveDateKey = useMemo(() => {
    if (selectedDateKey && (groupedSlots.get(selectedDateKey)?.length ?? 0) > 0) {
      return selectedDateKey;
    }
    return calendarDates.find((d) => d.hasAvailability)?.key ?? null;
  }, [selectedDateKey, groupedSlots, calendarDates]);

  const effectiveSelectedSlot = useMemo(() => {
    if (!selectedSlot) {
      return null;
    }
    const stillOffered = (fullSlots.data?.slots ?? []).some((s) => s.startUtc === selectedSlot.startUtc);
    return stillOffered ? selectedSlot : null;
  }, [selectedSlot, fullSlots.data]);

  const timesForSelectedDate = effectiveDateKey ? (groupedSlots.get(effectiveDateKey) ?? []) : [];
  const canGoNext = fullSlots.data
    ? toUtc < new Date(anchorNowMs + fullSlots.data.maxBookingWindowDays * 24 * 60 * 60 * 1000).toISOString()
    : true;

  const handleContinue = () => {
    if (!effectiveSelectedSlot) {
      return;
    }
    const params = new URLSearchParams({
      tutorUserId,
      tutorSlug,
      durationMinutes: String(duration),
      startUtc: effectiveSelectedSlot.startUtc,
      endUtc: effectiveSelectedSlot.endUtc,
    });
    router.push(`/bookings/new?${params.toString()}`);
  };

  if (!isAcceptingNewStudents) {
    return (
      <ProfileSection title="Availability">
        <p className={styles.notAccepting}>This tutor isn&apos;t currently accepting new students.</p>
      </ProfileSection>
    );
  }

  return (
    <ProfileSection title="Availability">
      <div id="tutor-availability">
        {preview.isLoading && <Skeleton width="70%" />}
        {preview.isError && (
          <ErrorState
            title="Couldn't load availability"
            description="We couldn't check this tutor's availability right now."
          />
        )}
        {preview.data && (
          <p className={styles.disclosure}>
            Times are shown in your local timezone.
            {preview.data.timeZoneId ? ` This tutor is based in ${preview.data.timeZoneId}.` : ''}
          </p>
        )}

        {preview.data && !preview.data.hasUpcomingAvailability && (
          <EmptyState
            title="No upcoming availability"
            description="This tutor doesn't have any open slots right now. Check back soon, or message them to ask about upcoming openings."
          />
        )}

        {preview.data?.hasUpcomingAvailability && !isAuthenticatedStudent && (
          <div className={styles.previewRow}>
            <p>
              Next available{' '}
              {preview.data.nextAvailableStartUtc ? formatDayLabel(preview.data.nextAvailableStartUtc) : 'soon'}.
            </p>
            <Button variant="secondary" size="sm" onClick={() => onRequireAuth('see exact available times')}>
              Log in to see times
            </Button>
          </div>
        )}

        {preview.data?.hasUpcomingAvailability && isAuthenticatedStudent && trialEligibility.isLoading && (
          <Skeleton width="100%" height="2rem" />
        )}

        {preview.data?.hasUpcomingAvailability && isAuthenticatedStudent && trialEligibility.data?.hasUsedTrial && (
          <p className={styles.notAccepting}>
            You&apos;ve already had a trial lesson with this tutor. Message them to arrange your next lesson.
          </p>
        )}

        {preview.data?.hasUpcomingAvailability && isEligible && (
          <>
            <TrialDurationSelector value={duration} onChange={setDuration} disabled={fullSlots.isLoading} />

            {fullSlots.isLoading && <Skeleton width="100%" height="6rem" />}
            {fullSlots.isError && (
              <ErrorState
                title="Couldn't load time slots"
                description="We couldn't load this tutor's open slots right now."
                onRetry={() => fullSlots.refetch()}
              />
            )}

            {fullSlots.data && (
              <>
                <AvailabilityCalendar
                  dates={calendarDates}
                  selectedDateKey={effectiveDateKey}
                  onSelectDate={setSelectedDateKey}
                  onPrevWeek={() => setWeekOffset((w) => w - 1)}
                  onNextWeek={() => setWeekOffset((w) => w + 1)}
                  canGoPrev={weekOffset > 0}
                  canGoNext={canGoNext}
                />

                <TimeSlotGrid
                  slots={timesForSelectedDate}
                  selectedStartUtc={effectiveSelectedSlot?.startUtc ?? null}
                  onSelect={setSelectedSlot}
                />

                {effectiveSelectedSlot && (
                  <SlotSelectionSummary startUtc={effectiveSelectedSlot.startUtc} endUtc={effectiveSelectedSlot.endUtc} />
                )}

                <div className={styles.continueRow}>
                  <Button disabled={!effectiveSelectedSlot} onClick={handleContinue}>
                    Continue
                  </Button>
                </div>
              </>
            )}
          </>
        )}
      </div>
    </ProfileSection>
  );
}
