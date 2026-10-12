'use client';

import { useState } from 'react';
import { FormField } from '@/components/ui/FormField';
import { Input } from '@/components/ui/Input';
import { Textarea } from '@/components/ui/Textarea';
import { TutorApplication, UpdateDraftCommand } from '../types';
import { StepShell } from './StepShell';

export interface BasicsStepProps {
  application: TutorApplication;
  onContinue: (patch: UpdateDraftCommand) => void;
  isSaving: boolean;
  error: string | null;
}

/** Preply section 4 ("profile description") split into its name/headline and the about text —
 * display name and headline are both required at submission (ADR-0026 §3, corrected per the
 * 2026-10-05 independent audit: Preply's own sourced guidance marks both as required). */
export function BasicsStep({ application, onContinue, isSaving, error }: BasicsStepProps) {
  const [displayName, setDisplayName] = useState(application.displayName ?? '');
  const [headline, setHeadline] = useState(application.headline ?? '');
  const [about, setAbout] = useState(application.about ?? '');

  return (
    <StepShell
      title="The basics"
      description="How students will see your name and a short introduction."
      onContinue={() => onContinue({ displayName, headline, about })}
      continueDisabled={!displayName.trim() || !headline.trim()}
      isSaving={isSaving}
      error={error}
    >
      <FormField label="Display name" required>
        {(props) => (
          <Input
            {...props}
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            maxLength={200}
          />
        )}
      </FormField>

      <FormField label="Headline" required helperText="A short, engaging line students see first.">
        {(props) => (
          <Input {...props} value={headline} onChange={(e) => setHeadline(e.target.value)} maxLength={200} />
        )}
      </FormField>

      <FormField label="About you" helperText="Describe your teaching style and experience.">
        {(props) => (
          <Textarea {...props} value={about} onChange={(e) => setAbout(e.target.value)} rows={6} maxLength={4000} />
        )}
      </FormField>
    </StepShell>
  );
}
