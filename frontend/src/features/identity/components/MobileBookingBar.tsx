import { Link as RouterLink } from "react-router-dom";
import { Box, Button, Typography } from "@mui/material";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import { formatToman } from "@/shared/money/rial";
import { paths } from "@/routes/paths";
import type { TutorDto } from "@/services/api/dtos";

/** The height `TutorDetailPage`'s own bottom spacer reserves so this bar never covers the page's last section. */
export const MOBILE_BOOKING_BAR_HEIGHT = 72;

/**
 * Phase 4 PART 6: on desktop the booking card already stays in view via
 * `position: sticky` (`TutorDetailPage`'s right column). Below `md`, that
 * column returns to normal document flow and can end up far below the
 * fold — this fixed bottom bar (price + the same "Book Lesson" link every
 * other CTA on this page already uses) keeps the one action that matters
 * reachable without scrolling back up, on mobile/tablet only
 * (`display: { xs: "flex", md: "none" }`). Never shown on desktop, where
 * the sticky rail already does this job.
 */
export function MobileBookingBar({ tutor }: { tutor: TutorDto }) {
  return (
    <Box
      sx={{
        display: { xs: "flex", md: "none" },
        position: "fixed",
        bottom: 0,
        left: 0,
        right: 0,
        zIndex: (t) => t.zIndex.appBar,
        bgcolor: "background.paper",
        borderTop: 1,
        borderColor: "divider",
        alignItems: "center",
        justifyContent: "space-between",
        gap: 2,
        px: 2,
        py: 1.5,
        pb: "calc(12px + env(safe-area-inset-bottom))",
      }}
    >
      <Typography variant="subtitle1" fontWeight={700} color="primary.main" noWrap>
        {tutor.hourlyRate !== null ? `${formatToman(tutor.hourlyRate)} Toman/hr` : "Rate not set"}
      </Typography>
      <Button
        component={RouterLink}
        to={`${paths.scheduling.bookSession}?tutorId=${tutor.tutorId}`}
        variant="contained"
        startIcon={<EventRoundedIcon />}
        sx={{ flexShrink: 0 }}
      >
        Book Lesson
      </Button>
    </Box>
  );
}
