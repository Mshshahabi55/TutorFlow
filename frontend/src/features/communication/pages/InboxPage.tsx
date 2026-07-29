import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { InputAdornment, Stack, TextField, Typography } from "@mui/material";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import { useMyConversations } from "@/features/communication/hooks/useConversationQueries";
import { ConversationListItem } from "@/features/communication/components/ConversationListItem";
import { ConversationListItemSkeleton } from "@/features/communication/components/ConversationListItemSkeleton";
import { PageHeader } from "@/shared/components/PageHeader";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { paths } from "@/routes/paths";
import type { ConversationDto } from "@/services/api/dtos";

/**
 * No name/subject exists per Conversation to search by (ConversationDto
 * carries only ids — see ConversationListItem's own comment) — the one
 * honest, real field to filter on is the last message's own text, so
 * "Search" (RC5.1 Step 2) filters by `lastMessagePreview`, not a fabricated
 * participant name.
 */
function filterConversations(conversations: ConversationDto[], query: string): ConversationDto[] {
  const normalized = query.trim().toLowerCase();
  if (!normalized) {
    return conversations;
  }

  return conversations.filter((conversation) =>
    conversation.lastMessagePreview?.toLowerCase().includes(normalized),
  );
}

export function InboxPage() {
  const navigate = useNavigate();
  const [searchQuery, setSearchQuery] = useState("");
  const conversationsQuery = useMyConversations();

  const filtered = useMemo(
    () => (conversationsQuery.isSuccess ? filterConversations(conversationsQuery.data, searchQuery) : []),
    [conversationsQuery.isSuccess, conversationsQuery.data, searchQuery],
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
          placeholder="Search messages…"
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
            />
          ))}
        </Stack>
      )}
    </Stack>
  );
}
