'use client';

import { useEffect, useRef } from 'react';
import Link from 'next/link';
import { useQueryClient } from '@tanstack/react-query';
import { PageContainer } from '@/components/layout/PageContainer';
import { Card } from '@/components/ui/Card';
import { Avatar } from '@/components/ui/Avatar';
import { ButtonLink } from '@/components/ui/ButtonLink';
import { Button } from '@/components/ui/Button';
import { LoadingState } from '@/components/ui/LoadingState';
import { EmptyState } from '@/components/ui/EmptyState';
import { ErrorState } from '@/components/ui/ErrorState';
import { ApiError } from '@/lib/api/client';
import { useAuth } from '@/features/auth/useAuth';
import { resolveMediaUrl } from '@/features/tutor-profile/mediaUrl';
import { conversationsQueryKey } from '@/features/messaging/useConversations';
import { useIsDocumentVisible, useMarkConversationRead, useMessages, useSendMessage } from '@/features/messaging/useMessages';
import { ConversationSummary } from '@/features/messaging/types';
import { MessageBubble } from '@/features/messaging/components/MessageBubble';
import { MessageComposer } from '@/features/messaging/components/MessageComposer';
import styles from './ConversationThreadView.module.css';

export interface ConversationThreadViewProps {
  conversationId: string;
}

export function ConversationThreadView({ conversationId }: ConversationThreadViewProps) {
  const { isAuthenticated, isLoading: authLoading, userId, roles } = useAuth();
  const queryClient = useQueryClient();
  const isVisible = useIsDocumentVisible();

  const { tutor, messages, isLoading, isError, error, refetch, hasOlder, isLoadingOlder, loadOlder } = useMessages(
    conversationId,
    isAuthenticated,
  );
  const sendMessage = useSendMessage(conversationId);
  const markRead = useMarkConversationRead(conversationId);

  const wasVisibleRef = useRef(false);
  useEffect(() => {
    if (!isAuthenticated) {
      return;
    }
    if (isVisible && !wasVisibleRef.current) {
      wasVisibleRef.current = true;
      markRead.mutate();
    } else if (!isVisible) {
      wasVisibleRef.current = false;
    }
    // markRead is a fresh mutation object every render; only the mount+visibility transition
    // should trigger a call, never a re-render of the mutation itself (avoids a mutation loop).
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAuthenticated, isVisible, conversationId]);

  if (authLoading) {
    return (
      <PageContainer>
        <LoadingState label="Loading…" />
      </PageContainer>
    );
  }

  if (!isAuthenticated) {
    return (
      <PageContainer className={styles.intro}>
        <Card className={styles.card}>
          <h1>Messages</h1>
          <p>Sign in to view this conversation.</p>
          <div className={styles.actions}>
            <ButtonLink href={`/signup?returnTo=${encodeURIComponent(`/messages/${conversationId}`)}`}>Sign up</ButtonLink>
            <ButtonLink href={`/login?returnTo=${encodeURIComponent(`/messages/${conversationId}`)}`} variant="secondary">
              Log in
            </ButtonLink>
          </div>
        </Card>
      </PageContainer>
    );
  }

  if (isLoading) {
    return (
      <PageContainer>
        <LoadingState label="Loading conversation…" />
      </PageContainer>
    );
  }

  if (isError) {
    const isNotFound = error instanceof ApiError && error.status === 404;
    return (
      <PageContainer>
        {isNotFound ? (
          // Uniform inaccessible state (ADR-0030) — the backend never distinguishes "no such
          // conversation" from "exists but isn't yours," so neither does this message.
          <EmptyState
            title="This conversation isn't available"
            description="It may not exist, or you may not have access to it."
            action={<ButtonLink href="/messages">Back to messages</ButtonLink>}
          />
        ) : (
          <ErrorState
            title="We couldn't load this conversation"
            description="Please try again."
            onRetry={() => refetch()}
          />
        )}
      </PageContainer>
    );
  }

  // The GetMessages response deliberately omits raw participant IDs (ADR-0030) — this reuses the
  // already-fetched ListConversations cache (which has them) to tell which side the caller is on.
  // Falls back to a roles-based guess for a direct deep link with no prior list fetch; a cosmetic
  // edge case only (see completion report), never a server-side access concern.
  const cachedConversations = queryClient.getQueryData<ConversationSummary[]>(conversationsQueryKey);
  const cachedConversation = cachedConversations?.find((c) => c.id === conversationId);
  const isViewerStudent = cachedConversation ? cachedConversation.studentUserId === userId : roles.includes('Student');

  const headerName = isViewerStudent ? (tutor?.displayName ?? 'Tutor') : 'Student';
  const headerAvatarSrc = isViewerStudent ? resolveMediaUrl(tutor?.profilePhotoReference) : null;

  return (
    <PageContainer className={styles.page}>
      <nav className={styles.breadcrumb} aria-label="Breadcrumb">
        <Link href="/messages">Messages</Link>
      </nav>

      <div className={styles.header}>
        <Avatar src={headerAvatarSrc} name={headerName} />
        <h1 className={styles.headerName}>{headerName}</h1>
      </div>

      <div className={styles.thread}>
        {hasOlder && (
          <div className={styles.loadOlder}>
            <Button variant="secondary" size="sm" onClick={loadOlder} loading={isLoadingOlder}>
              Load older messages
            </Button>
          </div>
        )}

        {messages.length === 0 ? (
          <EmptyState title="No messages yet" description="Send a message to start the conversation." />
        ) : (
          messages.map((message) => (
            <MessageBubble key={message.id} message={message} isOwnMessage={message.senderUserId === userId} />
          ))
        )}
      </div>

      <MessageComposer
        isSending={sendMessage.isPending}
        sendError={sendMessage.error}
        onSend={(text) => sendMessage.mutate({ text })}
      />
    </PageContainer>
  );
}
