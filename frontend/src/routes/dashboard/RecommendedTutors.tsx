import { Link as RouterLink } from "react-router-dom";
import { Button, Card, CardContent, Skeleton, Stack, Typography } from "@mui/material";
import { useSearchTutors } from "@/features/discovery/hooks/useSearchTutors";
import type { SearchTutorsFilters } from "@/features/discovery/api/discoveryService";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { formatToman } from "@/shared/money/rial";
import { paths } from "@/routes/paths";
import type { TutorDto } from "@/services/api/dtos";

const NO_FILTERS: SearchTutorsFilters = {
  subject: "",
  language: "",
  location: "",
  availableFrom: "",
};

/** How many Tutors the Recommended Tutors widget previews — not a real recommendation ranking, just the first page of the existing search result (see component doc comment). */
const PREVIEW_COUNT = 4;

const CARD_SX = { flex: "1 1 220px", minWidth: 220, maxWidth: 280 } as const;

function RecommendedTutorCardSkeleton() {
  return (
    <Card variant="outlined" sx={CARD_SX} data-testid="recommended-tutor-skeleton">
      <CardContent>
        <Skeleton variant="text" width="70%" height={28} />
        <Skeleton variant="text" width="45%" />
        <Skeleton variant="text" width="55%" />
        <Skeleton variant="rectangular" height={32} sx={{ mt: 2, borderRadius: 1 }} />
      </CardContent>
    </Card>
  );
}

function RecommendedTutorCard({ tutor }: { tutor: TutorDto }) {
  return (
    <Card variant="outlined" sx={CARD_SX}>
      <CardContent>
        <Typography variant="subtitle1" fontWeight={600} gutterBottom noWrap>
          {tutor.subject ?? "Tutor"}
        </Typography>
        <Stack spacing={0.5} mb={1.5}>
          {tutor.language ? (
            <Typography variant="body2" color="text.secondary">
              Speaks {tutor.language}
            </Typography>
          ) : null}
          {tutor.location ? (
            <Typography variant="body2" color="text.secondary">
              {tutor.location}
            </Typography>
          ) : null}
        </Stack>
        <Typography variant="body2" fontWeight={600} gutterBottom>
          {tutor.hourlyRate !== null ? `${formatToman(tutor.hourlyRate)} Toman/hr` : "Rate not set"}
        </Typography>
        <Button
          component={RouterLink}
          to={paths.identity.tutorDetail(tutor.tutorId)}
          size="small"
          variant="outlined"
          fullWidth
        >
          View profile
        </Button>
      </CardContent>
    </Card>
  );
}

/**
 * Reuses Discovery's existing `GET /tutors/search` capability (the same
 * hook/endpoint `TutorSearchPage` calls) with no filters, previewing the
 * first page as "Recommended" — there is no recommendation-engine endpoint
 * to call instead, and this widget does not pretend otherwise. Every
 * rendered field comes straight off `TutorDto`; `subject` stands in for a
 * name because `TutorDto` has none — no field is added, renamed, or
 * reshaped here (see `services/api/dtos.ts`).
 */
export function RecommendedTutors() {
  const searchQuery = useSearchTutors(NO_FILTERS, 1, PREVIEW_COUNT);

  if (searchQuery.isPending) {
    return (
      <Stack direction="row" flexWrap="wrap" gap={2}>
        {Array.from({ length: PREVIEW_COUNT }, (_, index) => (
          <RecommendedTutorCardSkeleton key={index} />
        ))}
      </Stack>
    );
  }

  if (searchQuery.isError) {
    return <ErrorState error={searchQuery.error} onRetry={() => void searchQuery.refetch()} />;
  }

  const tutors = searchQuery.data?.items ?? [];

  if (tutors.length === 0) {
    return (
      <EmptyState
        title="No tutors available yet"
        description="Check back soon as more tutors join TutorFlow."
      />
    );
  }

  return (
    <Stack direction="row" flexWrap="wrap" gap={2}>
      {tutors.map((tutor) => (
        <RecommendedTutorCard key={tutor.tutorId} tutor={tutor} />
      ))}
    </Stack>
  );
}
