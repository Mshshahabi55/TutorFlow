import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { InputAdornment, Stack, TextField, Typography } from "@mui/material";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import { useMyConversations } from "@/features/communication/hooks/useConversationQueries";
import { useTutorsByIds } from "@/features/identity/hooks/useTutorQueries";
import { ConversationListItem } from "@/features/communication/components/ConversationListItem";
import { ConversationListItemSkeleton } from "@/features/communication/components/ConversationListItemSkeleton";
import { PageHeader } from "@/shared/components/PageHeader";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { paths } from "@/routes/paths";
import type { ConversationDto, TutorDto } from "@/services/api/dtos";

/**
 * `ConversationDto` itself carries only ids, no name — filters on the last
 * message's own text always, plus the other participant's resolved Tutor
 * name/headline when one was found (`tutorById`, see `ConversationListItem`'s
 * own comment on why that lookup is best-effort/Tutor-only). A Conversation
 * whose other participant never resolved to a Tutor still filters correctly
 * by message text alone — this never regresses to matching nothing.
 */
function filterConversations(
  conversations: ConversationDto[],
  query: string,
  tutorById: Map<string, TutorDto>,
): ConversationDto[] {
  const normalized = query.trim().toLowerCase();
  if (!normalized) {
    return conversations;
  }

  return conversations.filter((conversation) => {
    if (conversation.lastMessagePreview?.toLowerCase().includes(normalized)) {
      return true;
    }
    const tutor = tutorById.get(conversation.otherParticipantId);
    return Boolean(
      tutor && [tutor.displayName, tutor.headline, tutor.subject].some((field) => field?.toLowerCase().includes(normalized)),
    );
  });
}

export function InboxPage() {
  const navigate = useNavigate();
  const [searchQuery, setSearchQuery] = useState("");
  const conversationsQuery = useMyConversations();

  // Best-effort enrichment, not authoritative: an id that doesn't resolve
  // to a Tutor (a Student/Parent-Guardian/Admin-Staff participant, or one
  // simply still loading) just leaves that row on the existing raw-id
  // fallback — see ConversationListItem's own comment on why this is a
  // batched, bounded lookup rather than N per-row fetches.
  const otherParticipantIds = useMemo(
    () => [...new Set((conversationsQuery.data ?? []).map((c) => c.otherParticipantId))],
    [conversationsQuery.data],
  );
  const tutorQueries = useTutorsByIds(otherParticipantIds);
  const tutorById = useMemo(() => {
    const map = new Map<string, TutorDto>();
    otherParticipantIds.forEach((id, index) => {
      const query = tutorQueries[index];
      if (query?.isSuccess && query.data) {
        map.set(id, query.data);
      }
    });
    return map;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [otherParticipantIds, tutorQueries.map((q) => q.dataUpdatedAt).join(",")]);

  const filtered = useMemo(
    () =>
      conversationsQuery.isSuccess
        ? filterConversations(conversationsQuery.data, searchQuery, tutorById)
        : [],
    [conversationsQuery.isSuccess, conversationsQuery.data, searchQuery, tutorById],
  );

  function openConversation(conversation: ConversationDto) {
    void navigate(paths.messages.conversationDetail(conversation.conversationId));
  }

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Messages"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Your conversations, most recently active first.
          </Typography>
        }
      />

      {conversationsQuery.isSuccess && conversationsQuery.data.length > 0 ? (
        <TextField
          placeholder="Search messages or tutor names…"
          value={searchQuery}
          onChange={(event) => setSearchQuery(event.target.value)}
          size="small"
          fullWidth
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchRoundedIcon fontSize="small" color="disabled" />
                </InputAdornment>
              ),
            },
            htmlInput: { "aria-label": "Search messages" },
          }}
        />
      ) : null}

      {/* RC4.4: isLoading, not isPending — useMyConversations is
          enabled: isAuthenticated, so a disabled query (no real session)
          stays "pending" forever with no data; isLoading is false for a
          disabled query, so an unauthenticated viewer falls straight
          through to the honest empty state below instead of an unending
          skeleton. */}
      {conversationsQuery.isLoading ? (
        <Stack spacing={2}>
          {Array.from({ length: 3 }, (_, index) => (
            <ConversationListItemSkeleton key={index} />
          ))}
        </Stack>
      ) : conversationsQuery.isError ? (
        <ErrorState error={conversationsQuery.error} onRetry={() => void conversationsQuery.refetch()} />
      ) : (conversationsQuery.data ?? []).length === 0 ? (
        <EmptyState
          title="No conversations yet"
          description="Once you start messaging a tutor, student, or parent/guardian, your conversations will show up here."
        />
      ) : filtered.length === 0 ? (
        <EmptyState
          title="No matching conversations"
          description="Try a different search term."
        />
      ) : (
        <Stack spacing={2} role="list" aria-label="Conversations">
          {filtered.map((conversation) => (
            <ConversationListItem
              key={conversation.conversationId}
              conversation={conversation}
              onOpen={openConversation}
              otherParticipantTutor={tutorById.get(conversation.otherParticipantId)}
            />
          ))}
        </Stack>
      )}
    </Stack>
  );
}
