'use client';

import { ButtonLink } from '@/components/ui/ButtonLink';
import { Button } from '@/components/ui/Button';
import { Tooltip } from '@/components/ui/Tooltip';
import { useToast } from '@/components/ui/Toast';
import { useAuth } from '@/features/auth/useAuth';
import { useAvailabilityPreview, useFavoriteMutations, useFavoriteStatus, useTrialEligibility } from '../useTutorProfile';
import { TutorPublicProfile } from '../types';
import styles from './BookingPanel.module.css';

function formatPrice(minorUnits: number, currency: string): string {
  return new Intl.NumberFormat('en-IE', { style: 'currency', currency, maximumFractionDigits: 0 }).format(
    minorUnits / 100,
  );
}

export interface BookingPanelProps {
  profile: TutorPublicProfile;
  onRequireAuth: (action: string) => void;
}

/**
 * Composes three independently-owned signals into the final CTA state
 * (PREPLY_TUTOR_PROFILE_PARITY_SPEC.md §6, ADR-0022): Tutors' `isAcceptingNewStudents`,
 * Availability's upcoming-slot preview, and Bookings' trial-eligibility read. Never creates a
 * booking or a hold itself — "Book a trial lesson" only ever hands off to the availability
 * picker (`#tutor-availability`) or the not-yet-built booking flow.
 */
export function BookingPanel({ profile, onRequireAuth }: BookingPanelProps) {
  const { isAuthenticated, roles } = useAuth();
  const isStudent = isAuthenticated && roles.includes('Student');
  const isTutorViewer = isAuthenticated && roles.includes('Tutor');
  const { showToast } = useToast();

  const availability = useAvailabilityPreview(profile.tutorUserId);
  const favoriteStatus = useFavoriteStatus(profile.tutorUserId, isStudent);
  const trialEligibility = useTrialEligibility(profile.tutorUserId, isStudent);
  const { add, remove } = useFavoriteMutations(profile.tutorUserId);

  const priceDisplay = (
    <div className={styles.price}>
      {formatPrice(profile.hourlyRateAmountMinorUnits, profile.hourlyRateCurrency)}
      <span className={styles.priceUnit}> / hour</span>
    </div>
  );

  // A pure Tutor (no Student capability) isn't a booking actor — TutorFlow has no concept of a
  // tutor booking another tutor. A dual-role account (ADR-0002: Student + Tutor on one account)
  // keeps full Student CTAs here; Tutor-ness alone must never suppress them.
  if (isTutorViewer && !isStudent) {
    return <div className={styles.panel}>{priceDisplay}</div>;
  }

  const handleFavoriteClick = () => {
    if (!isStudent) {
      onRequireAuth('favorite this tutor');
      return;
    }
    if (favoriteStatus.data?.isFavorited) {
      remove.mutate(undefined, { onSuccess: () => showToast('Removed from favorites') });
    } else {
      add.mutate(undefined, { onSuccess: () => showToast('Added to favorites') });
    }
  };

  const trialKind = !isStudent
    ? 'guest'
    : !profile.isAcceptingNewStudents
      ? 'blocked-accepting'
      : availability.isLoading || trialEligibility.isLoading
        ? 'loading'
        : availability.data && !availability.data.hasUpcomingAvailability
          ? 'blocked-availability'
          : trialEligibility.data?.hasUsedTrial
            ? 'used-trial'
            : 'ready';

  const trialLabel =
    trialKind === 'blocked-accepting'
      ? "Not accepting new students"
      : trialKind === 'blocked-availability'
        ? 'No availability right now'
        : trialKind === 'used-trial'
          ? 'Schedule a lesson'
          : 'Book a trial lesson';

  return (
    <div className={styles.panel}>
      {priceDisplay}

      {trialKind === 'ready' || trialKind === 'guest' ? (
        <ButtonLink
          href="#tutor-availability"
          fullWidth
          onClick={(event) => {
            if (!isStudent) {
              event.preventDefault();
              onRequireAuth('book a trial lesson');
            }
          }}
        >
          {trialLabel}
        </ButtonLink>
      ) : trialKind === 'used-trial' ? (
        <Tooltip content="Regular lesson scheduling isn't available yet — message this tutor to arrange your next lesson.">
          <span>
            <Button fullWidth disabled>
              {trialLabel}
            </Button>
          </span>
        </Tooltip>
      ) : (
        <Button fullWidth disabled aria-busy={trialKind === 'loading'}>
          {trialLabel}
        </Button>
      )}

      <div className={styles.secondaryActions}>
        {isStudent ? (
          <ButtonLink href="/messages" variant="secondary" fullWidth>
            Message
          </ButtonLink>
        ) : (
          <Button variant="secondary" fullWidth onClick={() => onRequireAuth('message this tutor')}>
            Message
          </Button>
        )}

        <Button
          variant="ghost"
          onClick={handleFavoriteClick}
          aria-pressed={isStudent ? Boolean(favoriteStatus.data?.isFavorited) : undefined}
          aria-label={isStudent && favoriteStatus.data?.isFavorited ? 'Remove from favorites' : 'Add to favorites'}
        >
          {isStudent && favoriteStatus.data?.isFavorited ? '♥' : '♡'}
        </Button>
      </div>
    </div>
  );
}
