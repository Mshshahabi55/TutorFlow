'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/Button';
import { FormField } from '@/components/ui/FormField';
import { FormSection } from '@/components/ui/FormSection';
import { Input } from '@/components/ui/Input';
import { Card } from '@/components/ui/Card';
import { CertificateEntryDraft, EducationEntryDraft, TutorApplication, UpdateDraftCommand } from '../types';
import { StepShell } from './StepShell';
import styles from './CredentialsStep.module.css';

export interface CredentialsStepProps {
  application: TutorApplication;
  onContinue: (patch: UpdateDraftCommand) => void;
  onBack: () => void;
  isSaving: boolean;
  error: string | null;
}

const EMPTY_EDUCATION: EducationEntryDraft = { institution: '', degree: '', fieldOfStudy: null, startYear: null, endYear: null };
const EMPTY_CERTIFICATE: CertificateEntryDraft = { name: '', issuingOrganization: '', issueYear: null, fileReference: null };

/** Preply sections 2+3 ("Certifications", "Education") — optional on both sides (no sourced
 * rule requires either, ADR-0026 §3). A `RepeatingEntryEditor`-shaped add/remove list, used
 * twice in this one step rather than promoted to a shared primitive (only this step needs it). */
export function CredentialsStep({ application, onContinue, onBack, isSaving, error }: CredentialsStepProps) {
  const [education, setEducation] = useState<EducationEntryDraft[]>(application.educationEntries);
  const [certificates, setCertificates] = useState<CertificateEntryDraft[]>(application.certificateEntries);

  return (
    <StepShell
      title="Education & certifications"
      description="Optional, but builds trust with students considering a trial lesson."
      onBack={onBack}
      onContinue={() => onContinue({ educationEntries: education, certificateEntries: certificates })}
      isSaving={isSaving}
      error={error}
    >
      <FormSection title="Education">
        <div className={styles.entries}>
          {education.map((entry, index) => (
            <Card key={index} className={styles.entry}>
              <FormField label="Institution">
                {(props) => (
                  <Input
                    {...props}
                    value={entry.institution}
                    onChange={(e) =>
                      setEducation((prev) => prev.map((x, i) => (i === index ? { ...x, institution: e.target.value } : x)))
                    }
                  />
                )}
              </FormField>
              <FormField label="Degree">
                {(props) => (
                  <Input
                    {...props}
                    value={entry.degree}
                    onChange={(e) => setEducation((prev) => prev.map((x, i) => (i === index ? { ...x, degree: e.target.value } : x)))}
                  />
                )}
              </FormField>
              <Button type="button" variant="ghost" size="sm" onClick={() => setEducation((prev) => prev.filter((_, i) => i !== index))}>
                Remove
              </Button>
            </Card>
          ))}
          <Button type="button" variant="secondary" size="sm" onClick={() => setEducation((prev) => [...prev, EMPTY_EDUCATION])}>
            Add education
          </Button>
        </div>
      </FormSection>

      <FormSection title="Certifications">
        <div className={styles.entries}>
          {certificates.map((entry, index) => (
            <Card key={index} className={styles.entry}>
              <FormField label="Certificate name">
                {(props) => (
                  <Input
                    {...props}
                    value={entry.name}
                    onChange={(e) => setCertificates((prev) => prev.map((x, i) => (i === index ? { ...x, name: e.target.value } : x)))}
                  />
                )}
              </FormField>
              <FormField label="Issuing organization">
                {(props) => (
                  <Input
                    {...props}
                    value={entry.issuingOrganization}
                    onChange={(e) =>
                      setCertificates((prev) => prev.map((x, i) => (i === index ? { ...x, issuingOrganization: e.target.value } : x)))
                    }
                  />
                )}
              </FormField>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => setCertificates((prev) => prev.filter((_, i) => i !== index))}
              >
                Remove
              </Button>
            </Card>
          ))}
          <Button type="button" variant="secondary" size="sm" onClick={() => setCertificates((prev) => [...prev, EMPTY_CERTIFICATE])}>
            Add certificate
          </Button>
        </div>
      </FormSection>
    </StepShell>
  );
}
