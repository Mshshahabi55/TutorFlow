import { Avatar, Box, Card, CardContent, Chip, Stack, Typography } from "@mui/material";
import PersonRoundedIcon from "@mui/icons-material/PersonRounded";
import { monoFontFamily } from "@/app/theme";
import type { StudentDto } from "@/services/api/dtos";

/**
 * "Who is this session with?" from the Tutor's side — same "Booking with"
 * card shape as `TutorSummaryCard` (Phase 3 Step 4/5), but for the
 * Student side of a Session. `StudentDto` has only two fields
 * (`studentId`, `isMinor`) — no name, so the id is shown as-is, same
 * honesty convention as every other entity without a name field in this
 * app.
 */
export function StudentSummaryCard({ student }: { student: StudentDto }) {
  return (
    <Card variant="outlined" aria-label="Student">
      <CardContent>
        <Typography variant="overline" color="text.secondary">
          Student
        </Typography>
        <Stack direction="row" spacing={2} alignItems="center" mt={0.5}>
          <Avatar sx={{ width: 56, height: 56, bgcolor: "action.selected" }}>
            <PersonRoundedIcon color="disabled" aria-hidden="true" />
          </Avatar>
          <Box flex={1} minWidth={0}>
            <Typography
              variant="subtitle1"
              fontWeight={600}
              fontFamily={monoFontFamily}
              sx={{ wordBreak: "break-all" }}
            >
              {student.studentId}
            </Typography>
            {student.isMinor ? <Chip label="Minor" size="small" sx={{ mt: 0.5 }} /> : null}
          </Box>
        </Stack>
      </CardContent>
    </Card>
  );
}
