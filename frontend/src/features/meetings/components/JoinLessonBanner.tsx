import { Alert, Button } from "@mui/material";
import VideocamRoundedIcon from "@mui/icons-material/VideocamRounded";
import { useActiveMeetingForConversation } from "@/features/meetings/hooks/useMeetingQueries";
import { deriveMeetingTiming } from "@/features/meetings/utils/meetingTiming";
import { useNow } from "@/features/meetings/hooks/useNow";
import { MeetingStatus } from "@/services/api/dtos";

/**
 * RC5.3: "Conversation page should surface the Join Lesson action whenever
 * a meeting exists" (docs/adr/ADR-023-...). Renders nothing at all when
 * there is no relevant online Session/Meeting between the two participants
 * — the common case — never a placeholder or empty banner.
 */
export function JoinLessonBanner({ conversationId }: { conversationId: string }) {
  const meetingQuery = useActiveMeetingForConversation(conversationId);
  const now = useNow();

  const meeting = meetingQuery.data;
  if (!meeting || meeting.status === MeetingStatus.Cancelled) {
    return null;
  }

  const timing = deriveMeetingTiming(meeting.startsAtUtc, meeting.endsAtUtc, now);
  if (timing.phase === "ended") {
    return null;
  }

  return (
    <Alert
      severity="info"
      icon={<VideocamRoundedIcon fontSize="inherit" />}
      action={
        <Button
          component="a"
          href={meeting.joinUrl}
          target="_blank"
          rel="noopener noreferrer"
          size="small"
          variant="contained"
        >
          Join Lesson
        </Button>
      }
    >
      {timing.phase === "live" ? "Your lesson is live now." : `Your lesson ${timing.label.toLowerCase()}.`}
    </Alert>
  );
}
