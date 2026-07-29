import { Button, IconButton, InputAdornment, Stack } from "@mui/material";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import ClearRoundedIcon from "@mui/icons-material/ClearRounded";
import { useFormContext, useWatch } from "react-hook-form";
import { FormTextField } from "@/shared/components/forms/FormTextField";

export interface ActiveFilterChip {
  key: string;
  label: string;
  onClear: () => void;
}

/**
 * The Tutor Directory's single visual focal point — a large, pill-shaped
 * Subject search box (still the existing `subject` filter field, just
 * promoted and restyled — same `<form>` submit as every other filter, no
 * new query behavior) plus its own Search button. Result count and active
 * filters moved out to their own components (`SearchResultsHeader`,
 * `ActiveFiltersBar`) so this component has exactly one job.
 */
export function SearchHero() {
  const { setValue, setFocus } = useFormContext<Record<string, unknown>>();
  const subjectValue = useWatch<Record<string, unknown>>({ name: "subject" });
  const hasSubjectValue = Boolean(subjectValue);

  function handleClearSubject() {
    setValue("subject", "", { shouldDirty: true });
    setFocus("subject");
  }

  return (
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
            endAdornment: hasSubjectValue ? (
              <InputAdornment position="end">
                <IconButton
                  aria-label="Clear subject"
                  onClick={handleClearSubject}
                  edge="end"
                  size="small"
                >
                  <ClearRoundedIcon fontSize="small" />
                </IconButton>
              </InputAdornment>
            ) : undefined,
          },
        }}
        sx={{
          "& .MuiInputBase-root": {
            borderRadius: 999,
            fontSize: "1.125rem",
            minHeight: 60,
            pl: 1,
          },
        }}
      />
      <Button
        type="submit"
        variant="contained"
        size="large"
        sx={{ borderRadius: 999, px: 4, minHeight: 60, flexShrink: 0 }}
      >
        Search
      </Button>
    </Stack>
  );
}
