'use client';

import { useState } from 'react';
import { FormField } from '@/components/ui/FormField';
import { Input } from '@/components/ui/Input';
import { TutorApplication, UpdateDraftCommand } from '../types';
import { StepShell } from './StepShell';

export interface VideoStepProps {
  application: TutorApplication;
  onContinue: (patch: UpdateDraftCommand) => void;
  onBack: () => void;
  isSaving: boolean;
  error: string | null;
}

/** URL reference, same boundary as `PhotoStep` (ADR-0026 §6). The "~2 minutes" guidance
 * (benchmark-confirmed) is content advice, not a hard validation rule — not invented as one. */
export function VideoStep({ application, onContinue, onBack, isSaving, error }: VideoStepProps) {
  const [videoUrl, setVideoUrl] = useState(application.introVideoReference ?? '');

  return (
    <StepShell
      title="Intro video"
      description="A short video (most tutors keep it under ~2 minutes) introducing yourself and your teaching style."
      onBack={onBack}
      onContinue={() => onContinue({ introVideoReference: videoUrl })}
      isSaving={isSaving}
      error={error}
    >
      <FormField
        label="Video URL"
        helperText="Paste a link to an already-hosted video — TutorFlow doesn't have its own video upload yet."
      >
        {(props) => (
          <Input
            {...props}
            type="url"
            placeholder="https://…"
            value={videoUrl}
            onChange={(e) => setVideoUrl(e.target.value)}
          />
        )}
      </FormField>
    </StepShell>
  );
}
