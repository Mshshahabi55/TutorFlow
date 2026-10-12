import { Suspense } from 'react';
import { FindTutorsView } from './FindTutorsView';
import { LoadingState } from '@/components/ui/LoadingState';

export const metadata = {
  title: 'Find a Tutor | TutorFlow',
  description: 'Search and filter tutors by subject, language, price, and availability.',
};

export default function FindTutorsPage() {
  return (
    <Suspense fallback={<LoadingState label="Loading search…" />}>
      <FindTutorsView />
    </Suspense>
  );
}
