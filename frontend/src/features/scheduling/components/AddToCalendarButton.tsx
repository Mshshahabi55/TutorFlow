import { Button } from "@mui/material";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import { buildSessionIcs, downloadTextFile } from "@/shared/utils/ics";
import { DeliveryMode } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

export interface AddToCalendarButtonProps {
  session: SessionDto;
  /** Falls back to a generic label when unknown (e.g. the id-only booking success state, before TutorProfileHero-style data is available). */
  tutorName?: string;
}

/**
 * A plain client-side .ics download — no calendar-provider integration
 * (Google/Outlook OAuth, etc.), which would be a materially larger scope
 * this feature doesn't need: every mainstream calendar app already knows
 * how to import a downloaded .ics file, so a single static file download
 * covers the same need without a new dependency or credential.
 */
export function AddToCalendarButton({ session, tutorName }: AddToCalendarButtonProps) {
  function handleClick() {
    const who = tutorName ?? "your Tutor";
    const ics = buildSessionIcs({
      sessionId: session.sessionId,
      startTimeUtc: session.scheduledTimeUtc,
      endTimeUtc: session.endTimeUtc,
      summary: `Lesson with ${who}`,
      description:
        session.deliveryMode === DeliveryMode.Online
          ? "Online lesson booked via TutorFlow. Join details will be available on the lesson's own page closer to the start time."
          : "In-person lesson booked via TutorFlow.",
    });
    downloadTextFile(`tutorflow-lesson-${session.sessionId.slice(0, 8)}.ics`, ics, "text/calendar");
  }

  return (
    <Button variant="outlined" startIcon={<EventRoundedIcon />} onClick={handleClick}>
      Add to Calendar
    </Button>
  );
}
