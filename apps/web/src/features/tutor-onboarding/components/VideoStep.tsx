'use client';

import { useState } from 'react';
import { FormField } from '@/components/ui/FormField';
import { Input } from '@/components/ui/Input';
import { TutorApplication, UpdateDraftCommand } from '../types';
import { StepShell } from './StepShell';
import styles from './VideoStep.module.css';

export interface VideoStepProps {
  application: TutorApplication;
  onContinue: (patch: UpdateDraftCommand) => void;
  onBack: () => void;
  isSaving: boolean;
  error: string | null;
}

/** Adapted from Preply's own "Do/Don't" video-requirements panel
 * (docs/product/preply-reverse-engineering/18-screenshot-evidence.md, SS08–SS09) — reworded in
 * TutorFlow's own copy, not reproduced verbatim. Presentational guidance only; nothing here is
 * enforced as a validation rule (no hard length/format check is invented either). */
const DO_ITEMS = [
  'Keep it between 30 seconds and 2 minutes',
  'Record in landscape (horizontal) mode, at eye level',
  'Use good lighting and a quiet, neutral background',
  'Use a stable surface so your video doesn’t appear shaky',
  'Make sure your face is clearly visible throughout',
  'Introduce yourself and mention your teaching experience',
];

const DONT_ITEMS = [
  'Share your phone number, email, or social media handles',
  'Include logos or links',
  'Read from slides or static images instead of talking to camera',
  'Include other people in the frame',
];

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
      <div className={styles.layout}>
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

        <aside className={styles.guidance} aria-label="Video guidelines">
          <p className={styles.guidanceTitle}>Video guidelines</p>
          <div>
            <p className={styles.doTitle}>Do</p>
            <ul className={styles.doList}>
              {DO_ITEMS.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </div>
          <div>
            <p className={styles.dontTitle}>Don&apos;t</p>
            <ul className={styles.dontList}>
              {DONT_ITEMS.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </div>
        </aside>
      </div>
    </StepShell>
  );
}
