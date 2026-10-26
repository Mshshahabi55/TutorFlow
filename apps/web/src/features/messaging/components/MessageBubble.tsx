import { MessageDto } from '../types';
import styles from './MessageBubble.module.css';

function formatTimestamp(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

export interface MessageBubbleProps {
  message: MessageDto;
  /** True when the authenticated caller sent this message — never derived from role, since a
   * dual-role account could be either participant depending on the conversation. */
  isOwnMessage: boolean;
}

export function MessageBubble({ message, isOwnMessage }: MessageBubbleProps) {
  return (
    <div className={[styles.row, isOwnMessage ? styles.own : styles.other].join(' ')}>
      <div className={styles.bubble}>
        <p className={styles.body}>{message.body}</p>
        <span className={styles.timestamp}>{formatTimestamp(message.createdAtUtc)}</span>
      </div>
    </div>
  );
}
