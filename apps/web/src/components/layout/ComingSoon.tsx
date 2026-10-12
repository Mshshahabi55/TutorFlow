import { PageContainer } from './PageContainer';
import { EmptyState } from '@/components/ui/EmptyState';
import { ButtonLink } from '@/components/ui/ButtonLink';

/**
 * Placeholder for routes the public shell links to that are scoped to a later phase
 * (docs/design/FRONTEND_IMPLEMENTATION_ROADMAP.md) — keeps the shell fully navigable
 * without a 404 while being explicit that the page itself isn't built yet.
 */
export function ComingSoon({ title }: { title: string }) {
  return (
    <PageContainer style={{ paddingBlock: '4rem' }}>
      <EmptyState
        title={`${title} is coming soon`}
        description="This part of TutorFlow hasn't been built yet. Check back soon."
        action={<ButtonLink href="/">Back to home</ButtonLink>}
      />
    </PageContainer>
  );
}
