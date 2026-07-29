import { Link as RouterLink, useSearchParams } from "react-router-dom";
import {
  Avatar,
  Box,
  Button,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  alpha,
} from "@mui/material";
import PersonRoundedIcon from "@mui/icons-material/PersonRounded";
import CompareArrowsRoundedIcon from "@mui/icons-material/CompareArrowsRounded";
import { useTutorsByIds } from "@/features/identity/hooks/useTutorQueries";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { PageHeader } from "@/shared/components/PageHeader";
import { formatToman } from "@/shared/money/rial";
import { formatMinutesList } from "@/shared/utils/duration";
import { paths } from "@/routes/paths";
import type { TutorDto } from "@/services/api/dtos";

interface ComparisonRow {
  label: string;
  render: (tutor: TutorDto) => React.ReactNode;
}

const ROWS: ComparisonRow[] = [
  { label: "Hourly rate", render: (t) => (t.hourlyRate !== null ? `${formatToman(t.hourlyRate)} Toman/hr` : "Not set") },
  { label: "Primary subject", render: (t) => t.subject ?? "Not set" },
  {
    label: "Additional subjects",
    render: (t) =>
      (t.tutorSubjects ?? []).length > 0
        ? t.tutorSubjects!.map((entry) => (entry.level ? `${entry.subject} (${entry.level})` : entry.subject)).join(", ")
        : "None",
  },
  { label: "Native language", render: (t) => t.language ?? "Not set" },
  { label: "Other languages", render: (t) => ((t.otherLanguages ?? []).length > 0 ? t.otherLanguages!.join(", ") : "None") },
  { label: "Location", render: (t) => t.location ?? "Not set" },
  { label: "Years of experience", render: (t) => (t.yearsOfExperience != null ? String(t.yearsOfExperience) : "Not set") },
  {
    label: "Session lengths",
    render: (t) => (t.offeredDurations.length > 0 ? `${formatMinutesList(t.offeredDurations)} min` : "Not set"),
  },
  {
    label: "Trial lesson",
    render: (t) =>
      t.trialLessonAvailable
        ? `Available${t.trialLessonPrice != null ? ` — ${formatToman(t.trialLessonPrice)} Toman` : ""}`
        : "Not offered",
  },
];

/**
 * Reads `?ids=a,b,c` (set by CompareBar) — always re-fetches fresh Tutor
 * data rather than trusting whatever was in memory on the search page, so a
 * bookmarked/shared compare link stays correct. No rating/review column:
 * no such data exists anywhere in this API (reviews/ratings are explicitly
 * out of scope, PRODUCT_REQUIREMENTS.md Decision C.12).
 */
export function CompareTutorsPage() {
  const [searchParams] = useSearchParams();
  const tutorIds = (searchParams.get("ids") ?? "").split(",").filter((id) => id.length > 0);
  const tutorQueries = useTutorsByIds(tutorIds);

  const isLoading = tutorQueries.some((query) => query.isPending);
  const tutors = tutorQueries
    .map((query) => (query.isSuccess ? query.data : null))
    .filter((tutor): tutor is NonNullable<typeof tutor> => tutor !== null);

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Compare Tutors"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            A side-by-side look at the Tutors you selected.
          </Typography>
        }
      />

      {tutorIds.length < 2 ? (
        <EmptyState
          title="Select at least two Tutors to compare"
          description="Head back to the Tutor Directory, check the Compare box on two or more cards, then choose Compare now."
          icon={<CompareArrowsRoundedIcon />}
          action={
            <Button component={RouterLink} to={paths.discovery.tutorSearch} variant="contained">
              Find Tutors
            </Button>
          }
        />
      ) : isLoading ? (
        <Typography color="text.secondary">Loading…</Typography>
      ) : tutors.length < 2 ? (
        <EmptyState
          title="Not enough of these Tutors are still available"
          description="One or more selected Tutors may have been removed or suspended."
          icon={<CompareArrowsRoundedIcon />}
        />
      ) : (
        <TableContainer sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell />
                {tutors.map((tutor) => (
                  <TableCell key={tutor.tutorId} align="center">
                    <Stack alignItems="center" spacing={1}>
                      <Avatar
                        src={tutor.photoUrl ?? undefined}
                        sx={{
                          width: 56,
                          height: 56,
                          bgcolor: (t) => alpha(t.palette.primary.main, t.palette.mode === "dark" ? 0.22 : 0.12),
                          color: "primary.main",
                        }}
                      >
                        <PersonRoundedIcon aria-hidden="true" />
                      </Avatar>
                      <Typography variant="subtitle2" fontWeight={700}>
                        {tutor.displayName ?? tutor.subject ?? "Tutor"}
                      </Typography>
                      <Stack direction="row" spacing={1}>
                        <Button component={RouterLink} to={paths.identity.tutorDetail(tutor.tutorId)} size="small">
                          View profile
                        </Button>
                        <Button
                          component={RouterLink}
                          to={`${paths.scheduling.bookSession}?tutorId=${tutor.tutorId}`}
                          size="small"
                          variant="contained"
                        >
                          Book
                        </Button>
                      </Stack>
                    </Stack>
                  </TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {ROWS.map((row) => (
                <TableRow key={row.label}>
                  <TableCell component="th" scope="row" sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>
                    {row.label}
                  </TableCell>
                  {tutors.map((tutor) => (
                    <TableCell key={tutor.tutorId} align="center">
                      {row.render(tutor)}
                    </TableCell>
                  ))}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <Box>
        <Button component={RouterLink} to={paths.discovery.tutorSearch} variant="outlined">
          Back to Tutor Directory
        </Button>
      </Box>
    </Stack>
  );
}
