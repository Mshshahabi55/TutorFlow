import { Avatar, Card, CardActionArea, CardContent, Stack, Typography, alpha } from "@mui/material";
import PersonRoundedIcon from "@mui/icons-material/PersonRounded";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { monoFontFamily } from "@/app/theme";
import type { ConversationDto, TutorDto } from "@/services/api/dtos";

export interface ConversationListItemProps {
  conversation: ConversationDto;
  onOpen: (conversation: ConversationDto) => void;
  /**
   * Resolved separately by the caller (`InboxPage`, one batched
   * `useTutorsByIds` call across the whole visible list — never a per-row
   * fetch here, the N+1 pattern this component's own history already
   * ruled out). Only ever set when the other participant genuinely is a
   * Tutor (the common case for a Student/Parent-Guardian's own Inbox,
   * since only they may start a Conversation with one, ADR-022) — a
   * Student/Parent-Guardian/Admin-Staff participant has no name field
   * anywhere in this API, so this stays undefined and the row falls back
   * to the raw id, same as before.
   */
  otherParticipantTutor?: TutorDto;
}

/** One Conversation, as a card. */
export function ConversationListItem({ conversation, onOpen, otherParticipantTutor }: ConversationListItemProps) {
  const hasUnread = conversation.unreadCount > 0;
  const timestamp = conversation.lastMessageAtUtc ?? conversation.createdAtUtc;

  return (
    <Card variant="outlined">
      <CardActionArea onClick={() => onOpen(conversation)}>
        <CardContent>
          <Stack spacing={1}>
            <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}>
              {otherParticipantTutor ? (
                <Stack direction="row" spacing={1.5} alignItems="center" minWidth={0}>
                  <Avatar
                    src={otherParticipantTutor.photoUrl ?? undefined}
                    sx={{
                      width: 32,
                      height: 32,
                      bgcolor: (t) => alpha(t.palette.primary.main, t.palette.mode === "dark" ? 0.22 : 0.12),
                      color: "primary.main",
                    }}
                  >
                    <PersonRoundedIcon fontSize="small" aria-hidden="true" />
                  </Avatar>
                  <Typography variant="subtitle1" fontWeight={hasUnread ? 700 : 600} noWrap>
                    {otherParticipantTutor.displayName ?? otherParticipantTutor.subject ?? "Tutor"}
                  </Typography>
                </Stack>
              ) : (
                <Typography
                  variant="subtitle1"
                  fontWeight={hasUnread ? 700 : 600}
                  fontFamily={monoFontFamily}
                  sx={{ wordBreak: "break-all" }}
                >
                  {conversation.otherParticipantId}
                </Typography>
              )}
              {hasUnread ? (
                <StatusPill label={`${conversation.unreadCount} unread`} tone="info" />
              ) : null}
            </Stack>
            <Typography
              variant="body2"
              color="text.secondary"
              fontWeight={hasUnread ? 600 : 400}
              sx={{
                overflow: "hidden",
                textOverflow: "ellipsis",
                display: "-webkit-box",
                WebkitLineClamp: 1,
                WebkitBoxOrient: "vertical",
              }}
            >
              {conversation.lastMessagePreview ?? "No messages yet"}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {toTehranDisplay(timestamp)}
            </Typography>
          </Stack>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}
