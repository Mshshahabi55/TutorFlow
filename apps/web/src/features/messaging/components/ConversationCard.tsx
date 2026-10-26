import Link from 'next/link';
import { Card } from '@/components/ui/Card';
import { Avatar } from '@/components/ui/Avatar';
import { resolveMediaUrl } from '@/features/tutor-profile/mediaUrl';
import { ConversationSummary } from '../types';
import styles from './ConversationCard.module.css';

function formatLastActivity(timestamp: string | null): string {
  if (!timestamp) {
    return 'No messages yet';
  }
  const date = new Date(timestamp);
  const isToday = date.toDateString() === new Date().toDateString();
  return isToday
    ? date.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' })
    : date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}

export interface ConversationCardProps {
  conversation: ConversationSummary;
  /** The authenticated caller's own user ID — determines which side of this conversation they
   * are, since a dual-role (Student + Tutor) account can appear as either across different
   * conversations (ADR-0002). */
  currentUserId: string;
}

/** A Student always sees the Tutor's real display identity; a Tutor always sees the generic
 * "Student" label — TutorFlow has no mechanism anywhere to resolve a student's public display
 * name, and fabricating one was rejected (docs/product/preply-reverse-engineering/
 * 12-classroom-messaging-reviews-notifications.md). */
export function ConversationCard({ conversation, currentUserId }: ConversationCardProps) {
  const isViewerStudent = conversation.studentUserId === currentUserId;
  const name = isViewerStudent ? (conversation.tutor?.displayName ?? 'Tutor') : 'Student';
  const avatarSrc = isViewerStudent ? resolveMediaUrl(conversation.tutor?.profilePhotoReference) : null;

  return (
    <Link href={`/messages/${conversation.id}`} className={styles.link}>
      <Card interactive className={styles.card}>
        <Avatar src={avatarSrc} name={name} />
        <div className={styles.body}>
          <span className={styles.name}>{name}</span>
          <span className={styles.timestamp}>{formatLastActivity(conversation.lastMessageAtUtc)}</span>
        </div>
      </Card>
    </Link>
  );
}
