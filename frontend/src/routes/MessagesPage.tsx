import { Link as RouterLink } from "react-router-dom";
import { Button, Stack } from "@mui/material";
import { PageHeader } from "@/shared/components/PageHeader";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { paths } from "@/routes/paths";

/**
 * RC2: "Messages" is part of the new Student/Tutor navigation (Preply/
 * Italki-style IA) but there is no messaging capability anywhere in this
 * API — no endpoint, no DTO, nothing to wire up. Rather than hide the nav
 * item (which would leave the promised "Messages" destination dangling)
 * or fabricate a fake inbox, this is an honest "coming soon" placeholder —
 * presentation only, no invented backend capability.
 */
export function MessagesPage() {
  return (
    <Stack spacing={3}>
      <PageHeader title="Messages" />
      <EmptyState
        title="Messaging is coming soon"
        description="You'll be able to message your tutor directly from here. For now, sort out lesson details when you book."
        action={
          <Button component={RouterLink} to={paths.discovery.tutorSearch} variant="contained">
            Find Tutors
          </Button>
        }
      />
    </Stack>
  );
}
