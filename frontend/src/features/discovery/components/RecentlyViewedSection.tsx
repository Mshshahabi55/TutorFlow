import { Link as RouterLink } from "react-router-dom";
import { Avatar, Box, Stack, Typography, alpha } from "@mui/material";
import PersonRoundedIcon from "@mui/icons-material/PersonRounded";
import { useRecentlyViewedTutors } from "@/features/discovery/hooks/useRecentlyViewedTutors";
import { useTutorsByIds } from "@/features/identity/hooks/useTutorQueries";
import { paths } from "@/routes/paths";

/**
 * A horizontal strip of small avatar links, shown above the search results
 * grid — same "recently viewed" convenience e-commerce search pages
 * typically offer, backed entirely by `useRecentlyViewedTutors` (client-side
 * only, see that hook's own comment). Silently renders nothing until at
 * least one Tutor has actually been viewed; a since-removed Tutor's id
 * (its own query now erroring) is skipped rather than shown broken.
 */
export function RecentlyViewedSection() {
  const { recentIds } = useRecentlyViewedTutors();
  const tutorQueries = useTutorsByIds(recentIds);

  const tutors = tutorQueries
    .map((query) => (query.isSuccess ? query.data : null))
    .filter((tutor): tutor is NonNullable<typeof tutor> => tutor !== null);

  if (tutors.length === 0) {
    return null;
  }

  return (
    <Box>
      <Typography variant="overline" color="text.secondary">
        Recently viewed
      </Typography>
      <Stack direction="row" spacing={1.5} sx={{ overflowX: "auto", pb: 1 }}>
        {tutors.map((tutor) => (
          <Stack
            key={tutor.tutorId}
            component={RouterLink}
            to={paths.identity.tutorDetail(tutor.tutorId)}
            alignItems="center"
            spacing={0.5}
            sx={{ textDecoration: "none", color: "inherit", flexShrink: 0, width: 72 }}
          >
            <Avatar
              src={tutor.photoUrl ?? undefined}
              sx={{
                width: 48,
                height: 48,
                bgcolor: (t) => alpha(t.palette.primary.main, t.palette.mode === "dark" ? 0.22 : 0.12),
                color: "primary.main",
              }}
            >
              <PersonRoundedIcon aria-hidden="true" fontSize="small" />
            </Avatar>
            <Typography variant="caption" noWrap textAlign="center" sx={{ width: "100%" }}>
              {tutor.displayName ?? tutor.subject ?? "Tutor"}
            </Typography>
          </Stack>
        ))}
      </Stack>
    </Box>
  );
}
