import FavoriteRoundedIcon from "@mui/icons-material/FavoriteRounded";
import { Stack, Typography } from "@mui/material";
import { useFavoriteTutors } from "@/features/discovery/hooks/useFavoriteTutors";
import { useTutorsByIds } from "@/features/identity/hooks/useTutorQueries";
import { TutorCard } from "@/features/discovery/components/TutorCard";
import { TutorCardSkeleton } from "@/features/discovery/components/TutorCardSkeleton";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { PageHeader } from "@/shared/components/PageHeader";

/**
 * Reads `useFavoriteTutors`' own (client-side only) id list and fetches
 * each one's current data — a since-suspended/removed Tutor's card is
 * simply skipped, never shown broken, since favoriting stores only an id,
 * never a snapshot of that Tutor's data at the time.
 */
export function FavoriteTutorsPage() {
  const { favoriteIds } = useFavoriteTutors();
  const tutorQueries = useTutorsByIds(favoriteIds);

  const isLoading = tutorQueries.some((query) => query.isPending);
  const tutors = tutorQueries
    .map((query) => (query.isSuccess ? query.data : null))
    .filter((tutor): tutor is NonNullable<typeof tutor> => tutor !== null);

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Your Favorite Tutors"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Saved on this device — tap the heart on any Tutor to add or remove them.
          </Typography>
        }
      />

      {favoriteIds.length === 0 ? (
        <EmptyState
          title="No favorites yet"
          description="Browse the Tutor Directory and tap the heart on a Tutor's card to save them here."
          icon={<FavoriteRoundedIcon />}
        />
      ) : isLoading ? (
        <Stack direction="row" flexWrap="wrap" gap={3} alignItems="stretch">
          {favoriteIds.map((id) => (
            <TutorCardSkeleton key={id} />
          ))}
        </Stack>
      ) : tutors.length === 0 ? (
        <EmptyState
          title="Your favorited Tutors are no longer available"
          description="They may have been removed or suspended."
          icon={<FavoriteRoundedIcon />}
        />
      ) : (
        <Stack direction="row" flexWrap="wrap" gap={3} alignItems="stretch">
          {tutors.map((tutor) => (
            <TutorCard key={tutor.tutorId} tutor={tutor} />
          ))}
        </Stack>
      )}
    </Stack>
  );
}
