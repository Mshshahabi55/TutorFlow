import { useState, type ReactNode } from "react";
import {
  Box,
  Button,
  Drawer,
  IconButton,
  Stack,
  Typography,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import TuneRoundedIcon from "@mui/icons-material/TuneRounded";
import CloseRoundedIcon from "@mui/icons-material/CloseRounded";

export interface TutorFilterPanelProps {
  /** Used only for the mobile "Filters (n)" button label — desktop shows every field inline already. */
  activeFilterCount: number;
  children: ReactNode;
}

/**
 * Desktop (`md`+): the filter fields render inline, in a wrapping row.
 * Below `md`: the exact same fields render once, inside a Drawer opened by
 * a "Filters" button — never duplicated, so there is always exactly one
 * instance of each field's `<label>` in the DOM, regardless of viewport.
 */
export function TutorFilterPanel({ activeFilterCount, children }: TutorFilterPanelProps) {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up("md"));
  const [open, setOpen] = useState(false);

  if (isDesktop) {
    return (
      <Stack direction="row" flexWrap="wrap" gap={2} useFlexGap>
        {children}
      </Stack>
    );
  }

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
      <Drawer anchor="bottom" open={open} onClose={() => setOpen(false)}>
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
