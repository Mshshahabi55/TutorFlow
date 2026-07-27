import { useState, type ReactNode } from "react";
import { Box, Button, Drawer, IconButton, Stack, Typography } from "@mui/material";
import TuneRoundedIcon from "@mui/icons-material/TuneRounded";
import CloseRoundedIcon from "@mui/icons-material/CloseRounded";

export interface TutorFilterPanelProps {
  /** Shown in the "Filters (n)" button label once at least one filter is active. */
  activeFilterCount: number;
  children: ReactNode;
}

/**
 * A slide-over filter panel, at every viewport size — RC2's marketplace
 * search spec: filters live behind a "Filters" button and a Drawer on
 * every screen, not inline on desktop and hidden on mobile. Keeps exactly
 * one instance of each field's `<label>` in the DOM at a time, opened
 * on demand instead of always taking up page width.
 */
export function TutorFilterPanel({ activeFilterCount, children }: TutorFilterPanelProps) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button
        variant="outlined"
        startIcon={<TuneRoundedIcon />}
        onClick={() => setOpen(true)}
        aria-haspopup="dialog"
        sx={{ alignSelf: "flex-start" }}
      >
        {activeFilterCount > 0 ? `Filters (${activeFilterCount})` : "Filters"}
      </Button>
      <Drawer
        anchor="right"
        open={open}
        onClose={() => setOpen(false)}
        slotProps={{ paper: { sx: { width: { xs: "100%", sm: 380 } } } }}
      >
        <Box p={3} role="presentation">
          <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
            <Typography variant="h5" component="h2">
              Filters
            </Typography>
            <IconButton onClick={() => setOpen(false)} aria-label="Close filters">
              <CloseRoundedIcon />
            </IconButton>
          </Stack>
          <Stack spacing={2}>{children}</Stack>
          <Button variant="contained" fullWidth sx={{ mt: 3 }} onClick={() => setOpen(false)}>
            Show results
          </Button>
        </Box>
      </Drawer>
    </>
  );
}
