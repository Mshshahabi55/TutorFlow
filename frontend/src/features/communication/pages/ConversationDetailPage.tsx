import { useEffect, useRef } from "react";
import { Link as RouterLink, useParams } from "react-router-dom";
import { Avatar, Box, IconButton, Skeleton, Stack, Typography, alpha } from "@mui/material";
import PersonRoundedIcon from "@mui/icons-material/PersonRounded";
import ArrowBackRoundedIcon from "@mui/icons-material/ArrowBackRounded";
import { useMyConversations, useConversationMessages } from "@/features/communication/hooks/useConversationQueries";
import {
  useMarkConversationRead,
  useSendMessage,
} from "@/features/communication/hooks/useConversationMutations";
import { useTutor } from "@/features/identity/hooks/useTutorQueries";
import { MessageBubble } from "@/features/communication/components/MessageBubble";
import { MessageComposer } from "@/features/communication/components/MessageComposer";
import { TypingPlaceholder } from "@/features/communication/components/TypingPlaceholder";
import { JoinLessonBanner } from "@/features/meetings/components/JoinLessonBanner";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { useAuth } from "@/shared/hooks/useAuth";
import { paths } from "@/routes/paths";
import { monoFontFamily } from "@/app/theme";

/**
 * A single lookup, once per page — mirrors `SessionDetailPage`'s own
 * "one enrichment fetch per detail page" convention, not
 * `ConversationListItem`'s deliberate no-fetch-per-row choice.
 * GET /tutors/{id} is public (Discovery is Public — no 403 possible), so a
 * non-Tutor id here just resolves to "not found" and falls back to the raw
 * id — never an alarming error banner for what is, for a Tutor/Admin
 * conversation partner, an expected outcome. displayName/photoUrl (ADR-024)
 * take priority over the old subject-as-name stand-in, same preference
 * order `TutorCard`/`TutorProfileHero`/`ConversationListItem` already use.
 */
function ConversationHeading({ otherParticipantId }: { otherParticipantId: string }) {
  const tutorQuery = useTutor(otherParticipantId);

  if (tutorQuery.isPending) {
    return <Skeleton variant="text" width={160} height={32} />;
  }

  if (tutorQuery.isSuccess) {
    return (
      <Stack direction="row" spacing={1.5} alignItems="center">
        <Avatar
          src={tutorQuery.data.photoUrl ?? undefined}
          sx={{
            width: 40,
            height: 40,
            bgcolor: (t) => alpha(t.palette.primary.main, t.palette.mode === "dark" ? 0.22 : 0.12),
            color: "primary.main",
          }}
        >
          <PersonRoundedIcon fontSize="small" aria-hidden="true" />
        </Avatar>
        <Typography variant="h5" component="h1">
          {tutorQuery.data.displayName ?? tutorQuery.data.subject ?? "Tutor"}
        </Typography>
      </Stack>
    );
  }

  return (
    <Typography variant="h5" component="h1" fontFamily={monoFontFamily} sx={{ wordBreak: "break-all" }}>
      {otherParticipantId}
    </Typography>
  );
}

export function ConversationDetailPage() {
  const { conversationId } = useParams<{ conversationId: string }>();
  const { user } = useAuth();
  const conversationsQuery = useMyConversations();
  const messagesQuery = useConversationMessages(conversationId);
  const markRead = useMarkConversationRead(conversationId ?? "");
  const sendMessage = useSendMessage(conversationId ?? "");
  const bottomRef = useRef<HTMLDivElement>(null);
  const markedReadFor = useRef<string | undefined>(undefined);

  const conversation = conversationsQuery.data?.find((c) => c.conversationId === conversationId);

  useEffect(() => {
    if (conversationId && markedReadFor.current !== conversationId) {
      markedReadFor.current = conversationId;
      markRead.mutate();
    }
    // Only re-run when the conversation identity changes, not on every mark-read mutation re-render.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [conversationId]);

  useEffect(() => {
    // jsdom (used in tests) has no scrollIntoView implementation at all.
    bottomRef.current?.scrollIntoView?.({ block: "end" });
  }, [messagesQuery.data?.length]);

  if (!conversationId) {
    return <ErrorState error={new Error("No conversation was specified.")} />;
  }

  return (
    <Stack spacing={2} sx={{ height: { md: "calc(100vh - 200px)" } }}>
      <Stack direction="row" spacing={1} alignItems="center">
        <IconButton component={RouterLink} to={paths.messages.inbox} aria-label="Back to Messages" size="small">
          <ArrowBackRoundedIcon />
        </IconButton>
        <ConversationHeading otherParticipantId={conversation?.otherParticipantId ?? conversationId} />
      </Stack>

      <JoinLessonBanner conversationId={conversationId} />

      <Box
        sx={{
          flex: 1,
          minHeight: 320,
          overflowY: "auto",
          border: "1px solid",
          borderColor: "divider",
          borderRadius: 1,
          p: 2,
        }}
      >
        {messagesQuery.isPending ? (
          <Stack spacing={2}>
            {Array.from({ length: 4 }, (_, index) => (
              <Skeleton key={index} variant="rounded" height={48} width="60%" sx={{ ml: index % 2 ? "auto" : 0 }} />
            ))}
          </Stack>
        ) : messagesQuery.isError ? (
          <ErrorState error={messagesQuery.error} onRetry={() => void messagesQuery.refetch()} />
        ) : messagesQuery.data.length === 0 ? (
          <EmptyState
            title="No messages yet"
            description="Send the first message to get the conversation started."
          />
        ) : (
          <Stack spacing={1.5} role="list" aria-label="Messages">
            {messagesQuery.data.map((message) => (
              <MessageBubble
                key={message.messageId}
                message={message}
                isOwn={message.senderId === user?.accountId}
              />
            ))}
            <TypingPlaceholder />
            <div ref={bottomRef} />
          </Stack>
        )}
      </Box>

      <MessageComposer
        onSend={(body) => sendMessage.mutate(body)}
        isSending={sendMessage.isPending}
      />
    </Stack>
  );
}
