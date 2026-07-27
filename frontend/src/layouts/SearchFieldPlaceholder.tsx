import { useState } from "react";
import { InputAdornment, TextField } from "@mui/material";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";

/**
 * UI only (per the shell brief): a real, typable field — local state only,
 * so it doesn't read as broken — but wired to nothing. No `onSubmit`, no
 * query, no navigation, no backend request of any kind.
 */
export function SearchFieldPlaceholder() {
  const [value, setValue] = useState("");

  return (
    <TextField
      size="small"
      placeholder="Search…"
      value={value}
      onChange={(event) => setValue(event.target.value)}
      slotProps={{
        htmlInput: { "aria-label": "Search" },
        input: {
          startAdornment: (
            <InputAdornment position="start">
              <SearchRoundedIcon fontSize="small" />
            </InputAdornment>
          ),
        },
      }}
      sx={{ width: 240 }}
    />
  );
}
