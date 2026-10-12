import { Avatar } from '@/components/ui/Avatar';
import { Rating } from '@/components/ui/Rating';
import { TutorTrustBadges } from './TutorTrustBadges';
import { TutorPublicProfile } from '../types';
import { resolveMediaUrl } from '../mediaUrl';
import styles from './TutorProfileHero.module.css';

function countryFlagEmoji(countryCode: string): string | null {
  if (!/^[a-zA-Z]{2}$/.test(countryCode)) {
    return null;
  }
  const codePoints = countryCode
    .toUpperCase()
    .split('')
    .map((char) => 127397 + char.charCodeAt(0));
  return String.fromCodePoint(...codePoints);
}

export function TutorProfileHero({ profile }: { profile: TutorPublicProfile }) {
  const flag = profile.countryOfBirthCode ? countryFlagEmoji(profile.countryOfBirthCode) : null;

  return (
    <div className={styles.root}>
      <Avatar src={resolveMediaUrl(profile.profilePhotoReference)} name={profile.displayName} size="lg" />
      <div className={styles.identity}>
        <h1 className={styles.name}>{profile.displayName}</h1>
        {profile.headline && <p className={styles.headline}>{profile.headline}</p>}
        {flag && (
          <p className={styles.origin}>
            <span aria-hidden="true">{flag}</span>{' '}
            <span className="visually-hidden">Country of birth: </span>
            {profile.countryOfBirthCode}
          </p>
        )}
        {/* No reviews pipeline exists yet (Reviews module is a stub) — Rating already renders
            "No reviews yet" for a zero count, the same honest empty state the search card uses. */}
        <Rating value={0} reviewCount={0} />
        <div className={styles.badgeRow}>
          <TutorTrustBadges isSuperTutor={profile.isSuperTutor} isProfessionalTutor={profile.isProfessionalTutor} />
        </div>
      </div>
    </div>
  );
}
