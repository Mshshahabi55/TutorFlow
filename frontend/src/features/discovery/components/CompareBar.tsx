import { Link as RouterLink } from "react-router-dom";
import { Box, Button, Chip, Paper, Stack, Typography } from "@mui/material";
import CompareArrowsRoundedIcon from "@mui/icons-material/CompareArrowsRounded";
import { paths } from "@/routes/paths";
import type { TutorDto } from "@/services/api/dtos";

export const MIN_COMPARE_COUNT = 2;
export const MAX_COMPARE_COUNT = 4;

export interface CompareBarProps {
  selectedTutors: TutorDto[];
  onRemove: (tutorId: string) => void;
  onClear: () => void;
}

/**
 * A sticky bottom bar, visible only once at least one Tutor is selected for
 * comparison — mirrors the "sticky action bar" convention `BookSessionPage`
 * and the onboarding wizard's own mobile-sticky-actions already established.
 * "Compare now" is disabled below MIN_COMPARE_COUNT: comparing one Tutor to
 * nothing isn't a comparison.
 */
export function CompareBar({ selectedTutors, onRemove, onClear }: CompareBarProps) {
  if (selectedTutors.length === 0) {
    return null;
  }

  return (
    <Paper
      elevation={4}
      sx={{
        position: "sticky",
        bottom: 0,
        zIndex: 3,
        p: 2,
        display: "flex",
        flexWrap: "wrap",
        alignItems: "center",
        gap: 1.5,
      }}
    >
      <CompareArrowsRoundedIcon color="primary" aria-hidden="true" />
      <Typography variant="body2" fontWeight={600}>
        Compare Tutors ({selectedTutors.length}/{MAX_COMPARE_COUNT})
      </Typography>
      <Stack direction="row" flexWrap="wrap" gap={0.75} flexGrow={1}>
        {selectedTutors.map((tutor) => (
          <Chip
            key={tutor.tutorId}
            label={tutor.displayName ?? tutor.subject ?? "Tutor"}
            size="small"
            onDelete={() => onRemove(tutor.tutorId)}
          />
        ))}
      </Stack>
      <Box>
        <Button size="small" onClick={onClear} sx={{ mr: 1 }}>
          Clear
        </Button>
        <Button
          component={RouterLink}
          to={`${paths.discovery.tutorCompare}?ids=${selectedTutors.map((tutor) => tutor.tutorId).join(",")}`}
          variant="contained"
          size="small"
          disabled={selectedTutors.length < MIN_COMPARE_COUNT}
        >
          Compare now
        </Button>
      </Box>
    </Paper>
  );
}
