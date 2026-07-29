import { Typography } from "@mui/material";

export interface SearchResultsHeaderProps {
  resultCount?: number;
  isSearching: boolean;
}

/**
 * The "Results count" step between Filters/Active Filters and the Tutor
 * Grid (RC — Preply Redesign Phase 3). `aria-live="polite"` so a screen
 * reader announces the updated count after a search, the same way a
 * sighted user sees it change, without needing to re-focus anything.
 *
 * No sort control is rendered here: `GET /tutors/search` has no sort
 * parameter today (`useSearchTutors`/`searchTutors` accept only
 * subject/language/location/availableFrom + page/pageSize) — inventing a
 * sort dropdown with no real effect on the results would be a fake
 * feature, not a UI improvement. See `DESIGN-SYSTEM.md` §10 for this
 * phase's full backend-limitations note.
 */
export function SearchResultsHeader({ resultCount, isSearching }: SearchResultsHeaderProps) {
  return (
    <Typography variant="h6" component="p" fontWeight={700} aria-live="polite">
      {isSearching
        ? "Searching…"
        : resultCount === 1
          ? "1 tutor found"
          : `${resultCount ?? 0} tutors found`}
    </Typography>
  );
}
