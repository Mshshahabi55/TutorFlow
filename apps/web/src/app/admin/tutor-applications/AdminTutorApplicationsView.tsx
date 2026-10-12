'use client';

import { useState } from 'react';
import Link from 'next/link';
import { PageContainer } from '@/components/layout/PageContainer';
import { Card } from '@/components/ui/Card';
import { Badge, BadgeVariant } from '@/components/ui/Badge';
import { LoadingState } from '@/components/ui/LoadingState';
import { EmptyState } from '@/components/ui/EmptyState';
import { ErrorState } from '@/components/ui/ErrorState';
import { Pagination } from '@/components/ui/Pagination';
import { RequireAdmin } from '@/features/tutor-approval/components/RequireAdmin';
import { usePendingTutorApplications } from '@/features/tutor-approval/useTutorApproval';
import { PendingApplicationSummary, TutorApplicationStatus } from '@/features/tutor-approval/types';
import styles from './AdminTutorApplicationsView.module.css';

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

function formatDate(iso: string | null): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

function ApplicationRow({ application }: { application: PendingApplicationSummary }) {
  const { variant, label } = statusBadge(application.status);
  return (
    <Link href={`/admin/tutor-applications/${application.id}`} className={styles.rowLink}>
      <Card interactive className={styles.row}>
        <div className={styles.rowMain}>
          <span className={styles.name}>{application.displayName ?? 'Unnamed applicant'}</span>
          {application.headline && <span className={styles.headline}>{application.headline}</span>}
        </div>
        <div className={styles.rowMeta}>
          <Badge variant={variant}>{label}</Badge>
          <span className={styles.submittedAt}>Submitted {formatDate(application.submittedAtUtc)}</span>
        </div>
      </Card>
    </Link>
  );
}

const STATUS_FILTERS: { value: TutorApplicationStatus | undefined; label: string }[] = [
  { value: undefined, label: 'Pending (default)' },
  { value: 'Approved', label: 'Approved' },
  { value: 'Rejected', label: 'Rejected' },
  { value: 'Published', label: 'Published' },
  { value: 'Suspended', label: 'Suspended' },
];

export function AdminTutorApplicationsView() {
  const [status, setStatus] = useState<TutorApplicationStatus | undefined>(undefined);
  const [page, setPage] = useState(1);
  const { data, isLoading, isError, refetch } = usePendingTutorApplications(status, page, true);

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;

  return (
    <RequireAdmin>
      <PageContainer className={styles.page}>
        <div className={styles.header}>
          <h1>Tutor applications</h1>
          <label className={styles.filterLabel}>
            Status
            <select
              className={styles.filterSelect}
              value={status ?? ''}
              onChange={(e) => {
                setPage(1);
                setStatus((e.target.value || undefined) as TutorApplicationStatus | undefined);
              }}
            >
              {STATUS_FILTERS.map((f) => (
                <option key={f.label} value={f.value ?? ''}>
                  {f.label}
                </option>
              ))}
            </select>
          </label>
        </div>

        {isLoading ? (
          <LoadingState label="Loading applications…" />
        ) : isError ? (
          <ErrorState title="Couldn't load applications" onRetry={() => refetch()} />
        ) : !data || data.items.length === 0 ? (
          <EmptyState
            title="Nothing to review"
            description="There are no tutor applications matching this filter right now."
          />
        ) : (
          <>
            <div className={styles.list}>
              {data.items.map((application) => (
                <ApplicationRow key={application.id} application={application} />
              ))}
            </div>
            <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
          </>
        )}
      </PageContainer>
    </RequireAdmin>
  );
}
