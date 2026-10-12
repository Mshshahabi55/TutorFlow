import { resolveMediaUrl } from '../mediaUrl';
import styles from './TutorIntroVideo.module.css';

export function TutorIntroVideo({
  introVideoReference,
  profilePhotoReference,
  displayName,
}: {
  introVideoReference: string | null;
  profilePhotoReference: string | null;
  displayName: string;
}) {
  const videoUrl = resolveMediaUrl(introVideoReference);

  // Matches Preply's own observed behavior: a tutor with no video simply has no video section
  // at all — never an empty-state placeholder (PREPLY_TUTOR_PROFILE_PARITY_SPEC.md §1.6).
  if (!videoUrl) {
    return null;
  }

  return (
    <video
      className={styles.video}
      controls
      preload="none"
      poster={resolveMediaUrl(profilePhotoReference) ?? undefined}
      aria-label={`Introduction video from ${displayName}`}
    >
      <source src={videoUrl} />
    </video>
  );
}
