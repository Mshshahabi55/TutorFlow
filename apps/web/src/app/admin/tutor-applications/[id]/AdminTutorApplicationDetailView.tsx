'use client';

import { FormEvent, useState } from 'react';
import { PageContainer } from '@/components/layout/PageContainer';
import { Card } from '@/components/ui/Card';
import { Badge, BadgeVariant } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { ButtonLink } from '@/components/ui/ButtonLink';
import { Textarea } from '@/components/ui/Textarea';
import { Alert } from '@/components/ui/Alert';
import { Avatar } from '@/components/ui/Avatar';
import { LoadingState } from '@/components/ui/LoadingState';
import { ErrorState } from '@/components/ui/ErrorState';
import { ApiError } from '@/lib/api/client';
import { resolveMediaUrl } from '@/features/tutor-profile/mediaUrl';
import { RequireAdmin } from '@/features/tutor-approval/components/RequireAdmin';
import {
  useApproveTutorApplication,
  useBeginReviewTutorApplication,
  usePublishTutorApplication,
  useRejectTutorApplication,
  useSuspendTutorApplication,
  useTutorApplicationForAdmin,
} from '@/features/tutor-approval/useTutorApproval';
import { AdminTutorApplicationDetail, TutorApplicationStatus } from '@/features/tutor-approval/types';
import styles from './AdminTutorApplicationDetailView.module.css';

function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.problem.detail ?? error.message;
  }
  return 'Something went wrong. Please try again.';
}

function statusBadge(status: TutorApplicationStatus): { variant: BadgeVariant; label: string } {
  switch (status) {
    case 'Submitted':
      return { variant: 'info', label: 'Submitted' };
    case 'UnderReview':
      return { variant: 'warning', label: 'Under review' };
    case 'Approved':
      return { variant: 'success', label: 'Approved' };
    case 'Published':
      return { variant: 'success', label: 'Published' };
    case 'Rejected':
      return { variant: 'neutral', label: 'Rejected' };
    case 'Suspended':
      return { variant: 'neutral', label: 'Suspended' };
    case 'Draft':
    default:
      return { variant: 'neutral', label: 'Draft' };
  }
}

function formatDateTime(iso: string | null): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleString(undefined, { year: 'numeric', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' });
}

function formatRate(application: AdminTutorApplicationDetail): string {
  if (application.hourlyRateAmountMinorUnits == null || !application.hourlyRateCurrency) return '—';
  return `${(application.hourlyRateAmountMinorUnits / 100).toFixed(2)} ${application.hourlyRateCurrency}/hr`;
}

function ReasonForm({
  label,
  submitLabel,
  isPending,
  error,
  onSubmit,
  onCancel,
}: {
  label: string;
  submitLabel: string;
  isPending: boolean;
  error: string | null;
  onSubmit: (reason: string) => void;
  onCancel: () => void;
}) {
  const [reason, setReason] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!reason.trim()) {
      setValidationError('A reason is required.');
      return;
    }
    setValidationError(null);
    onSubmit(reason.trim());
  };

  return (
    <form className={styles.reasonForm} onSubmit={handleSubmit}>
      <label className={styles.reasonLabel} htmlFor="reason">
        {label}
      </label>
      <Textarea id="reason" rows={3} maxLength={2000} value={reason} onChange={(e) => setReason(e.target.value)} />
      {validationError && (
        <p role="alert" className={styles.fieldError}>
          {validationError}
        </p>
      )}
      {error && (
        <Alert variant="error" title="Couldn't complete this action">
          {error}
        </Alert>
      )}
      <div className={styles.reasonActions}>
        <Button type="submit" variant="danger" loading={isPending}>
          {submitLabel}
        </Button>
        <Button type="button" variant="ghost" onClick={onCancel} disabled={isPending}>
          Cancel
        </Button>
      </div>
    </form>
  );
}

export function AdminTutorApplicationDetailView({ id }: { id: string }) {
  const { data: application, isLoading, isError, refetch } = useTutorApplicationForAdmin(id, true);
  const [showRejectForm, setShowRejectForm] = useState(false);
  const [showSuspendForm, setShowSuspendForm] = useState(false);

  const beginReview = useBeginReviewTutorApplication(id);
  const approve = useApproveTutorApplication(id);
  const reject = useRejectTutorApplication(id);
  const publish = usePublishTutorApplication(id);
  const suspend = useSuspendTutorApplication(id);

  return (
    <RequireAdmin>
      <PageContainer className={styles.page}>
        <ButtonLink href="/admin/tutor-applications" variant="ghost" size="sm" className={styles.backLink}>
          ← All applications
        </ButtonLink>

        {isLoading ? (
          <LoadingState label="Loading application…" />
        ) : isError || !application ? (
          <ErrorState title="Couldn't load this application" onRetry={() => refetch()} />
        ) : (
          <>
            <Card className={styles.headerCard}>
              <div className={styles.headerMain}>
                <Avatar src={resolveMediaUrl(application.profilePhotoReference)} name={application.displayName ?? 'Unnamed applicant'} size="lg" />
                <div>
                  <h1 className={styles.name}>{application.displayName ?? 'Unnamed applicant'}</h1>
                  {application.headline && <p className={styles.headline}>{application.headline}</p>}
                </div>
              </div>
              <Badge variant={statusBadge(application.status).variant}>{statusBadge(application.status).label}</Badge>
            </Card>

            <Card className={styles.detailCard}>
              <dl className={styles.detailGrid}>
                <div>
                  <dt>Submitted</dt>
                  <dd>{formatDateTime(application.submittedAtUtc)}</dd>
                </div>
                <div>
                  <dt>Reviewed</dt>
                  <dd>{formatDateTime(application.reviewedAtUtc)}</dd>
                </div>
                <div>
                  <dt>Rate</dt>
                  <dd>{formatRate(application)}</dd>
                </div>
                <div>
                  <dt>Years of experience</dt>
                  <dd>{application.yearsOfExperience ?? '—'}</dd>
                </div>
                <div>
                  <dt>Subjects</dt>
                  <dd>{application.subjectIds.length}</dd>
                </div>
                <div>
                  <dt>Languages</dt>
                  <dd>{application.spokenLanguages.map((l) => l.languageCode.toUpperCase()).join(', ') || '—'}</dd>
                </div>
                <div>
                  <dt>Education entries</dt>
                  <dd>{application.educationEntries.length}</dd>
                </div>
                <div>
                  <dt>Certificates</dt>
                  <dd>{application.certificateEntries.length}</dd>
                </div>
                <div>
                  <dt>Availability configured</dt>
                  <dd>{application.availabilitySettingsConfigured ? 'Yes' : 'No'}</dd>
                </div>
                <div>
                  <dt>Intro video</dt>
                  <dd>{resolveMediaUrl(application.introVideoReference) ? 'Provided' : '—'}</dd>
                </div>
              </dl>
              {application.about && (
                <div className={styles.about}>
                  <dt>About</dt>
                  <p>{application.about}</p>
                </div>
              )}
            </Card>

            {application.status === 'Rejected' && (
              <Alert variant="warning" title="Rejected">
                {application.rejectionReason ?? 'No reason recorded.'}
              </Alert>
            )}

            {application.status === 'Suspended' && (
              <Alert variant="error" title="Suspended">
                {application.suspensionReason ?? 'No reason recorded.'}
              </Alert>
            )}

            {application.status === 'Published' && application.publicSlug && (
              <Alert variant="success" title="Live">
                <ButtonLink href={`/tutors/${application.publicSlug}`} variant="secondary" size="sm">
                  View public profile
                </ButtonLink>
              </Alert>
            )}

            {application.status === 'Draft' && (
              <Alert variant="info" title="Not yet submitted">
                This application is still a draft and hasn&apos;t been submitted for review.
              </Alert>
            )}

            <Card className={styles.actionsCard}>
              {(application.status === 'Submitted' || application.status === 'UnderReview') && !showRejectForm && (
                <div className={styles.actionRow}>
                  {application.status === 'Submitted' && (
                    <Button variant="secondary" onClick={() => beginReview.mutate()} loading={beginReview.isPending}>
                      Begin review
                    </Button>
                  )}
                  <Button onClick={() => approve.mutate()} loading={approve.isPending}>
                    Approve
                  </Button>
                  <Button variant="danger" onClick={() => setShowRejectForm(true)}>
                    Reject
                  </Button>
                </div>
              )}

              {approve.isError && (
                <Alert variant="error" title="Couldn't approve">
                  {errorMessage(approve.error)}
                </Alert>
              )}
              {beginReview.isError && (
                <Alert variant="error" title="Couldn't start review">
                  {errorMessage(beginReview.error)}
                </Alert>
              )}

              {(application.status === 'Submitted' || application.status === 'UnderReview') && showRejectForm && (
                <ReasonForm
                  label="Reason for rejection"
                  submitLabel="Reject application"
                  isPending={reject.isPending}
                  error={reject.isError ? errorMessage(reject.error) : null}
                  onSubmit={(reason) => reject.mutate(reason, { onSuccess: () => setShowRejectForm(false) })}
                  onCancel={() => setShowRejectForm(false)}
                />
              )}

              {application.status === 'Approved' && (
                <div className={styles.actionRow}>
                  <Button onClick={() => publish.mutate()} loading={publish.isPending}>
                    Publish
                  </Button>
                </div>
              )}
              {publish.isError && (
                <Alert variant="error" title="Couldn't publish">
                  {errorMessage(publish.error)}
                </Alert>
              )}

              {application.status === 'Published' && !showSuspendForm && (
                <div className={styles.actionRow}>
                  <Button variant="danger" onClick={() => setShowSuspendForm(true)}>
                    Suspend
                  </Button>
                </div>
              )}

              {application.status === 'Published' && showSuspendForm && (
                <ReasonForm
                  label="Reason for suspension"
                  submitLabel="Suspend tutor"
                  isPending={suspend.isPending}
                  error={suspend.isError ? errorMessage(suspend.error) : null}
                  onSubmit={(reason) => suspend.mutate(reason, { onSuccess: () => setShowSuspendForm(false) })}
                  onCancel={() => setShowSuspendForm(false)}
                />
              )}
            </Card>
          </>
        )}
      </PageContainer>
    </RequireAdmin>
  );
}
