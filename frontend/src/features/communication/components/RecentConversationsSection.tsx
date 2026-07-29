import { Link as RouterLink, useNavigate } from "react-router-dom";
import { Button, Stack } from "@mui/material";
import { useMyConversations } from "@/features/communication/hooks/useConversationQueries";
import { ConversationListItem } from "@/features/communication/components/ConversationListItem";
import { ConversationListItemSkeleton } from "@/features/communication/components/ConversationListItemSkeleton";
import { SectionCard } from "@/shared/components/SectionCard";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { paths } from "@/routes/paths";
import type { ConversationDto } from "@/services/api/dtos";

const RECENT_CONVERSATIONS_LIMIT = 3;

/**
 * RC5.1 Steps 5-7: Student/Tutor/Parent-Guardian Dashboards each get the
 * same "recent conversations + unread badge" widget — there is nothing
 * role-specific about *whose* conversations these are (every role sees
 * only its own, via GET /conversations/mine), so one shared component
 * covers all three rather than three near-identical copies.
 */
export function RecentConversationsSection() {
  const navigate = useNavigate();
  const conversationsQuery = useMyConversations();

  const conversations = conversationsQuery.data ?? [];
  const totalUnread = conversations.reduce((sum, conversation) => sum + conversation.unreadCount, 0);
  const recent = conversations.slice(0, RECENT_CONVERSATIONS_LIMIT);

  function openConversation(conversation: ConversationDto) {
    void navigate(paths.messages.conversationDetail(conversation.conversationId));
  }

  return (
    <SectionCard
      title="Messages"
      action={
        <Stack direction="row" spacing={1} alignItems="center">
          {totalUnread > 0 ? <StatusPill label={`${totalUnread} unread`} tone="info" /> : null}
          <Button component={RouterLink} to={paths.messages.inbox} size="small">
            View all
          </Button>
        </Stack>
      }
    >
      {/* RC4.4: isLoading (isPending && isFetching), not isPending alone —
          useMyConversations is enabled: isAuthenticated, so a disabled
          query is permanently "pending" with no data; isLoading is false
          for a disabled query, letting an unauthenticated viewer fall
          straight through to the same honest empty state below instead of
          an unending skeleton. */}
      {conversationsQuery.isLoading ? (
        <Stack spacing={2}>
          {Array.from({ length: 2 }, (_, index) => (
            <ConversationListItemSkeleton key={index} />
          ))}
        </Stack>
      ) : conversationsQuery.isError ? (
        <ErrorState error={conversationsQuery.error} onRetry={() => void conversationsQuery.refetch()} />
      ) : conversations.length === 0 ? (
        <EmptyState
          title="No conversations yet"
          description="Messages will show up here once you start a conversation."
        />
      ) : (
        <Stack spacing={2}>
          {recent.map((conversation) => (
            <ConversationListItem
              key={conversation.conversationId}
              conversation={conversation}
              onOpen={openConversation}
            />
          ))}
        </Stack>
      )}
    </SectionCard>
  );
}
