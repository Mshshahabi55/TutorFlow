'use client';

import { FormEvent, useId, useState } from 'react';
import { Button } from '@/components/ui/Button';
import { Textarea } from '@/components/ui/Textarea';
import { ApiError } from '@/lib/api/client';
import styles from './MessageComposer.module.css';

const MAX_LENGTH = 5000;

function composerErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.problem.detail ?? error.problem.title ?? 'Could not send your message. Please try again.';
  }
  return 'Could not send your message. Please try again.';
}

export interface MessageComposerProps {
  isSending: boolean;
  sendError: unknown;
  onSend: (text: string) => void;
}

/** Validation mirrors `SendMessage.Validator` exactly (ADR-0030 Batch 69): non-empty after
 * trimming whitespace, at most 5000 characters — never looser or stricter than the backend. */
export function MessageComposer({ isSending, sendError, onSend }: MessageComposerProps) {
  const [text, setText] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);
  const textareaId = useId();
  const errorId = `${textareaId}-error`;

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const trimmed = text.trim();
    if (!trimmed) {
      setValidationError('Message must not be empty.');
      return;
    }
    if (trimmed.length > MAX_LENGTH) {
      setValidationError(`Message must be ${MAX_LENGTH} characters or fewer.`);
      return;
    }
    setValidationError(null);
    onSend(trimmed);
    setText('');
  };

  const displayedError = validationError ?? (sendError ? composerErrorMessage(sendError) : null);

  return (
    <form className={styles.form} onSubmit={handleSubmit}>
      <label htmlFor={textareaId} className={styles.visuallyHidden}>
        Message
      </label>
      <Textarea
        id={textareaId}
        value={text}
        onChange={(event) => {
          setText(event.target.value);
          setValidationError(null);
        }}
        rows={2}
        maxLength={MAX_LENGTH}
        placeholder="Write a message…"
        disabled={isSending}
        aria-invalid={Boolean(displayedError)}
        aria-describedby={displayedError ? errorId : undefined}
      />
      {displayedError && (
        <span id={errorId} role="alert" className={styles.error}>
          {displayedError}
        </span>
      )}
      <Button type="submit" loading={isSending} disabled={isSending}>
        Send
      </Button>
    </form>
  );
}
