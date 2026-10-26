'use client';

import { PageContainer } from '@/components/layout/PageContainer';
import { Card } from '@/components/ui/Card';
import { ButtonLink } from '@/components/ui/ButtonLink';
import { LoadingState } from '@/components/ui/LoadingState';
import { EmptyState } from '@/components/ui/EmptyState';
import { ErrorState } from '@/components/ui/ErrorState';
import { useAuth } from '@/features/auth/useAuth';
import { useConversations } from '@/features/messaging/useConversations';
import { ConversationCard } from '@/features/messaging/components/ConversationCard';
import styles from './ConversationListView.module.css';

export function ConversationListView() {
  const { isAuthenticated, isLoading: authLoading, userId, roles } = useAuth();
  const { data, isLoading, isError, refetch } = useConversations(isAuthenticated);
  const isStudent = roles.includes('Student');

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
          <p>Sign in to see your conversations.</p>
          <div className={styles.actions}>
            <ButtonLink href="/signup?returnTo=%2Fmessages">Sign up</ButtonLink>
            <ButtonLink href="/login?returnTo=%2Fmessages" variant="secondary">
              Log in
            </ButtonLink>
          </div>
        </Card>
      </PageContainer>
    );
  }

  return (
    <PageContainer className={styles.page}>
      <h1 className={styles.title}>Messages</h1>

      {isLoading ? (
        <LoadingState label="Loading your conversations…" />
      ) : isError ? (
        <ErrorState
          title="We couldn't load your conversations"
          description="Please try again."
          onRetry={() => refetch()}
        />
      ) : data && data.length === 0 ? (
        <EmptyState
          title="No conversations yet"
          description={
            isStudent
              ? 'Message a tutor from their profile to start a conversation.'
              : "When a student messages you, it'll show up here."
          }
          action={isStudent ? <ButtonLink href="/find-tutors">Find a tutor</ButtonLink> : undefined}
        />
      ) : (
        <div className={styles.list}>
          {data?.map((conversation) => (
            <ConversationCard key={conversation.id} conversation={conversation} currentUserId={userId!} />
          ))}
        </div>
      )}
    </PageContainer>
  );
}
