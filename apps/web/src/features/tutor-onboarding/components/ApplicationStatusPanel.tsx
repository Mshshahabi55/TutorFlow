'use client';

import { Alert } from '@/components/ui/Alert';
import { Button } from '@/components/ui/Button';
import { ButtonLink } from '@/components/ui/ButtonLink';
import { TutorApplication } from '../types';
import styles from './ApplicationStatusPanel.module.css';

export interface ApplicationStatusPanelProps {
  application: TutorApplication;
  onReopenForRevision: () => void;
  isReopening: boolean;
  reopenError: string | null;
}

/** Everything that isn't `Draft` — benchmark-confirmed states (Submitted → review within a few
 * business days → Approved/Rejected; Published) plus `Suspended`, already an accepted platform
 * action. `Approved` is deliberately honest about the still-platform-gated `Publish` step (no
 * Admin surface exists yet, ADR-0026) rather than implying the tutor did something wrong. */
export function ApplicationStatusPanel({ application, onReopenForRevision, isReopening, reopenError }: ApplicationStatusPanelProps) {
  switch (application.status) {
    case 'Submitted':
    case 'UnderReview':
      return (
        <Alert variant="info" title="Application submitted">
          Thanks — we&apos;re reviewing your application. Most applicants hear back within a few business days.
        </Alert>
      );
    case 'Approved':
      return (
        <Alert variant="success" title="Approved">
          Your application was approved. Your profile will go live shortly.
        </Alert>
      );
    case 'Published':
      return (
        <Alert variant="success" title="Your profile is live">
          Students can now find and book trial lessons with you.
          <div className={styles.profileLink}>
            {application.publicSlug && (
              <ButtonLink href={`/tutors/${application.publicSlug}`} variant="secondary" size="sm">
                View your public profile
              </ButtonLink>
            )}
            <ButtonLink href="/become-a-tutor/availability" variant="secondary" size="sm">
              Manage your availability
            </ButtonLink>
          </div>
        </Alert>
      );
    case 'Suspended':
      return (
        <Alert variant="error" title="Profile suspended">
          {application.suspensionReason ?? 'Your profile has been temporarily suspended.'}
        </Alert>
      );
    case 'Rejected':
      return (
        <Alert variant="warning" title="Changes requested">
          <p className={styles.reason}>{application.rejectionReason ?? 'Your application needs some changes before it can be approved.'}</p>
          {reopenError && (
            <p className={styles.error} role="alert">
              {reopenError}
            </p>
          )}
          <Button type="button" onClick={onReopenForRevision} loading={isReopening}>
            Revise application
          </Button>
        </Alert>
      );
    default:
      return null;
  }
}
