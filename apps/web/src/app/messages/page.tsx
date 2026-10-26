import { Suspense } from 'react';
import { ConversationListView } from './ConversationListView';
import { LoadingState } from '@/components/ui/LoadingState';

export const metadata = { title: 'Messages | TutorFlow' };

export default function MessagesPage() {
  return (
    <Suspense fallback={<LoadingState />}>
      <ConversationListView />
    </Suspense>
  );
}
