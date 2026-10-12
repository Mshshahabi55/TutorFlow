import { Suspense } from 'react';
import { MyLessonsView } from './MyLessonsView';
import { LoadingState } from '@/components/ui/LoadingState';

export const metadata = { title: 'My lessons | TutorFlow' };

export default function MyLessonsPage() {
  return (
    <Suspense fallback={<LoadingState />}>
      <MyLessonsView />
    </Suspense>
  );
}
