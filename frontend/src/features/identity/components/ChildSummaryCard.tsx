import { Link as RouterLink } from "react-router-dom";
import { Avatar, Box, Button, Card, CardContent, Stack, Typography } from "@mui/material";
import PersonRoundedIcon from "@mui/icons-material/PersonRounded";
import CalendarMonthRoundedIcon from "@mui/icons-material/CalendarMonthRounded";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { monoFontFamily } from "@/app/theme";
import { RelationshipStatus } from "@/services/api/dtos";
import type { RelationshipDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

export interface ChildSummaryCardProps {
  relationship: RelationshipDto;
}

/**
 * A Parent/Guardian-specific card: presents one of their Student
 * Relationships as "a child," something neither the Student nor Tutor
 * Workspace has a reason to show. `RelationshipDto` has no Student name —
 * same honesty convention as every other entity without a name field in
 * this app — so `studentId` is shown as-is. Only a Confirmed Relationship
 * (the business rule already encoded by `RelationshipStatus`) gets a
 * "View sessions" link — an Invited-but-not-yet-confirmed Relationship
 * isn't an authorized family link yet, so it's shown with its own status,
 * not a dead link to session data that isn't really the parent's to view.
 */
export function ChildSummaryCard({ relationship }: ChildSummaryCardProps) {
  const isConfirmed = relationship.status === RelationshipStatus.Confirmed;

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
                {relationship.studentId}
              </Typography>
              <StatusPill
                label={isConfirmed ? "Confirmed" : "Invited"}
                tone={isConfirmed ? "success" : "warning"}
              />
            </Stack>
          </Box>
        </Stack>
        {isConfirmed ? (
          <Button
            component={RouterLink}
            to={paths.scheduling.studentSchedule(relationship.studentId)}
            size="small"
            variant="outlined"
            startIcon={<CalendarMonthRoundedIcon />}
            sx={{ mt: 2 }}
          >
            View sessions
          </Button>
        ) : (
          <Typography variant="body2" color="text.secondary" mt={2}>
            Waiting for this Relationship to be confirmed.
          </Typography>
        )}
      </CardContent>
    </Card>
  );
}
