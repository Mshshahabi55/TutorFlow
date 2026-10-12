import { ProfileSection } from './ProfileSection';
import styles from './TutorAbout.module.css';

export function TutorAbout({ about }: { about: string | null }) {
  if (!about) {
    // Matches the intro-video precedent: a field the tutor never filled in is omitted
    // entirely, not shown as an empty-state placeholder (About isn't required for submission).
    return null;
  }

  return (
    <ProfileSection title="About">
      <p className={styles.body}>{about}</p>
    </ProfileSection>
  );
}
