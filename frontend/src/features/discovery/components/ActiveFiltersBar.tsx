import { Button, Chip, Stack, Typography } from "@mui/material";

export interface ActiveFilterChip {
  key: string;
  label: string;
  onClear: () => void;
}

export interface ActiveFiltersBarProps {
  activeFilters: ActiveFilterChip[];
  onClearAll: () => void;
}

/**
 * The "Active Filters" step between Filters and Results (RC — Preply
 * Redesign Phase 3): every currently-applied filter as its own removable
 * chip (`Chip`'s own `onDelete`, no new removal mechanism — this is a
 * presentation-only extraction of behavior `TutorSearchPage` already had),
 * plus one "Clear filters" action for all of them at once. Renders nothing
 * at all when no filter is active — there is nothing to show or clear.
 */
export function ActiveFiltersBar({ activeFilters, onClearAll }: ActiveFiltersBarProps) {
  if (activeFilters.length === 0) {
    return null;
  }

  return (
    <Stack direction="row" alignItems="center" flexWrap="wrap" gap={1}>
      <Typography variant="body2" fontWeight={600} color="text.secondary">
        Active filters:
      </Typography>
      {activeFilters.map((filter) => (
        <Chip key={filter.key} label={filter.label} size="small" onDelete={filter.onClear} />
      ))}
      <Button variant="text" size="small" onClick={onClearAll}>
        Clear filters
      </Button>
    </Stack>
  );
}
