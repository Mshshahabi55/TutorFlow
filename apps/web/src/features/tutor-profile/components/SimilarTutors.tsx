import { Skeleton } from '@/components/ui/Skeleton';
import { useToast } from '@/components/ui/Toast';
import { TutorResultCard } from '@/features/search/components/TutorResultCard';
import { useFavoriteStatuses, useFavoriteToggle } from '@/features/search/useTutorSearch';
import { useSimilarTutors } from '../useTutorProfile';
import { ProfileSection } from './ProfileSection';
import styles from './SimilarTutors.module.css';

export interface SimilarTutorsProps {
  subjectId: string | undefined;
  excludeTutorUserId: string;
  isStudent: boolean;
  isTutorOnlyViewer: boolean;
  onRequireAuth: (action: string) => void;
}

/** Reuses Search's existing endpoint + the existing `TutorResultCard` — no bespoke
 * recommendation engine, no new card component (ADR-0022, PREPLY_TUTOR_PROFILE_PARITY_SPEC.md
 * §8). Favorite/Message/Book Trial on these cards reuse the same bulk-favorites hook and
 * TutorProfileView's own single AuthGateModal (ADR-0024) — not a second mechanism. Hidden
 * entirely on error or when there's nothing to show, rather than showing an empty carousel. */
export function SimilarTutors({ subjectId, excludeTutorUserId, isStudent, isTutorOnlyViewer, onRequireAuth }: SimilarTutorsProps) {
  const { data, isLoading, isError } = useSimilarTutors(subjectId, excludeTutorUserId);
  const tutorUserIds = data?.items.map((item) => item.tutorUserId) ?? [];
  const { data: favoriteStatuses } = useFavoriteStatuses(tutorUserIds, isStudent);
  const favoritedTutorUserIds = new Set(favoriteStatuses?.favoritedTutorUserIds ?? []);
  const { add: addFavorite, remove: removeFavorite } = useFavoriteToggle();
  const { showToast } = useToast();

  const handleToggleFavorite = (tutorUserId: string) => {
    if (favoritedTutorUserIds.has(tutorUserId)) {
      removeFavorite.mutate(tutorUserId, { onSuccess: () => showToast('Removed from favorites') });
    } else {
      addFavorite.mutate(tutorUserId, { onSuccess: () => showToast('Added to favorites') });
    }
  };

  if (isError) {
    return null;
  }

  if (isLoading) {
    return (
      <ProfileSection title="You might also like">
        <Skeleton width="100%" height="10rem" />
      </ProfileSection>
    );
  }

  if (!data || data.items.length === 0) {
    return null;
  }

  return (
    <ProfileSection title="You might also like">
      <div className={styles.grid}>
        {data.items.map((tutor) => (
          <TutorResultCard
            key={tutor.tutorUserId}
            tutor={tutor}
            isStudent={isStudent}
            isTutorOnlyViewer={isTutorOnlyViewer}
            isFavorited={favoritedTutorUserIds.has(tutor.tutorUserId)}
            onToggleFavorite={() => handleToggleFavorite(tutor.tutorUserId)}
            onRequireAuth={onRequireAuth}
          />
        ))}
      </div>
    </ProfileSection>
  );
}
