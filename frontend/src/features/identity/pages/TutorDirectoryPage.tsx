import { Link as RouterLink } from "react-router-dom";
import { Button, Stack, Typography } from "@mui/material";
import { useTutorDirectory } from "@/features/identity/hooks/useTutorQueries";
import { TutorCard } from "@/features/discovery/components/TutorCard";
import { TutorCardSkeleton } from "@/features/discovery/components/TutorCardSkeleton";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { PageHeader } from "@/shared/components/PageHeader";
import { paths } from "@/routes/paths";

const SKELETON_COUNT = 6;

/**
 * GET /tutors — every discoverable (approved and not suspended) Tutor, not
 * paginated (the endpoint itself returns a plain array). RC3.3: reuses
 * `TutorCard` verbatim (the same marketplace card `TutorSearchPage` and
 * `RecommendedTutors` already render) instead of a raw DataTable — one
 * card component for "here is a Tutor," not a duplicated presentation.
 */
export function TutorDirectoryPage() {
  const directoryQuery = useTutorDirectory();

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Tutor directory"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Every currently discoverable Tutor.
          </Typography>
        }
        action={
          <Button component={RouterLink} to={paths.identity.tutorRegister} variant="outlined">
            Register as Tutor
          </Button>
        }
      />

      {directoryQuery.isPending ? (
        <Stack direction="row" flexWrap="wrap" gap={2}>
          {Array.from({ length: SKELETON_COUNT }, (_, index) => (
            <TutorCardSkeleton key={index} />
          ))}
        </Stack>
      ) : directoryQuery.isError ? (
        <ErrorState error={directoryQuery.error} onRetry={() => void directoryQuery.refetch()} />
      ) : directoryQuery.data.length === 0 ? (
        <EmptyState
          title="No discoverable Tutors yet"
          description="Tutors appear here once an Admin has approved them."
        />
      ) : (
        <Stack direction="row" flexWrap="wrap" gap={2}>
          {directoryQuery.data.map((tutor) => (
            <TutorCard key={tutor.tutorId} tutor={tutor} />
          ))}
        </Stack>
      )}
    </Stack>
  );
}
