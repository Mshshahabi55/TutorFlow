import { Card, CardActionArea, CardContent, Stack, Typography } from "@mui/material";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { monoFontFamily } from "@/app/theme";
import type { ConversationDto } from "@/services/api/dtos";

export interface ConversationListItemProps {
  conversation: ConversationDto;
  onOpen: (conversation: ConversationDto) => void;
}

/**
 * One Conversation, as a card — mirrors `SessionCard`'s own convention of
 * showing the other party's raw id in mono font rather than fetching a
 * name per row (a per-row cross-context lookup here would be the same
 * N+1 pattern that file's own comment already rules out). `ConversationDetailPage`,
 * a single page rather than a list, is where a one-time enrichment lookup
 * is worth doing instead.
 */
export function ConversationListItem({ conversation, onOpen }: ConversationListItemProps) {
  const hasUnread = conversation.unreadCount > 0;
  const timestamp = conversation.lastMessageAtUtc ?? conversation.createdAtUtc;

  return (
    <Card variant="outlined">
      <CardActionArea onClick={() => onOpen(conversation)}>
        <CardContent>
          <Stack spacing={1}>
            <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}>
              <Typography
                variant="subtitle1"
                fontWeight={hasUnread ? 700 : 600}
                fontFamily={monoFontFamily}
                sx={{ wordBreak: "break-all" }}
              >
                {conversation.otherParticipantId}
              </Typography>
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
