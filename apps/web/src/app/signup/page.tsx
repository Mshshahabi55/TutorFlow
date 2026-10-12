import { Suspense } from 'react';
import { SignupPageContent } from './SignupPageContent';

export const metadata = { title: 'Sign up | TutorFlow' };

export default function SignupPage() {
  return (
    <Suspense fallback={null}>
      <SignupPageContent />
    </Suspense>
  );
}
