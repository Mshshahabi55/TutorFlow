import { useState } from "react";
import { Box, Button, Chip, Stack, TextField, Tooltip, Typography } from "@mui/material";
import BookmarkAddRoundedIcon from "@mui/icons-material/BookmarkAddRounded";
import DeleteOutlineRoundedIcon from "@mui/icons-material/DeleteOutlineRounded";
import { useSavedSearches } from "@/features/discovery/hooks/useSavedSearches";
import type { SearchTutorsFilters } from "@/features/discovery/api/discoveryService";

export interface SavedSearchesPanelProps {
  currentFilters: SearchTutorsFilters;
  hasActiveFilters: boolean;
  onApply: (filters: SearchTutorsFilters) => void;
}

/**
 * Client-side only, same reasoning as Favorites/Recently Viewed —
 * `useSavedSearches`' own comment. "Save this search" is disabled with no
 * active filters (saving an empty search is not useful); the name field is
 * a plain inline TextField rather than a modal dialog, matching this app's
 * general preference for lightweight in-place affordances over new dialog
 * chrome for a one-field form.
 */
export function SavedSearchesPanel({ currentFilters, hasActiveFilters, onApply }: SavedSearchesPanelProps) {
  const { savedSearches, saveSearch, removeSearch } = useSavedSearches();
  const [isNaming, setIsNaming] = useState(false);
  const [name, setName] = useState("");

  function handleSave() {
    const trimmed = name.trim();
    if (trimmed.length === 0) {
      return;
    }
    saveSearch(trimmed, currentFilters);
    setName("");
    setIsNaming(false);
  }

  return (
    <Stack spacing={1}>
      {savedSearches.length > 0 ? (
        <Stack direction="row" flexWrap="wrap" gap={0.75} alignItems="center">
          <Typography variant="caption" color="text.secondary">
            Saved searches:
          </Typography>
          {savedSearches.map((search) => (
            <Chip
              key={search.id}
              label={search.name}
              size="small"
              onClick={() => onApply(search.filters)}
              onDelete={() => removeSearch(search.id)}
              deleteIcon={
                <Tooltip title="Delete saved search">
                  <DeleteOutlineRoundedIcon fontSize="small" />
                </Tooltip>
              }
            />
          ))}
        </Stack>
      ) : null}

      {isNaming ? (
        <Stack direction="row" spacing={1} alignItems="center">
          <TextField
            size="small"
            autoFocus
            placeholder="Name this search"
            value={name}
            onChange={(event) => setName(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                handleSave();
              }
              if (event.key === "Escape") {
                setIsNaming(false);
                setName("");
              }
            }}
          />
          <Button size="small" variant="contained" onClick={handleSave} disabled={name.trim().length === 0}>
            Save
          </Button>
          <Button
            size="small"
            onClick={() => {
              setIsNaming(false);
              setName("");
            }}
          >
            Cancel
          </Button>
        </Stack>
      ) : (
        <Box>
          <Button
            size="small"
            startIcon={<BookmarkAddRoundedIcon />}
            disabled={!hasActiveFilters}
            onClick={() => setIsNaming(true)}
          >
            Save this search
          </Button>
        </Box>
      )}
    </Stack>
  );
}
