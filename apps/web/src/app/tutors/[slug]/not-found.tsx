import { PageContainer } from '@/components/layout/PageContainer';
import { EmptyState } from '@/components/ui/EmptyState';
import { ButtonLink } from '@/components/ui/ButtonLink';

export const metadata = {
  title: 'Tutor not found | TutorFlow',
  robots: { index: false, follow: false },
};

export default function TutorNotFound() {
  return (
    <PageContainer style={{ paddingBlock: '4rem' }}>
      <EmptyState
        title="Tutor not found"
        description="This tutor's profile doesn't exist, or isn't published right now."
        action={<ButtonLink href="/find-tutors">Back to search</ButtonLink>}
      />
    </PageContainer>
  );
}
