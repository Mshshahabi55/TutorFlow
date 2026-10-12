'use client';

import { PageContainer } from '@/components/layout/PageContainer';
import { Card } from '@/components/ui/Card';
import { Badge, BadgeVariant } from '@/components/ui/Badge';
import { ButtonLink } from '@/components/ui/ButtonLink';
import { LoadingState } from '@/components/ui/LoadingState';
import { EmptyState } from '@/components/ui/EmptyState';
import { ErrorState } from '@/components/ui/ErrorState';
import { useAuth } from '@/features/auth/useAuth';
import { useMyLessons } from '@/features/lessons/useMyLessons';
import { LessonStatus, LessonSummary } from '@/features/lessons/types';
import styles from './MyLessonsView.module.css';

// TutorFlow currently only ever writes LessonStatus.Scheduled (docs/domain/STATE_MACHINES.md:
// who/what marks a Lesson Completed is `[OPEN — Product Owner]`, not yet decided) — every other
// case is rendered honestly in case that changes, never assumed unreachable.
function statusBadge(status: LessonStatus): { variant: BadgeVariant; label: string } {
  switch (status) {
    case 'Completed':
      return { variant: 'success', label: 'Completed' };
    case 'InProgress':
      return { variant: 'info', label: 'In progress' };
    case 'Cancelled':
      return { variant: 'neutral', label: 'Cancelled' };
    case 'NoShow':
      return { variant: 'warning', label: 'No-show' };
    case 'Scheduled':
    default:
      return { variant: 'info', label: 'Upcoming' };
  }
}

function formatSlot(startUtc: string, endUtc: string): { dateLabel: string; timeLabel: string } {
  const start = new Date(startUtc);
  const end = new Date(endUtc);
  return {
    dateLabel: start.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' }),
    timeLabel: `${start.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' })}–${end.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit', timeZoneName: 'short' })}`,
  };
}

function LessonRow({ lesson }: { lesson: LessonSummary }) {
  const { variant, label } = statusBadge(lesson.status);
  const { dateLabel, timeLabel } = formatSlot(lesson.startUtc, lesson.endUtc);

  return (
    <Card className={styles.lessonCard}>
      <div className={styles.lessonHeader}>
        <span className={styles.dateLabel}>{dateLabel}</span>
        <Badge variant={variant}>{label}</Badge>
      </div>
      <p className={styles.timeLabel}>
        {timeLabel} · {lesson.durationMinutes} min
      </p>
    </Card>
  );
}

export function MyLessonsView() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const { data, isLoading, isError, refetch } = useMyLessons(isAuthenticated);

  if (authLoading) {
    return (
      <PageContainer>
        <LoadingState label="Loading…" />
      </PageContainer>
    );
  }

  if (!isAuthenticated) {
    return (
      <PageContainer className={styles.intro}>
        <Card className={styles.card}>
          <h1>My lessons</h1>
          <p>Sign in to see your upcoming and past lessons.</p>
          <div className={styles.actions}>
            <ButtonLink href="/signup?returnTo=%2Fmy-lessons">Sign up</ButtonLink>
            <ButtonLink href="/login?returnTo=%2Fmy-lessons" variant="secondary">
              Log in
            </ButtonLink>
          </div>
        </Card>
      </PageContainer>
    );
  }

  return (
    <PageContainer className={styles.page}>
      <h1 className={styles.title}>My lessons</h1>

      {isLoading ? (
        <LoadingState label="Loading your lessons…" />
      ) : isError ? (
        <ErrorState
          title="We couldn't load your lessons"
          description="Please try again."
          onRetry={() => refetch()}
        />
      ) : data && data.lessons.length === 0 ? (
        <EmptyState
          title="No lessons yet"
          description="Once you book and pay for a trial lesson, it will show up here."
          action={<ButtonLink href="/find-tutors">Find a tutor</ButtonLink>}
        />
      ) : (
        <div className={styles.list}>
          {data?.lessons.map((lesson) => (
            <LessonRow key={lesson.id} lesson={lesson} />
          ))}
        </div>
      )}
    </PageContainer>
  );
}
