import { Stack, Typography } from "@mui/material";
import { useSearchTutors } from "@/features/discovery/hooks/useSearchTutors";
import { TutorCard } from "@/features/discovery/components/TutorCard";
import { TutorCardSkeleton } from "@/features/discovery/components/TutorCardSkeleton";
import { SectionCard } from "@/shared/components/SectionCard";
import type { TutorDto } from "@/services/api/dtos";

const RELATED_TUTORS_LIMIT = 3;
/** Fetch one extra result, since the Tutor being viewed is often part of their own subject's search results and must be filtered out. */
const RELATED_TUTORS_FETCH_COUNT = RELATED_TUTORS_LIMIT + 1;

/**
 * "Related tutors" (Phase 4 PART 1) — only rendered when it's backed by a
 * real, already-supported capability: `GET /tutors/search`, the exact same
 * endpoint `TutorSearchPage`/`RecommendedTutors` already call, filtered to
 * this Tutor's own `subject`. No new endpoint, no new query parameter, no
 * "similar tutor" ranking logic — just the existing subject filter, with
 * the Tutor being viewed excluded from their own "more like this" list.
 * Renders nothing at all if the Tutor has no subject set, or if no *other*
 * Tutor teaching it exists — there is no honest content to show either way.
 */
export function RelatedTutorsSection({ tutor }: { tutor: TutorDto }) {
  const searchQuery = useSearchTutors(
    { subject: tutor.subject ?? "", language: "", location: "", availableFrom: "" },
    1,
    RELATED_TUTORS_FETCH_COUNT,
  );

  if (!tutor.subject) {
    return null;
  }

  if (searchQuery.isPending) {
    return (
      <SectionCard headingComponent="h2" title={`More tutors teaching ${tutor.subject}`}>
        <Stack direction="row" flexWrap="wrap" gap={3} alignItems="stretch">
          {Array.from({ length: RELATED_TUTORS_LIMIT }, (_, index) => (
            <TutorCardSkeleton key={index} />
          ))}
        </Stack>
      </SectionCard>
    );
  }

  if (searchQuery.isError) {
    // A failed "related" query has no consumer-facing value worth an error
    // banner of its own — the rest of the profile already loaded fine.
    return null;
  }

  const otherTutors = searchQuery.data.items
    .filter((candidate) => candidate.tutorId !== tutor.tutorId)
    .slice(0, RELATED_TUTORS_LIMIT);

  if (otherTutors.length === 0) {
    return null;
  }

  return (
    <SectionCard headingComponent="h2" title={`More tutors teaching ${tutor.subject}`}>
      <Typography variant="body2" color="text.secondary" mb={2}>
        Compare a few other real options before you book.
      </Typography>
      <Stack direction="row" flexWrap="wrap" gap={3} alignItems="stretch">
        {otherTutors.map((candidate) => (
          <TutorCard key={candidate.tutorId} tutor={candidate} />
        ))}
      </Stack>
    </SectionCard>
  );
}
