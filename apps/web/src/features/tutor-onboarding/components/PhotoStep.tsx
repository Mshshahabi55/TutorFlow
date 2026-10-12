'use client';

import { useState } from 'react';
import { FormField } from '@/components/ui/FormField';
import { Input } from '@/components/ui/Input';
import { Avatar } from '@/components/ui/Avatar';
import { TutorApplication, UpdateDraftCommand } from '../types';
import { StepShell } from './StepShell';
import styles from './PhotoStep.module.css';

export interface PhotoStepProps {
  application: TutorApplication;
  onContinue: (patch: UpdateDraftCommand) => void;
  onBack: () => void;
  isSaving: boolean;
  error: string | null;
}

/** A URL-reference input, not a file-upload control (ADR-0026 §6) — no object-storage
 * abstraction exists anywhere in this codebase yet (confirmed: `mediaUrl.ts`'s own comment).
 * Honest about that boundary rather than faking an upload against local disk. Required at
 * submission (ADR-0026 §3, corrected per the 2026-10-05 independent audit: Preply's own sourced
 * guidance marks the profile photo as required). */
export function PhotoStep({ application, onContinue, onBack, isSaving, error }: PhotoStepProps) {
  const [photoUrl, setPhotoUrl] = useState(application.profilePhotoReference ?? '');

  return (
    <StepShell
      title="Profile photo"
      description="A professional, friendly photo is one of the strongest trust signals on your profile."
      onBack={onBack}
      onContinue={() => onContinue({ profilePhotoReference: photoUrl })}
      continueDisabled={!photoUrl.trim()}
      isSaving={isSaving}
      error={error}
    >
      <div className={styles.preview}>
        <Avatar src={/^https?:\/\//i.test(photoUrl) ? photoUrl : null} name={application.displayName ?? '?'} size="lg" />
      </div>

      <FormField
        label="Photo URL"
        required
        helperText="Paste a link to an already-hosted photo — TutorFlow doesn't have its own photo upload yet."
      >
        {(props) => (
          <Input
            {...props}
            type="url"
            placeholder="https://…"
            value={photoUrl}
            onChange={(e) => setPhotoUrl(e.target.value)}
          />
        )}
      </FormField>
    </StepShell>
  );
}
