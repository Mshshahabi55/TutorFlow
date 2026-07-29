import { Link as RouterLink, useNavigate } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import CheckCircleRoundedIcon from "@mui/icons-material/CheckCircleRounded";
import ChatBubbleOutlineRoundedIcon from "@mui/icons-material/ChatBubbleOutlineRounded";
import CalendarMonthRoundedIcon from "@mui/icons-material/CalendarMonthRounded";
import HomeRoundedIcon from "@mui/icons-material/HomeRounded";
import { useStartConversation } from "@/features/communication/hooks/useConversationMutations";
import { AddToCalendarButton } from "@/features/scheduling/components/AddToCalendarButton";
import { useNotification } from "@/shared/hooks/useNotification";
import { CopyableId } from "@/shared/components/CopyableId";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { isConflictError } from "@/services/api/errorClassification";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { paths } from "@/routes/paths";
import type { SessionDto } from "@/services/api/dtos";

export type BookingStatusBannerProps =
  | { status: "success"; session: SessionDto; tutorId?: string; tutorName?: string }
  | {
      status: "error";
      error: unknown;
      onRetry?: () => void;
      /** Reset to the time picker with a fresh Availability list — the recovery action for a 409 (someone else booked this time first). */
      onChooseAnotherTime?: () => void;
      /** The Tutor being booked, if known — offers a way back to their profile from a conflict. */
      tutorId?: string;
    };

/**
 * "Message Tutor" reuses the exact same start-or-resume-conversation
 * mutation `TutorProfileHero`'s `SendMessageAction` already calls — no new
 * capability, just a second entry point into it right after a booking
 * succeeds, when messaging the Tutor is often the very next thing a
 * Student/Parent wants to do. Not role-gated here the way
 * `SendMessageAction` gates itself: this banner only ever renders inside
 * `BookSessionPage`, whose route already restricts booking to Student/
 * ParentGuardian (`router.tsx`), so any viewer reaching this success state
 * is already one of the two roles allowed to start a Conversation.
 */
function MessageTutorAction({ tutorId }: { tutorId: string }) {
  const navigate = useNavigate();
  const { notify } = useNotification();
  const startConversation = useStartConversation();

  function handleClick() {
    startConversation.mutate(tutorId, {
      onSuccess: (conversation) => {
        void navigate(paths.messages.conversationDetail(conversation.conversationId));
      },
      onError: () => {
        notify({ message: "Couldn't start a conversation. Please try again.", severity: "error" });
      },
    });
  }

  return (
    <Button
      variant="outlined"
      startIcon={<ChatBubbleOutlineRoundedIcon />}
      onClick={handleClick}
      disabled={startConversation.isPending}
    >
      Message Tutor
    </Button>
  );
}

/**
 * The post-submit result of `POST /sessions` (`useBookSession`) — same
 * mutation, just a clearer success/failure presentation than the old
 * inline text. A 409 (the Availability Slot was booked by someone else
 * between selection and submit) gets its own recovery UI instead of the
 * raw Domain Error text — every other failure keeps the underlying,
 * already-human-readable Domain Error message (ADR-008) rather than a
 * second, invented wording layered on top of it.
 *
 * Phase 5 (Booking Experience) PART 8: the success view now explains what
 * happens next (the Tutor is notified; join details appear on the session
 * once ready) and surfaces three shortcuts alongside the existing "View
 * this session" — My Lessons, Message Tutor (only when the Tutor id is
 * known), and Return Home — every one of them an existing route/capability,
 * nothing new invented.
 */
export function BookingStatusBanner(props: BookingStatusBannerProps) {
  if (props.status === "error") {
    const isSlotConflict = isConflictError(props.error);

    return (
      <Card variant="outlined" sx={{ borderColor: "error.main" }}>
        <CardContent>
          {isSlotConflict ? (
            <Stack spacing={2} alignItems="flex-start">
              <Typography variant="subtitle1" fontWeight={600}>
                This time was just booked by another student
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Someone booked this lesson time moments before you did. Please choose another time.
              </Typography>
              <Stack direction="row" spacing={1.5} flexWrap="wrap">
                {props.onChooseAnotherTime ? (
                  <Button variant="contained" onClick={props.onChooseAnotherTime}>
                    Choose another time
                  </Button>
                ) : null}
                {props.tutorId ? (
                  <Button
                    component={RouterLink}
                    to={paths.identity.tutorDetail(props.tutorId)}
                    variant="outlined"
                  >
                    Back to tutor
                  </Button>
                ) : null}
              </Stack>
            </Stack>
          ) : (
            <ErrorState error={props.error} onRetry={props.onRetry} title="Booking failed" />
          )}
        </CardContent>
      </Card>
    );
  }

  const { session, tutorId, tutorName } = props;

  return (
    <Card variant="outlined" sx={{ borderColor: "success.main" }}>
      <CardContent>
        <Stack spacing={2.5} alignItems="flex-start">
          <Stack direction="row" spacing={1.5} alignItems="center">
            <CheckCircleRoundedIcon color="success" sx={{ fontSize: 36 }} aria-hidden="true" />
            <Typography variant="h5" component="h2" fontWeight={700}>
              Your lesson is booked!
            </Typography>
          </Stack>
          <Typography variant="body1" color="text.secondary">
            {toTehranDisplay(session.scheduledTimeUtc)} – {toTehranDisplay(session.endTimeUtc)} (Tehran).
            Your tutor has been notified — join details will appear on the lesson once it&rsquo;s ready.
          </Typography>
          <CopyableId id={session.sessionId} />
          <Stack direction="row" spacing={1.5} flexWrap="wrap">
            <Button
              component={RouterLink}
              to={paths.scheduling.sessionDetail(session.sessionId)}
              variant="contained"
            >
              View Lesson
            </Button>
            <Button
              component={RouterLink}
              to={paths.scheduling.studentScheduleBase}
              variant="outlined"
              startIcon={<CalendarMonthRoundedIcon />}
            >
              My Lessons
            </Button>
            <AddToCalendarButton session={session} tutorName={tutorName} />
            {tutorId ? <MessageTutorAction tutorId={tutorId} /> : null}
            <Button component={RouterLink} to={paths.home} variant="text" startIcon={<HomeRoundedIcon />}>
              Return Home
            </Button>
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
}
