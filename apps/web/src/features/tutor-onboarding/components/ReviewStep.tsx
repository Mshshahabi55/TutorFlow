'use client';

import { Alert } from '@/components/ui/Alert';
import { Button } from '@/components/ui/Button';
import { TutorApplication } from '../types';
import styles from './ReviewStep.module.css';

export interface ReviewStepProps {
  application: TutorApplication;
  onBack: () => void;
  onSubmit: () => void;
  isSubmitting: boolean;
  error: string | null;
}

/** A recommended best-practice addition, not Preply-confirmed as a distinct UI (ADR-0026,
 * `PREPLY_WIZARD_COMPONENT_INVENTORY.md`) — a final submit action's existence is confirmed,
 * a dedicated review screen is not. Readiness here is informational only; the backend's own
 * `GetSubmissionReadinessErrors` remains the single authoritative check — bypassing this and
 * calling submit directly still gets rejected the same way. */
export function ReviewStep({ application, onBack, onSubmit, isSubmitting, error }: ReviewStepProps) {
  const missing: string[] = [];
  if (!application.displayName) missing.push('Display name (Basics)');
  if (!application.headline) missing.push('Headline (Basics)');
  if (!application.profilePhotoReference) missing.push('Profile photo (Photo)');
  if (application.subjectIds.length === 0) missing.push('At least one subject (Teaching)');
  if (application.spokenLanguages.length === 0) missing.push('At least one language (Teaching)');
  if (!application.hourlyRateAmountMinorUnits) missing.push('Hourly rate (Rate)');

  return (
    <div className={styles.step}>
      <h2 className={styles.title}>Review & submit</h2>
      <p className={styles.description}>
        Your application will be reviewed within a few business days. You&apos;ll be notified of the outcome, and if
        anything needs changing, you can revise and resubmit.
      </p>

      <dl className={styles.summary}>
        <div>
          <dt>Name</dt>
          <dd>{application.displayName || '—'}</dd>
        </div>
        <div>
          <dt>Headline</dt>
          <dd>{application.headline || '—'}</dd>
        </div>
        <div>
          <dt>Subjects</dt>
          <dd>{application.subjectIds.length} selected</dd>
        </div>
        <div>
          <dt>Languages</dt>
          <dd>{application.spokenLanguages.length} selected</dd>
        </div>
        <div>
          <dt>Hourly rate</dt>
          <dd>
            {application.hourlyRateAmountMinorUnits
              ? `${(application.hourlyRateAmountMinorUnits / 100).toFixed(2)} ${application.hourlyRateCurrency}`
              : '—'}
          </dd>
        </div>
        <div>
          <dt>Availability</dt>
          <dd>{application.availabilitySettingsConfigured ? 'Configured' : 'Not yet configured'}</dd>
        </div>
      </dl>

      {missing.length > 0 && (
        <Alert variant="warning" title="A few required sections are incomplete">
          <ul>
            {missing.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </Alert>
      )}

      {error && (
        <p className={styles.error} role="alert">
          {error}
        </p>
      )}

      <div className={styles.footer}>
        <Button type="button" variant="ghost" onClick={onBack} disabled={isSubmitting}>
          Back
        </Button>
        <Button type="button" onClick={onSubmit} loading={isSubmitting}>
          Submit application
        </Button>
      </div>
    </div>
  );
}
