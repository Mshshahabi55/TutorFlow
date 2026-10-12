import type { MouseEvent } from 'react';
import styles from './TutorResultCard.module.css';
import { Card } from '@/components/ui/Card';
import { Avatar } from '@/components/ui/Avatar';
import { Rating } from '@/components/ui/Rating';
import { Chip } from '@/components/ui/Chip';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { ButtonLink } from '@/components/ui/ButtonLink';
import { Tooltip } from '@/components/ui/Tooltip';
import { TutorSearchResult } from '../types';
import { useSpecialties, useSubjects } from '@/features/catalog/useCatalog';
import { TutorTrustBadges } from '@/features/tutor-profile/components/TutorTrustBadges';

function formatPrice(minorUnits: number, currency: string): string {
  return new Intl.NumberFormat('en-IE', { style: 'currency', currency, maximumFractionDigits: 0 }).format(
    minorUnits / 100,
  );
}

export interface TutorResultCardProps {
  tutor: TutorSearchResult;
  /** Authenticated with Student capability — dual-role safe (ADR-0002): a Tutor who is also a
   * Student still sees these actions. */
  isStudent: boolean;
  /** Authenticated, Tutor capability, no Student capability — the only case these actions are
   * hidden rather than auth-gated, mirroring BookingPanel's identical rule. */
  isTutorOnlyViewer: boolean;
  isFavorited: boolean;
  onToggleFavorite: () => void;
  onRequireAuth: (action: string) => void;
}

export function TutorResultCard({
  tutor,
  isStudent,
  isTutorOnlyViewer,
  isFavorited,
  onToggleFavorite,
  onRequireAuth,
}: TutorResultCardProps) {
  const { data: subjects } = useSubjects();
  const { data: specialties } = useSpecialties();
  const nativeLanguages = tutor.spokenLanguages.filter((l) => l.proficiency === 'Native');

  const subjectNames = tutor.subjectIds.map((id) => subjects?.find((s) => s.id === id)?.displayName).filter(Boolean);
  const specialtyNames = tutor.specialtyIds
    .map((id) => specialties?.find((s) => s.id === id)?.displayName)
    .filter(Boolean);

  const handleFavoriteClick = () => {
    if (!isStudent) {
      onRequireAuth('favorite this tutor');
      return;
    }
    onToggleFavorite();
  };

  const handleBookTrialClick = (event: MouseEvent<HTMLAnchorElement>) => {
    if (!isStudent) {
      event.preventDefault();
      onRequireAuth('book a trial lesson');
    }
  };

  return (
    <Card interactive>
      <article className={styles.card}>
        <div className={styles.header}>
          <Avatar src={tutor.photoUrl} name={tutor.displayName} size="lg" />
          <div className={styles.identity}>
            <h3 className={styles.name}>{tutor.displayName}</h3>
            <p className={styles.headline}>{tutor.headline}</p>
            <Rating value={tutor.averageRating} reviewCount={tutor.reviewCount} />
          </div>
        </div>

        <div className={styles.tags}>
          <TutorTrustBadges isSuperTutor={tutor.isSuperTutor} isProfessionalTutor={tutor.isProfessionalTutor} />
          {subjectNames.slice(0, 1).map((s) => (
            <Chip key={s}>{s}</Chip>
          ))}
          {specialtyNames.slice(0, 2).map((s) => (
            <Chip key={s} variant="neutral">
              {s}
            </Chip>
          ))}
          {nativeLanguages.map((l) => (
            <Badge key={l.languageCode} variant="success">
              Native {l.languageCode.toUpperCase()}
            </Badge>
          ))}
          {tutor.hasUpcomingAvailability && (
            <Tooltip content="This tutor has reported open slots recently — exact times are shown on their profile.">
              <span>
                <Badge variant="neutral">Available soon</Badge>
              </span>
            </Tooltip>
          )}
        </div>

        <p className={styles.stats}>
          {tutor.lessonCount} lesson{tutor.lessonCount === 1 ? '' : 's'} taught
        </p>

        <div className={styles.footer}>
          <span className={styles.price}>
            {formatPrice(tutor.hourlyRateMinorUnits, tutor.currency)}
            <span className={styles.priceUnit}> / hour</span>
          </span>
          <ButtonLink href={`/tutors/${tutor.slug}`} variant="secondary" size="sm">
            View profile
          </ButtonLink>
        </div>

        {!isTutorOnlyViewer && (
          <div className={styles.actions}>
            {!tutor.isAcceptingNewStudents ? (
              <Tooltip content="This tutor isn't accepting new students right now.">
                <span className={styles.notAccepting}>
                  <Button size="sm" fullWidth disabled>
                    Not accepting new students
                  </Button>
                </span>
              </Tooltip>
            ) : (
              <ButtonLink
                href={`/tutors/${tutor.slug}#tutor-availability`}
                size="sm"
                fullWidth
                onClick={handleBookTrialClick}
              >
                Book a trial lesson
              </ButtonLink>
            )}

            {isStudent ? (
              <ButtonLink href="/messages" variant="secondary" size="sm">
                Message
              </ButtonLink>
            ) : (
              <Button variant="secondary" size="sm" onClick={() => onRequireAuth('message this tutor')}>
                Message
              </Button>
            )}

            <Button
              variant="ghost"
              size="sm"
              onClick={handleFavoriteClick}
              aria-pressed={isStudent ? isFavorited : undefined}
              aria-label={isStudent && isFavorited ? 'Remove from favorites' : 'Add to favorites'}
            >
              {isStudent && isFavorited ? '♥' : '♡'}
            </Button>
          </div>
        )}
      </article>
    </Card>
  );
}
