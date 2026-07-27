import { Button, Chip, InputAdornment, Stack, Typography } from "@mui/material";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import { FormTextField } from "@/shared/components/forms/FormTextField";

export interface ActiveFilterChip {
  key: string;
  label: string;
  onClear: () => void;
}

export interface SearchHeroProps {
  resultCount?: number;
  isSearching: boolean;
  hasActiveFilters: boolean;
  activeFilters: ActiveFilterChip[];
  onClearAll: () => void;
}

/**
 * The Tutor Directory's discovery-focused header: a large Subject search
 * box (still the existing `subject` filter field, just promoted and
 * restyled — same `<form>` submit as every other filter, no new query
 * behavior), a result count, and a removable summary of every currently
 * applied filter.
 */
export function SearchHero({
  resultCount,
  isSearching,
  hasActiveFilters,
  activeFilters,
  onClearAll,
}: SearchHeroProps) {
  return (
    <Stack spacing={1.5}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
        <FormTextField
          name="subject"
          label="Subject"
          placeholder="Search by subject, e.g. Mathematics, Physics, English…"
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchRoundedIcon color="action" aria-hidden="true" />
                </InputAdornment>
              ),
            },
          }}
          sx={{ "& .MuiInputBase-root": { borderRadius: 999, fontSize: "1.05rem" } }}
        />
        <Stack direction="row" spacing={1} flexShrink={0} alignItems="flex-start">
          <Button type="submit" variant="contained" size="large" sx={{ borderRadius: 999, px: 4 }}>
            Search
          </Button>
          {hasActiveFilters ? (
            <Button variant="text" onClick={onClearAll}>
              Clear filters
            </Button>
          ) : null}
        </Stack>
      </Stack>

      <Typography variant="caption" color="text.secondary">
        Tip: leave a filter blank to widen your results — subject, language, location, and
        availability all combine together.
      </Typography>

      <Stack direction="row" alignItems="center" flexWrap="wrap" gap={1}>
        <Typography variant="body2" color="text.secondary">
          {isSearching
            ? "Searching…"
            : resultCount === 1
              ? "1 tutor found"
              : `${resultCount ?? 0} tutors found`}
        </Typography>
        {activeFilters.map((filter) => (
          <Chip key={filter.key} label={filter.label} size="small" onDelete={filter.onClear} />
        ))}
      </Stack>
    </Stack>
  );
}
