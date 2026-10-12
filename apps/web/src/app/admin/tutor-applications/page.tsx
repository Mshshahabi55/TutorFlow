import { Suspense } from 'react';
import { AdminTutorApplicationsView } from './AdminTutorApplicationsView';
import { LoadingState } from '@/components/ui/LoadingState';

export const metadata = { title: 'Tutor applications | TutorFlow Admin' };

export default function AdminTutorApplicationsPage() {
  return (
    <Suspense fallback={<LoadingState />}>
      <AdminTutorApplicationsView />
    </Suspense>
  );
}
