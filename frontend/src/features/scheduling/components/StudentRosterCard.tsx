import { Avatar, Box, Card, CardContent, Stack, Typography } from "@mui/material";
import PersonRoundedIcon from "@mui/icons-material/PersonRounded";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { monoFontFamily } from "@/app/theme";

export interface StudentRosterEntry {
  studentId: string;
  upcomingSessions: number;
  totalSessions: number;
}

export interface StudentRosterCardProps {
  entry: StudentRosterEntry;
}

/**
 * A Tutor-specific card for "My Students" (`TutorStudentsPage`) — mirrors
 * `ChildSummaryCard`'s layout (avatar placeholder, mono id, a status pill)
 * since both are "here is one person, honestly, with no name field to show"
 * cards. `SessionDto` has no Student name, so `studentId` is shown as-is —
 * same honesty convention as every other entity without a name field in
 * this app. No "view schedule" link: a Tutor has no authorized route to a
 * Student's own schedule page (`StudentSessionListPage` is Student/
 * ParentGuardian/AdminStaff only), so this card only ever summarizes counts
 * already derived from the Tutor's own schedule — no new capability implied.
 */
export function StudentRosterCard({ entry }: StudentRosterCardProps) {
  return (
    <Card variant="outlined">
      <CardContent>
        <Stack direction="row" spacing={2} alignItems="center">
          <Avatar sx={{ width: 48, height: 48, bgcolor: "action.selected" }}>
            <PersonRoundedIcon color="disabled" aria-hidden="true" />
          </Avatar>
          <Box flex={1} minWidth={0}>
            <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
              <Typography
                variant="subtitle1"
                fontWeight={600}
                fontFamily={monoFontFamily}
                sx={{ wordBreak: "break-all" }}
              >
                {entry.studentId}
              </Typography>
              {entry.upcomingSessions > 0 ? (
                <StatusPill label={`${entry.upcomingSessions} upcoming`} tone="success" />
              ) : null}
            </Stack>
            <Typography variant="body2" color="text.secondary" mt={0.5}>
              {entry.totalSessions} {entry.totalSessions === 1 ? "lesson" : "lessons"} together
            </Typography>
          </Box>
        </Stack>
      </CardContent>
    </Card>
  );
}
