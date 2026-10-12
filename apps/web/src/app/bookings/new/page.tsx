import { Suspense } from 'react';
import { BookingCheckoutBoundary } from './BookingCheckoutBoundary';

export const metadata = { title: 'Confirm your lesson | TutorFlow' };

export default function BookingNewPage() {
  return (
    <Suspense fallback={null}>
      <BookingCheckoutBoundary />
    </Suspense>
  );
}
