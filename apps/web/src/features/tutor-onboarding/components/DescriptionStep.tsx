'use client';

import { useState } from 'react';
import { FormField } from '@/components/ui/FormField';
import { Input } from '@/components/ui/Input';
import { Textarea } from '@/components/ui/Textarea';
import { TutorApplication, UpdateDraftCommand } from '../types';
import { StepShell } from './StepShell';
import styles from './DescriptionStep.module.css';

export interface DescriptionStepProps {
  application: TutorApplication;
  onContinue: (patch: UpdateDraftCommand) => void;
  onBack: () => void;
  isSaving: boolean;
  error: string | null;
}

const ABOUT_MAX_LENGTH = 4000;

/** Adapted from the Preply wizard's own four-prompt "Profile description" writing guide
 * (docs/product/preply-reverse-engineering/18-screenshot-evidence.md, SS06) — reworded in
 * TutorFlow's own copy, not reproduced verbatim. */
const WRITING_PROMPTS = [
  'Introduce yourself',
  'Describe your teaching experience',
  'Explain how you can help students reach their goals',
  'Give your profile a catchy headline',
];

/** Headline + About you — positioned as step 5 ("Description"), matching Preply's evidenced
 * breadcrumb position exactly (SS06: H1 "Profile description", step 5 of 8), not step 2 as this
 * content was previously placed under the name `BasicsStep`. This content was always a 1:1
 * content match for Preply's Description step — the earlier placement was a position error, not
 * a duplication of the About step (About and this step have zero overlapping fields). Headline
 * is still required at submission (ADR-0026 §3) — but Continue is not disabled client-side
 * pending that (ADR-0026 §3 addendum). Missing fields are now surfaced on the final Pricing step
 * when submission is attempted, not duplicated here. */
export function DescriptionStep({ application, onContinue, onBack, isSaving, error }: DescriptionStepProps) {
  const [headline, setHeadline] = useState(application.headline ?? '');
  const [about, setAbout] = useState(application.about ?? '');

  return (
    <StepShell
      title="Profile description"
      description="This info will go on your public profile. Write it in the language you'll be teaching."
      onBack={onBack}
      onContinue={() => onContinue({ headline, about })}
      isSaving={isSaving}
      error={error}
    >
      <FormField label="Headline" required helperText="A short, engaging line students see first.">
        {(props) => (
          <Input {...props} value={headline} onChange={(e) => setHeadline(e.target.value)} maxLength={200} />
        )}
      </FormField>

      <div className={styles.guide}>
        <p className={styles.guideTitle}>Writing tips</p>
        <ol className={styles.guideList}>
          {WRITING_PROMPTS.map((prompt) => (
            <li key={prompt}>{prompt}</li>
          ))}
        </ol>
      </div>

      <FormField label="About you" helperText="Describe your teaching style and experience.">
        {(props) => (
          <Textarea
            {...props}
            value={about}
            onChange={(e) => setAbout(e.target.value)}
            rows={6}
            maxLength={ABOUT_MAX_LENGTH}
          />
        )}
      </FormField>
      <p className={styles.counter} aria-live="polite">
        {about.length} / {ABOUT_MAX_LENGTH}
      </p>
    </StepShell>
  );
}
